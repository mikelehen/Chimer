using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chimer
{
    static class Logger
    {
        private static StreamWriter logWriter = new StreamWriter(Paths.LogFile, append: true);

        public static event EventHandler<String> MessageLogged;

        public static void Log(string text) {
            string message = DateTime.Now.ToString() + ": " + text + "\r\n"; 
            
            logWriter.Write(message);
            logWriter.Flush();

            MessageLogged(null, message);
        }
    }
}
