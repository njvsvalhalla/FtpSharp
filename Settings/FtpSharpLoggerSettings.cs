using Serilog;

namespace FtpSharp.Settings
{
    public class FtpSharpLoggerSettings
    {
        public string FileName { get; set; }
        public string LogDirectory { get; set; } = "/home/neal";
        public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;
    }
}