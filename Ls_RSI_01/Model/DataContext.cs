using Krs.Ats.IBNet;
using System;
using System.Collections.Generic;
using System.Data;

namespace Ls_RSI_01.Model
{
    public class DataContext
    {
        readonly DataHelper dbHelper = new DataHelper();

        public void StartConnection()
        {
            const string catalog = "SQL2008_856748_ntrlabs";
            dbHelper.Connect(catalog);
        }

        public void CloseConnection()
        {
            dbHelper.Disconnect();
        }

        //return data from server
        public List<OrderInfo> GetRsiOrders(UserSettings user)
        {
            List<OrderInfo> orders = new List<OrderInfo>();
            DataTable table = dbHelper.GetRsiOrders(user.UserId);

            foreach (DataRow row in table.Rows)
                orders.Add(new OrderInfo
                {
                    Date = (DateTime)row["date"],
                    Symbol = (string)row["instrument"],
                    Direction = (string)row["action"],
                    Amount = (int)row["amount"],
                    OrderId = 0,
                    Status = OrderStatus.None,
                    RealAmount = 0
                });

            return orders;
        }

        public void CalculateTodaysOeders(string userId, int numOfOrder, int capital, string direction)
        {
            dbHelper.CalculateTodaysOeders(userId, numOfOrder, capital, direction);
        }

        public UserSettings GetUserSettings(string userId)
        {
            UserSettings user = new UserSettings();

            DataTable table = dbHelper.GetUserSettings(userId);

            if (table.Rows.Count > 0)
            {
                user.UserId = (string) table.Rows[0][0];
                user.UserPassword = (string)table.Rows[0][1];
                user.UserPort = (int)table.Rows[0][2];
                user.Capital = (int)table.Rows[0][3];
                user.NumberOfOrders = (int)table.Rows[0][4];

            }
            return user;
        }
    }
}
