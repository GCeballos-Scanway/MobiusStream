using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;

namespace Mobius_Stream
{
    static class Program
    {

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>


        static string logFile = "logs\\" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".log";
        public static void Log(string line)
        {
            Debug.WriteLine(line);
            File.AppendAllLines(logFile, new string[] { DateTime.Now.ToString("HH:mm:ss.ff  ") + line });
        }

        [STAThread]
        static void Main(string[] args)
        {
            string addr = "localhost";
            bool topBar = false;



            if (Directory.Exists("D:\\"))
            {
                if (!Directory.Exists("D:\\MobiusData\\Logs\\MobiusStream"))
                {
                    Directory.CreateDirectory("D:\\MobiusData\\Logs\\MobiusStream");
                }
                logFile = "D:\\MobiusData\\Logs\\MobiusStream\\" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".log";
            }
            else
            {
                if (Directory.Exists("E:\\"))
                {
                    if (!Directory.Exists("E:\\Logs\\MobiusStream"))
                    {
                        Directory.CreateDirectory("E:\\Logs\\MobiusStream");
                    }
                    logFile = "E:\\Logs\\MobiusStream\\" + DateTime.Now.ToString("yyMMdd_HHmmss") + ".log";
                }
                else if (!Directory.Exists("logs"))
                {
                    Directory.CreateDirectory("logs");
                }
            }


            Log(String.Format("args.Length: {0}", args.Length));
            foreach (string arg in args)
            {
                try
                {
                    if (arg.Contains("-s")) topBar = true;
                    IPAddress address;
                    if (IPAddress.TryParse(arg, out address)) addr = arg;
                }
                catch { }
                Log(arg);
            }
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(addr, topBar));
        }
    }
}
