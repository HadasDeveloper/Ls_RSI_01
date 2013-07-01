using System;
using System.IO;
using System.Globalization;
using System.Configuration;

namespace Ls_RSI_01.Helpers
{
    public static class Logger
    {
        public static string FolderPath = string.Format(ConfigurationManager.AppSettings["folderPath"], Program.UserId, DateTime.Now.Year, DateTime.Now.ToString("MMM", CultureInfo.InvariantCulture));

        static Logger()
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);
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

    }
}
