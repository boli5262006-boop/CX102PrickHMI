namespace CX102PrickHMI.Models
{
    public sealed class AlarmEntry
    {
        public AlarmEntry(string time, string message, string level, string recoveryTime)
        {
            Time = time;
            Message = message;
            Level = level;
            RecoveryTime = recoveryTime;
        }

        public string Time { get; }
        public string Message { get; }
        public string Level { get; }
        public string RecoveryTime { get; }
    }
}
