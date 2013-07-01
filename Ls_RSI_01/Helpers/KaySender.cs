using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ls_RSI_01.Helpers
{
    public class KeySender
    {

        public const int HWND_BROADCAST = 0xFFFF;
        private const UInt32 WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);

        public static readonly int WM_ACTIVATEAPP = RegisterWindowMessage("WM_ACTIVATEAPP");

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [STAThread]
        public void Send()
        {
            bool createdNew;

            using (Mutex mutex = new Mutex(true, "MyMutexName", out createdNew))
            {
                foreach (Process process in Process.GetProcessesByName("Javaw"))
                {
                    Console.WriteLine(process.Id);
                    IntPtr handle = process.MainWindowHandle;
                    try
                    {

                        if (handle != IntPtr.Zero)
                        {
                            Logger.WriteToLog(DateTime.Now, "Send Message", Program.UserId);
                            SetForegroundWindow(handle);
                            SendMessage((IntPtr) HWND_BROADCAST, (int) WM_KEYDOWN, 0x20, 0);
                            SendMessage((IntPtr) HWND_BROADCAST, (int) WM_KEYDOWN, 0x0D, 0);
                        }
                        else
                        {
                            Logger.WriteToLog(DateTime.Now, "Post Message", Program.UserId);
                            SetForegroundWindow(handle);
                            SendMessage((IntPtr)HWND_BROADCAST, (int)WM_KEYDOWN, 0x20, 0);
                            SendMessage((IntPtr)HWND_BROADCAST, (int)WM_KEYDOWN, 0x0D, 0);
                            //PostMessage((IntPtr)HWND_BROADCAST, WM_ACTIVATEAPP, IntPtr.Zero, IntPtr.Zero);
                        }
                }
                catch (Exception e)
                    {
                        Logger.WriteToLog(DateTime.Now, "Error sending space key : " + e.Message, Program.UserId);

                        throw;
                    }

                    break;
                }
            }
        }
        
    }

   
}
