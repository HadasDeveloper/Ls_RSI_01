using System;
using System.IO;
using System.Globalization;
using System.Configuration;

namespace Ls_RSI_01.Helpers
{
    public static class Logger
    {
        private static string FolderPath = string.Format(ConfigurationManager.AppSettings["userFolderPath"], Program.UserId, DateTime.Now.Year, DateTime.Now.ToString("MMM", CultureInfo.InvariantCulture));
        private static string globalfilePath = string.Format("{0}\\{1}.txt",ConfigurationManager.AppSettings["globalFolderPath"],DateTime.Now.ToString("yymmdd", CultureInfo.InvariantCulture));

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
            using (StreamWriter writer = new StreamWriter(globalfilePath, true))
            {
                writer.WriteLine(time + " : " + message);
            }
        }

    }
}
