using System;
using System.Collections.Generic;
using Krs.Ats.IBNet;
using Krs.Ats.IBNet.Contracts;
using Ls_RSI_01.Helpers;
using Ls_RSI_01.Model;
using System.Threading;
using System.Diagnostics;
using System.Configuration;

namespace Ls_RSI_01
{
    public class ClientManager
    {
        public static string Mode {get; set;}

        private static int nextOrderId; 
        private static int runTwsProcesId;
        private static int portfolioOedersCounter;
        private static int execCounter;
        private static DateTime runTwsStartingTime;
        private static IBClient client;
        private static List<OrderInfo> orders = new List<OrderInfo>();
        private static bool fCurentTime;
        private static bool fNextValisId;
        private static bool fPlaceOrders;
        private static bool fdone;
        private static UserSettings user;

        public static void Start(string[] args)
        {
            //set Mode to "entry" (for market entry) or "exit" (for market exit) 
            //if args[1] = 1    :   Mode = "entry"
            //if args[1] = -1   :   Mode = "exit"
            GetProcessMode(args[1]);

            Logger.WriteStartToLog(DateTime.Now, "starting Program", Program.UserId);
            Logger.WriteToProgramLog(DateTime.Now,
                                   String.Format("Start Process for userId: {0} for market {1}.", Program.UserId, Mode));

            DataContext dbmanager = new DataContext();

            //get user's settings 
            user = dbmanager.GetUserSettings(Program.UserId);
            if (user.UserId == null)
            {
                Logger.WriteToProgramLog(DateTime.Now, String.Format("wrong userId, cant find user settings for userId = {0}",Program.UserId));
                return;
            }

            //if mode = 'entry' calculate new rsi orders and get last(new) RSI orders list
            //if mode = 'exit' sell all the orders in the portfolio  
            if (Mode.Equals("entry"))
            {
                dbmanager.CalculateTodaysOeders(user.UserId, user.NumberOfOrders, user.Capital, args[1]);
                orders = dbmanager.GetRsiOrders(user);
            }

            client = new IBClient {ThrowExceptions = true};
            client.NextValidId += (ClientNextValidId);
            client.OrderStatus += (ClientOrderStatus);
            client.ExecDetails += (ClientExecDetails);
            client.Error += (ClientError);
            client.UpdatePortfolio += (ClientUpdatePortfolio);
            client.UpdateAccountValue += (ClientUpdateAccountValue);
            client.CurrentTime += (ClientCurrentTime);

            //connect to TWS
            ConnectToTws();
            if (!client.Connected)  
                ConnectToTws();
                

            if (!client.Connected)
            {
                Logger.WriteToLog(DateTime.Now, "ClientManager.Start: cannot connect to TWS, terminate the program", Program.UserId);
                Logger.WriteToProgramLog(DateTime.Now, string.Format("{0}: Could not connect to TWS", Program.UserId));
                if (runTwsProcesId!=0)
                    CloseTws();
                return;
            }

            client.RequestAccountUpdates(true, "");
            client.RequestCurrentTime();

            DateTime startingTime = DateTime.Now;

            // Close when all orders have been submited or 1.5 minutes have passed (counter = count orders that their status has changed)
            //while (DateTime.Now.Subtract(startingTime).Minutes < 1.5 && counter < orders.Count)
            while (!fdone)
            {
                if (fCurentTime)
                    //if (fNextValisId && done == false)
                    if (fNextValisId)
                        PlaceOrders();

                Thread.Sleep(1000); //1 secound (Wait a second for writing to the log all the remained order status)

                if (DateTime.Now.Subtract(startingTime).Minutes >= 3.5)
                {
                    Logger.WriteToLog(DateTime.Now, "Program Time Down", Program.UserId);
                    Logger.WriteToProgramLog(DateTime.Now, string.Format("{0}: Time Down", Program.UserId));
                    fdone = true;
                }
            }

            //close tws
            int closeAttempt = 0;
            if (runTwsProcesId != 0)
            {
                while (!CloseTws() && closeAttempt < 3)
                closeAttempt++;
            }

        Logger.WriteToLog(DateTime.Now, "Done", Program.UserId);
        Logger.WriteToProgramLog(DateTime.Now, string.Format("{0}: Done",Program.UserId));
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
                    Logger.WriteToLog(DateTime.Now, "ClientManager.GetOrdersMode: wrong action mode", Program.UserId);
                    Environment.Exit(0);
                    break;
            }

        }

        //try connect to tws, if it's closed run it and try again
        public static void ConnectToTws()
        {
            Random random = new Random();
            int randomNumber = random.Next(0, 10000);

            try
            {
                Logger.WriteToLog(DateTime.Now, "try connct to tws", Program.UserId);
                client.Connect("127.0.0.1", user.UserPort, randomNumber);
                Thread.Sleep(1000);
                if (client.Connected)
                    Logger.WriteToLog(DateTime.Now, "ClientManager.ConnectToTws: connected successfully to TWS", Program.UserId);
            }
            catch (Exception)
            {
                Logger.WriteToLog(DateTime.Now, "cant find open Tws", Program.UserId);

                //if there is now javaw Process running start new process
                if (runTwsProcesId == 0)
                {
                    Logger.WriteToLog(DateTime.Now, "starting new Tws process", Program.UserId);
                    Process runTws = new Process
                                         {
                                             StartInfo =
                                                 {
                                                     CreateNoWindow = false,
                                                     WorkingDirectory = ConfigurationManager.AppSettings["WorkingDirectory"],
                                                     FileName = ConfigurationManager.AppSettings["FileName"],
                                                     Arguments = string.Format("{0} username={1} password={2}",ConfigurationManager.AppSettings["Arguments"], user.UserId, user.UserPassword),
                                                     UseShellExecute = false,
                                                     RedirectStandardOutput = false
                                                 }
                                         };
                    runTws.Start();
                    runTwsProcesId = runTws.Id;
                    runTwsStartingTime = runTws.StartTime;
                    Logger.WriteToLog(DateTime.Now, "running Tws process", Program.UserId);
                }

                Logger.WriteToLog(DateTime.Now, "Before sending kay.send", Program.UserId);
                Thread.Sleep(20000); // 20 seconds

                KeySender key = new KeySender();

                key.Send(runTwsProcesId);
                Logger.WriteToLog(DateTime.Now, string.Format("After sending kay.send, "), Program.UserId);
                Thread.Sleep(60000); // 60 seconds
                
            }  
        
        }

