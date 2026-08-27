using System;
using System.IO;

namespace KBAtomCreator
{
    internal class Logger
    {
        private static readonly string logFile = "log.txt";
        public static void WriteLog(string logEntry, Exception e) 
        {
            File.WriteAllText(logFile, $"{logEntry}. Исходная ошибка {e.Message}");
        }
        public static void WriteLog(string logEntry)
        {
            File.WriteAllText(logFile, $"{logEntry}.");
        }
    }
}
