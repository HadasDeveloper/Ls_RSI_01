using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Ls_RSI_01.Halpers;
using System.Configuration;

namespace Ls_RSI_01.Model
{
    public class DataHelper
    {
        private const string ConnectionFormat = "User Id={0};Data Source={1};Initial Catalog={2};connection timeout={3};Password={4}";

        private const string DataSource = "WORK\\HADASSQL";
        private const string Password = "m4ffCr113P3vqOGGtuTW";
        private const string UserId = "DevUser";
        private const string DefaultDb = "Dev";
        private const int ConnectionTimeout = 3600;

        private SqlConnection _connection;
        private bool _isConnected;

        public bool IsConnected
        {
            get { return _isConnected; }
        }

        public void Connect(string initialCatalog)
        {
            if (_isConnected) return;
            if (_connection != null && _connection.State == ConnectionState.Connecting)
            {
                return;
            }

            lock (new object())
            {
                _connection = new SqlConnection { ConnectionString = string.Format(ConnectionFormat, UserId, DataSource, initialCatalog, ConnectionTimeout, Password) };

                if (_connection.State != ConnectionState.Open)
                {
                    try
                    {
                        _connection.Open();
                        _isConnected = true;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteToLog(DateTime.Now, "DataHelper.Connect(): " + e.Message);
                        if (_connection.State != ConnectionState.Open)
                            _isConnected = false;
                    }
                }
            }
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                try
                {
                    _connection.Close();
                    _isConnected = false;
                }
                catch (Exception e)
                {
                    if (_connection.State != ConnectionState.Open)
                        _isConnected = false;
                    Logger.WriteToLog(DateTime.Now, "DataHelper.Disconnect(): " + e.Message);
                }
                finally
                {
                    _connection = null;
                }
            }
        }

        private SqlConnection GetConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
                Connect(DefaultDb);

            return _connection;
        }


        public void InsertDataTable(DataTable table, string storedProcedureName, string tableValueParameterName)
        {
            SqlCommand insertCommand = new SqlCommand(storedProcedureName, GetConnection()) { CommandType = CommandType.StoredProcedure };

            SqlParameter tvpParam = insertCommand.Parameters.AddWithValue(tableValueParameterName, table);
            tvpParam.SqlDbType = SqlDbType.Structured;

            try
            {
                insertCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.WriteToLog(DateTime.Now, "DataHelper.GetConnection(): " + e.Message);
            }
        }

        //run stored procedures
        // ----------------------  running  query ---------------------------------

        public void CalculateTodaysOeders(string userId, int numOfOrder, int capital, string direction)
        {
            ExecuteSQL(string.Format(StoredProcedures.CalculateTodaysOeders, userId, numOfOrder, capital, direction));
        }


        public DataTable GetRsiOrders(string userId)
        {
            return ExecuteSqlForData(string.Format(StoredProcedures.SqlGetTodaysOeders, userId)) ?? new DataTable();
        }


        internal DataTable GetUserSettings(string userId)
        {
            return ExecuteSqlForData(string.Format(StoredProcedures.SqlGetUserSettings, userId)) ?? new DataTable();
        }

        // ----------------------  Core Functions ---------------------------------

        public bool ExecuteSQL(string sql)
        {
            return ExecuteSQL(sql, CommandType.Text, null);
        }

        public bool ExecuteSQL(string sql, CommandType commandType, List<SqlParameter> parameters)
        {
            SqlCommand command;

            if (!IsConnected)
                Connect(DefaultDb);

            try
            {
                command = new SqlCommand(sql, GetConnection()) { CommandType = commandType };

                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var sqlParameter in parameters)
                    {
                        command.Parameters.Add(sqlParameter);
                    }
                }

                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Logger.WriteToLog(DateTime.Now, "DataHelper.ExecuteSQL(): " + e.Message);
                return false;
            }
            return true;
        }

        public DataTable ExecuteSqlForData(string sql)
        {
            if (!IsConnected || _connection == null)
                Connect(DefaultDb);

            System.Diagnostics.Debug.WriteLine(sql);

            DataTable result = null;
            SqlCommand command = null;
            SqlDataReader reader = null;
            try
            {
                command = new SqlCommand(sql, GetConnection()) { CommandType = CommandType.Text };
                command.CommandTimeout = 0;
                reader = command.ExecuteReader();
                if (reader != null)
                    while (reader.Read())
                    {
                        if (result == null)
                        {
                            result = CreateResultTable(reader);
                        }
                        DataRow row = result.NewRow();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.IsDBNull(i)
                                && !(reader.GetFieldType(i) == typeof(string))
                                && !(reader.GetFieldType(i) == typeof(DateTime)))
                            {
                                row[i] = 0;
                            }
                            else
                            {
                                row[i] = reader.GetValue(i);
                            }
                        }
                        result.Rows.Add(row);
                    }

                System.Diagnostics.Debug.WriteLine(reader.FieldCount);

                return result;
            }
            catch (SqlException e)
            {
                Logger.WriteToLog(DateTime.Now,"DataHelper.ExecuteSqlForData(): " + e.Message);
                return result;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                if (command != null)
                    command.Dispose();
            }
        }

        private static DataTable CreateResultTable(IDataRecord reader)
        {
            DataTable dataTable = new DataTable();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                DataColumn dataColumn = new DataColumn(reader.GetName(i), reader.GetFieldType(i));
                dataTable.Columns.Add(dataColumn);
            }

            return dataTable;
        }

    }
}
