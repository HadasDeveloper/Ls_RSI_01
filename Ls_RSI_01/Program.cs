using System;
using System.Diagnostics;
using Ls_RSI_01.Helpers;

namespace Ls_RSI_01
{
    class Program
    {
        public static string UserId;
        
        static void Main(string[] args)
        {
            foreach (Process process in Process.GetProcessesByName("Javaw"))
            {
                Console.WriteLine(process.Id);
                process.Kill();
            }
 
            switch (args.Length)
            {
                // args[0] = UserId , //args[1] = 1/(-1) for enter/exit markat respectively
                case 2:
                    UserId = args[0];
                    ClientManager.Start(args);
                    break;
                default:
                    Logger.WriteToProgramLog(DateTime.Now, "Program.Main() : Number of elements does not match for starting the program");
                    break;
            }
        }
    }
}
