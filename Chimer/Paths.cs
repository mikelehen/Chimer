using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chimer
{
    static class Paths
    {
        private static readonly string CONFIG_FILE = "config.json";
        private static readonly string LOG_FILE = "chimer.log";

        public static string BaseDir
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string ConfigFile
        {
            get
            {
                return BaseDir + "\\" + CONFIG_FILE;
            }
        }

        public static string LogFile
        {
            get
            {
                return BaseDir + "\\" + LOG_FILE;
            }
        }

    }
}
