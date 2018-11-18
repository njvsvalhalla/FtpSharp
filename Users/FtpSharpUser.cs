using FtpSharp.Interfaces;

namespace FtpSharp.Users
{
    public class FtpSharpUser : IFtpSharpUser
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string BaseDirectory { get; set; }
    }
}