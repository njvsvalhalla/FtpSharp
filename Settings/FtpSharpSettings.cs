using System.Collections.Generic;
using FtpSharp.Interfaces;
using FtpSharp.Users;

namespace FtpSharp.Settings
{
    public class FtpSharpSettings
    {
        #region  Server Settings

        public string ServerName { get; set; } = "FtpSharp";
        public int Port { get; set; } = 21221;
        public string SslCertFile { get; set; }
        public bool UseIp6 { get; set; }

        #endregion

        #region User List

        public IEnumerable<FtpSharpUser> Users { get; set; } = new List<FtpSharpUser>();
        public bool AllowAnonymous { get; set; }
        public AnonymousFtpSharpUser AnonymousUser { get; set; }

        #endregion

        #region Logger Settings

        public FtpSharpLoggerSettings LoggerSettings { get; set; }

        #endregion
    }
}