using FtpSharp.Interfaces;

namespace FtpSharp.Users
{
    public class AnonymousFtpSharpUser : IFtpSharpUser
    {
        public string UserName { get; } = "anonymous";
        public string Password { get; } = null;
        public string BaseDirectory { get; set; }
    }
}