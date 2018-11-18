namespace FtpSharp.Interfaces
{
    public interface IFtpSharpUser
    {
        string UserName { get; }
        string Password { get; }
        string BaseDirectory { get; set; }
    }
}