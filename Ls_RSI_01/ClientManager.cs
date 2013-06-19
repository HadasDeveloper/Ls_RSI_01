﻿using System;
using System.Collections.Generic;
using Krs.Ats.IBNet;
using Krs.Ats.IBNet.Contracts;
using Ls_RSI_01.Halpers;
using Ls_RSI_01.Model;
using System.Threading;

namespace Ls_RSI_01
{
    public class ClientManager
    {
        public static string Mode {get; set;}

        private static int nextOrderId;
        private static IBClient client;
        private static List<OrderInfo> orders = new List<OrderInfo>();
        private static int counter;
        private static bool fCurentTime;
        private static bool fNextValisId;
        private static bool done;
        private static UserSettings user;
        


        public static void Start(string[] args)
        {
            Logger.WriteStartToLog(DateTime.Now,"Start Process for userId:" + args[0]);

            DataContext dbmanager = new DataContext();

            //get user's settings 
            user = dbmanager.GetUserSettings(args[0]);

            //set Mode to "entry" (for market entry) or "exit" (for market exit) 
            //if args[1] = 1    :   Mode = "entry"
            //if args[1] = -1   :   Mode = "exit"
            GetProcessMode(args[1]);

            //if mode = 'entry' calculate new rsi orders and then get last(new) RSI orders list
            if (Mode.Equals("entry"))
                dbmanager.CalculateTodaysOeders(user.UserId, user.NumberOfOrders, user.Capital, args[1]);

            //if mode = 'sell' just get the last RSI orders list
            orders = dbmanager.GetRsiOrders(user);

            //orders[0].Symbol = "USD.JPY";
            //orders[0].Amount = 25000;
            //orders[0].Direction = "Sell";

            client = new IBClient {ThrowExceptions = true};
            client.NextValidId += (ClientNextValidId);
            client.OrderStatus += (ClientOrderStatus);
            client.ExecDetails += (ClientExecDetails);
            client.Error += (ClientError);
            client.UpdatePortfolio += (ClientUpdatePortfolio);
            client.UpdateAccountValue += (ClientUpdateAccountValue);
            client.CurrentTime += (ClientCurrentTime);

            Random random = new Random();
            int randomNumber = random.Next(0, 10000);

            //connect to TWS
            client.Connect("127.0.0.1", user.UserPort, randomNumber);

            client.RequestAccountUpdates(true, "");
            client.RequestCurrentTime();

//            client.RequestExecutions(1,);
           
            DateTime startingTime = DateTime.Now;

            // Close when all orders have been submited or 5 minutes have passed
            while (DateTime.Now.Subtract(startingTime).Minutes < 0.5 && counter < orders.Count)
            { 
                if(fCurentTime)
                    if(fNextValisId && done==false)
                        Work();

                Thread.Sleep(1000);
                if(DateTime.Now.Subtract(startingTime).Minutes >= 0.5)
                    Logger.WriteToLog(DateTime.Now, "Time Down");
            }

            Logger.WriteToLog(DateTime.Now, "Done");
        }

        //Get action Mode (buy/sell) from program starting elements  
        public static void GetProcessMode(string mode)
        {
            switch (mode)
            {
                case "1" :
                    Mode = "entry";
                    break;
                case "-1" :
                    Mode = "exit";
                    break;
                default:
                    Logger.WriteToLog(DateTime.Now, "ClientManager.GetOrdersMode: wrong action mode");
                    Environment.Exit(0);
                    break;
            }

        }

        //navigate between the exit and entry to the market
        public static void Work()
        {
            if (Mode.Equals("entry"))
                PlaceOrdersForMarketEntry();
            else
                PlaceOrdersForMarketExit();

            done = true;
        }

        //entry to the market and buy/sell orders determined by calculating the RSI values
        static public void PlaceOrdersForMarketEntry()
        {
            int id = nextOrderId;

            Logger.WriteToLog(DateTime.Now, "ClientManager.PlaceOrdersForMarketEntry(): start placing orders for entry");

           foreach (OrderInfo order in orders)
            {
                if (order.Amount == 0)
                    continue;

                //Equity stock = new Equity(order.Symbol);
                Forex stock = new Forex("usd","jpy");
                Order contract = new Order { Action = orders[0].Direction == "Buy" ? ActionSide.Buy : ActionSide.Sell, TotalQuantity = order.Amount };
                order.OrderId = id;
                int ordersCounter = 0;
                try
                {
                    client.PlaceOrder(id++, stock, contract);
                    Logger.WriteToLog(DateTime.Now, String.Format("ClientManager.PlaceOrdersForMarketEntry(): index:{0,-4}  orderid: {1,-4} symbol:{2,-4} diraction:{3,-4} amount:{4,-4} stutus:{5,-4}", ordersCounter++, contract.OrderId, stock.Symbol, contract.Action, contract.TotalQuantity, order.Status));
                }
                catch (Exception e)
                {
                    Logger.WriteToLog(DateTime.Now, "ClientManager.PlaceOrdersForBuy(): " + e.Message);
                }
            }
        }