        //close tws application and stops its javaw process
        static public bool CloseTws()
        {
            try
            {
                Process process = Process.GetProcessById(runTwsProcesId);
                if (process.StartTime.Equals(runTwsStartingTime) && runTwsProcesId != 0)
                {    
                    process.Kill();
                    Logger.WriteToLog(DateTime.Now, "Tws Process stoped successfully", Program.UserId);
                    Logger.WriteToProgramLog(DateTime.Now, "Tws Process stoped successfully");
                    return true;
                }
            }
            catch (Exception e)
            {
                Logger.WriteToLog(DateTime.Now, string.Format("ClientManager.CloseTws: {0}", e.Message), Program.UserId);
                return false;
            }
            return false;
        }


        //Place the orders
        static public void PlaceOrders()
        {
            int id = nextOrderId;

            fPlaceOrders = true;

            Logger.WriteToLog(DateTime.Now, string.Format("ClientManager.PlaceOrders: start placing orders for {0}", Mode) , Program.UserId);

            foreach (OrderInfo order in orders)
            {
                Equity stock = new Equity(order.Symbol);

                if (order.Amount == 0)
                    continue;

                Order contract = new Order{ Action = order.Direction == "Buy" ? ActionSide.Buy : ActionSide.Sell,TotalQuantity = order.Amount };

                contract.Tif = TimeInForce.Day;
                   
                order.OrderId = id;

                try
                {
                    client.PlaceOrder(id++, stock, contract);
                    Logger.WriteToLog(DateTime.Now,String.Format("ClientManager.PlaceOrders: orderid: {0,-4} symbol:{1,-4} diraction:{2,-4} amount:{3,-4} stutus:{4,-4}",order.OrderId, stock.Symbol, contract.Action, contract.TotalQuantity, order.Status), Program.UserId);
                }
                catch (Exception e)
                {
                    Logger.WriteToLog(DateTime.Now, String.Format("ClientManager.PlaceOrders: {0}", e.Message),Program.UserId);
                }
            }
            
                fdone = true;
                Logger.WriteToLog(DateTime.Now, String.Format("ClientManager.PlaceOrders: done placing orders"), Program.UserId);
                Logger.WriteToProgramLog(DateTime.Now, string.Format("{0}: done placing {1} orders", Program.UserId,execCounter));
        }


        //-----------------------------event handlers-----------------------------

        //find next valid ID
        static void ClientNextValidId(object sender, NextValidIdEventArgs e)
        {
            Logger.WriteToLog(DateTime.Now, string.Format("Next Valid Id: {0}", e.OrderId), Program.UserId);

            nextOrderId = e.OrderId;
            fNextValisId = true;
        }

        //called if accured tws error
        static void ClientError(object sender, ErrorEventArgs e)
        {
            Logger.WriteToLog(DateTime.Now, string.Format("ClientManager.client_Error(): {0}", e.ErrorMsg), Program.UserId);
            Console.WriteLine(e.ErrorMsg);
        }

        //get all the orders in the tws protfolio
        static void ClientUpdatePortfolio(object sender, UpdatePortfolioEventArgs e)
        {
            if (Mode.Equals("exit") && !fPlaceOrders)
            {
                if (e.Contract.SecurityType.Equals(SecurityType.Stock))
                {
                    OrderInfo order = new OrderInfo();
                    order.Symbol = e.Contract.Symbol;
                    order.Direction = e.Position < 0 ? "Buy" : "Sell";
                    order.Amount = Math.Abs(e.Position);

                    Logger.WriteToLog(DateTime.Now,string.Format("ClientManager.client_UpdatePortfolio: {0}). found symbol: {1,-4} whith amount: {2,-4}",
                                          portfolioOedersCounter, e.Contract.Symbol, e.Position), Program.UserId);
                    orders.Add(order);
                    portfolioOedersCounter++;

                }
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
                    Logger.WriteToLog(DateTime.Now, string.Format("ClientManager.ClientOrderStatus : Client order status for market {0}: order: {1,-4},  status:{2,-4}, ", Mode, order.Symbol, order.Status), " OrderStatus");
                }
        }

        static void ClientExecDetails(object sender, ExecDetailsEventArgs e)
        {
            execCounter++;
            Logger.WriteToLog(DateTime.Now, string.Format("ClientManager.client_ExecDetails(): Execution details for market {0} : Time: {1,-4}, Symbol: {2,-4}, Side: {3,-4}, Quantity: {4,-4} ", Mode, e.Execution.Time, e.Contract.Symbol, e.Execution.Side, e.Execution.CumQuantity), " ExecDetails");
        }

        static void ClientCurrentTime(object sender, CurrentTimeEventArgs e)
        {
            Logger.WriteToLog(DateTime.Now, string.Format("ClientManager.ClientCurrentTime: {0}", e.Time), Program.UserId);
            fCurentTime = true;
        }

    }
}
