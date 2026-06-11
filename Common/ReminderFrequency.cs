using System;
using System.Collections.Generic;
using System.Text;

namespace BackupSyncApp.Common
{
    public enum ReminderFrequency
    {
        Daily,
        Weekly, // specific day of the week
        Monthly, // specific number
        Yearly // specific date
    }
}
