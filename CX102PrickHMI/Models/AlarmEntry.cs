namespace CX102PrickHMI.Models
{
    public sealed class AlarmEntry
    {
        public AlarmEntry(string time, string message, string level, string recoveryTime, string varName = null)
        {
            Time = time;
            Message = message;
            Level = level;
            RecoveryTime = recoveryTime;
            VarName = varName;
        }

        public string Time { get; }
        public string Message { get; }
        public string Level { get; }
        public string RecoveryTime { get; }
        public string VarName { get; }
    }
}
