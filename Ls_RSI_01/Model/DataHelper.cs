using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Ls_RSI_01.helper;
using System.Configuration;

namespace Ls_RSI_01.Model
{
    public class DataHelper
    {

        private readonly string connectionFormat = ConfigurationManager.AppSettings["connectionFormat"];

        private readonly string dataSource = ConfigurationManager.AppSettings["dataSource"];
        private readonly string password = ConfigurationManager.AppSettings["password"];
        private readonly string userId = ConfigurationManager.AppSettings["userId"];
        private readonly string defaultDb = ConfigurationManager.AppSettings["defaultDB"];
        private readonly int connectionTimeout = Convert.ToInt16(ConfigurationManager.AppSettings["connectionTimeout"]);

        //private const string connectionFormat = "User Id={0};Data Source={1};Initial Catalog={2};connection timeout={3};Password={4}";

        //private const string dataSource = "WORK\\HADASSQL";
        //private const string password = "m4ffCr113P3vqOGGtuTW";
        //private const string userId = "DevUser";
        //private const string defaultDb = "Dev";
        //private const int connectionTimeout = 3600;

        private SqlConnection connection;
        private bool isConnected;

        public bool IsConnected
        {
            get { return isConnected; }
        }

        public void Connect(string initialCatalog)
        {
            if (isConnected) return;
            if (connection != null && connection.State == ConnectionState.Connecting)
            {
                return;
            }

            lock (new object())
            {
                connection = new SqlConnection { ConnectionString = string.Format(connectionFormat, userId, dataSource, initialCatalog, connectionTimeout, password) };

                if (connection.State != ConnectionState.Open)
                {
                    try
                    {
                        connection.Open();
                        isConnected = true;
                    }
                    catch (Exception e)
                    {
                        Logger.WriteToLog(DateTime.Now, "DataHelper.Connect(): " + e.Message,Program.UserId);
                        if (connection.State != ConnectionState.Open)
                            isConnected = false;
                    }
                }
            }
        }

        public void Disconnect()
        {
            if (isConnected)
            {
                try
                {
                    connection.Close();
                    isConnected = false;
                }
                catch (Exception e)
                {
                    if (connection.State != ConnectionState.Open)
                        isConnected = false;
                    Logger.WriteToLog(DateTime.Now, "DataHelper.Disconnect(): " + e.Message,Program.UserId);
                }
                finally
                {
                    connection = null;
                }
            }
        }

        private SqlConnection GetConnection()
        {
            if (connection == null || connection.State != ConnectionState.Open)
                Connect(defaultDb);

            return connection;
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
                Logger.WriteToLog(DateTime.Now, "DataHelper.GetConnection(): " + e.Message, Program.UserId);
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
                Connect(defaultDb);

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
                Logger.WriteToLog(DateTime.Now, "DataHelper.ExecuteSQL(): " + e.Message, Program.UserId);
                return false;
            }
            return true;
        }

        public DataTable ExecuteSqlForData(string sql)
        {
            if (!IsConnected || connection == null)
                Connect(defaultDb);

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
                Logger.WriteToLog(DateTime.Now, "DataHelper.ExecuteSqlForData(): " + e.Message, Program.UserId);
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