        //exit market by buy/sell only orders that appears in the protfolio and were determined in the last time for market entry 
        static public void PlaceOrdersForMarketExit()
        {
            int id = nextOrderId;

            Logger.WriteToLog(DateTime.Now, "ClientManager.PlaceOrdersForMarketExit(): start placing orders for exit");

            foreach (OrderInfo order in orders)
            {
                //if realAmount = 0 there is no order to sell 
                if(order.RealAmount == 0)
                   continue;

                Equity stock = new Equity(order.Symbol);
                //Order contract = new Order() { Action = order.Direction == "Buy" ? ActionSide.Sell : ActionSide.Buy, TotalQuantity = order.RealAmount };
                
                                               //if protfolio amount nagative, buy the order if, positive sell the order 
                Order contract = new Order{ Action = order.RealAmount < 0 ? ActionSide.Buy : ActionSide.Sell, TotalQuantity = Math.Abs(order.RealAmount) };
                int ordersCounter = 0;
                try
                {
                    client.PlaceOrder(id++, stock, contract);
                    Logger.WriteToLog(DateTime.Now, String.Format("ClientManager.PlaceOrdersForMarketEntry(): index:{0,-4}  orderid: {1,-4} symbol:{2,-4} diraction:{3,-4} amount:{4,-4} stutus:{5,-4}", ordersCounter++, contract.OrderId, stock.Symbol, contract.Action, contract.TotalQuantity, order.Status));
                
                }
                catch (Exception e)
                {
                    Logger.WriteToLog(DateTime.Now,"ClientManager.PlaceOrdersForSell(): " + e.Message);
                    
                }
            }
        }

        //-----------------------------event handlers-----------------------------

        //find next valid ID
        static void ClientNextValidId(object sender, NextValidIdEventArgs e)
        {
            Logger.WriteToLog(DateTime.Now,"Next Valid Id: " + e.OrderId);

            nextOrderId = e.OrderId;
            fNextValisId = true;
        }

        //called if accured tws error
        static void ClientError(object sender, ErrorEventArgs e)
        {
            Logger.WriteToLog(DateTime.Now,"ClientManager.client_Error(): " + e.ErrorMsg);
            Console.WriteLine(e.ErrorMsg);
        }

        //get all the orders in the tws protfolio
        static void ClientUpdatePortfolio(object sender, UpdatePortfolioEventArgs e)
        {
            //Determines the real amount according to what appears in the portfolio
            foreach (OrderInfo order in orders)
                if (e.Contract.Symbol.Equals((order.Symbol)))
                {
                    order.RealAmount = e.Position;
                    Logger.WriteToLog(DateTime.Now,"ClientManager.client_UpdatePortfolio: found symbol: " + e.Contract.Symbol +" whith amount: " + e.Position);
                }
        }
    
        static void ClientUpdateAccountValue(object sender, UpdateAccountValueEventArgs e)
        {

        }

        //get order status
        static void ClientOrderStatus(object sender, OrderStatusEventArgs e)
        {
            // Loop through the order list and find the order to change the stuts for by looking at the order id
            foreach (OrderInfo order in orders)
                if (order.OrderId == e.OrderId)
                {
                    order.Status = e.Status;
                    counter++;
                    Logger.WriteToLog(DateTime.Now, "order: " + order.Symbol + " status:" + order.Status, "OrderStatus");
                }
        }

        static void ClientExecDetails(object sender, ExecDetailsEventArgs e)
        {
            Logger.WriteToLog(DateTime.Now, "ClientManager.client_ExecDetails(): Execution Time: " + e.Execution.Time + ", Symbol: " + e.Contract.Symbol +
                ", Side: " + e.Execution.Side + " Quantity: " + e.Execution.CumQuantity, " ExecDetails");
        }

        static void ClientCurrentTime(object sender, CurrentTimeEventArgs e)
        {
            Logger.WriteToLog(DateTime.Now, "ClientCurrentTime: " + e.Time);
            fCurentTime = true;
        }
        
    }
}
