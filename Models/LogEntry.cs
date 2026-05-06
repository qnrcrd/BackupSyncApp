using System;
using System.Collections.Generic;
using System.Text;
using BackupSyncApp.Common;
namespace BackupSyncApp.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get;}
        public string Message { get;}
        public LogMessageType Type { get;}

        public LogEntry(string message, LogMessageType type)
        {
            Timestamp = DateTime.Now;
            Message = message;
            Type = type;
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {Message}";
        }
    }
}
