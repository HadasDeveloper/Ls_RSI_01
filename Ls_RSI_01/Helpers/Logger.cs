using System;
using System.Collections.Generic;
using System.IO;
using Ls_RSI_01.Model;
using System.Globalization;
using System.Configuration;

namespace Ls_RSI_01.helper
{
    public static class Logger
    {
        public static string FolderPath = string.Format(ConfigurationManager.AppSettings["folderPath"], Program.UserId, DateTime.Now.Year, DateTime.Now.ToString("MMM", CultureInfo.InvariantCulture));
//        public static string FileName;

        static Logger()
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);
//            FileName = string.Format("{0}{1}_log.txt", FolderPath, DateTime.Now.ToString("MMMdd", CultureInfo.InvariantCulture));
        }

        private static string GetFilePath(string fileName)
        {
            return
                (string.Format("{0}{1}_{2}log.txt", FolderPath,
                               DateTime.Now.ToString("MMMdd", CultureInfo.InvariantCulture), fileName));
        }


        public static void WriteStartToLog(DateTime time, string message, string fileName)
        {
            string filePath = GetFilePath(fileName);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("");
                writer.WriteLine(time + " : " + message);
            }
        }

        public static void WriteToLog(DateTime time, string message, string fileName)
        {
            string filePath = GetFilePath(fileName);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(time + " : " + message);
            }
        }

        public static void WriteToProgramLog(DateTime time, string message)
        {
            const string filePath = "logs\\ProgramLog.txt";

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(time + " : " + message);
            }
        }

        public static void WriteEntryOrdersToLog(DateTime time, List<OrderInfo> orders,string mode, string fileName)
        {
            string filePath = GetFilePath(fileName);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("".PadLeft(4) + "Order Table:");
                writer.WriteLine("------------------------------------------------------------------------------------------------");
                writer.WriteLine("".PadLeft(2) + "Time" + "".PadLeft(2) + "Date" + "".PadLeft(14) + "Index" + "".PadLeft(4) + "OrderId" + "".PadLeft(4) + "Instrument" + "".PadLeft(3) + "Action" + "".PadLeft(4) + "Amount" + "".PadLeft(5) + "Status");
                writer.WriteLine("------------------------------------------------------------------------------------------------");
                for (int i = 0; i < orders.Count; i++)
                {
                    string direction;
                    if (mode.Equals("buy") || mode.Equals("Buy"))
                        direction = orders[i].Direction;
                    else 
                        if (orders[i].Direction.Equals("buy") || orders[i].Direction.Equals("Buy")) 
                            direction = "Sell";
                        else
                            direction = "Buy";
                    writer.WriteLine(String.Format("{0,-4} {1,-4} {2,-10} {3,-10} {4,-10} {5,-10} {6,-10} {7,-10}", time, orders[i].Date, i, orders[i].OrderId, orders[i].Symbol, direction, orders[i].RealAmount, orders[i].Status));
                }
            }
        }
        
        public static void WriteExitOrdersToLog(DateTime time, List<OrderInfo> orders,string mode, string fileName)
        {
            string filePath = GetFilePath(fileName);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("".PadLeft(4) + "Order Table:");
                writer.WriteLine("------------------------------------------------------------------------------------------------");
                writer.WriteLine("".PadLeft(2) + "Time" + "".PadLeft(2) + "Date" + "".PadLeft(14) + "Index" + "".PadLeft(4) + "OrderId" + "".PadLeft(4) + "Instrument" + "".PadLeft(3) + "Action" + "".PadLeft(4) + "Amount" + "".PadLeft(5) + "Status");
                writer.WriteLine("------------------------------------------------------------------------------------------------");
                for (int i = 0; i < orders.Count; i++)
                {
                    string direction;
                    if (mode.Equals("buy") || mode.Equals("Buy"))
                        direction = orders[i].Direction;
                    else 
                        if (orders[i].Direction.Equals("buy") || orders[i].Direction.Equals("Buy")) 
                            direction = "Sell";
                        else
                            direction = "Buy";
                    writer.WriteLine(String.Format("{0,-4} {1,-4} {2,-10} {3,-10} {4,-10} {5,-10} {6,-10} {7,-10}", time, orders[i].Date, i, orders[i].OrderId, orders[i].Symbol, direction, orders[i].RealAmount, orders[i].Status));
                }
            }
        }

    }
}
