# FtpSharp

Simple FTP Server written in C#

## Why?

Through out the past couple years I've needed random files served with a no-frills ftp server and I didn't want to have to mess with configs.
Also, thought it would be fun to implement a protocol based off RFC.
It's probably not secure, scalable, or anything. I really only tested this over my local network.


## Requirements

This library is .NET Standard 2.0 compatible.

# Howto

The server is configured with FtpSharpSettings. Most settings are really optional, but if you don't set Anonymous or other users no one can obviously connect.

## ServerName
_optional_: This is only really displayed when a user connects to the server. Default is "FTPSharp".

## Port
_optional, but recommended_: I have this set to 21221 as a default. Be mindful of your system and what ports you can set. Linux throws an exception if you do it under 1024 (unless you are root).

## SslCertFile
_optional_: If you want to use SSL, create a cert. On windows you can use [makecert.exe](https://docs.microsoft.com/en-us/previous-versions/dotnet/netframework-2.0/bfsktky3(v=vs.80)) and Linux you can use [makecert](https://linux.die.net/man/1/makecert).

## UseIp6
_optional_: If you want to use IPV6 use this.

## Anonymous users

_optional_: If you want to allow anonymous users, set AllowAnonymous to true. You must also set AnonymousUser with a new AnonymousFtpSharpUser, which you need to set the BaseDirectory.

## Authenticating users

_optional, but recommended_: There is a List<FtpSharpUser> Users. FtpSharpUser contains username, password and base directory.

## Logging
_optional, but recommended_: LoggerSettings is FtpSharpLoggerSettings, which takes a FileName and LogDirectory. Optionally, you can also set RollingInterval, which is what SeriLog has (by default is Day so it will create a log file each day it's running)

## Example console app

    public class Program
    {
        public static void Main(string[] args)
        {
            var ftpSharpSettings = new FtpSharpSettings
            {
                AllowAnonymous = false,
                Users = new List<FtpSharpUser> {
                    new FtpSharpUser {
                        UserName = "neal",
                        Password = "laen",
                        BaseDirectory = "/home/neal/Ftp/Files"
                    }
                },
                LoggerSettings = new FtpSharpLoggerSettings
                {
                    FileName = "FtpSharp.txt",
                    LogDirectory = "/home/neal/Ftp/Logs"
                }
            };    
            var ftpSharpClient = new FtpSharpClient(ftpSharpSettings, true);
            ftpSharpClient.StartClient();
            Console.WriteLine("Client started.");
            Console.Read();
        }
    }

# Credits

Shout out to Rick Bassham for this [guide](https://www.codeproject.com/Articles/380769/Creating-an-FTP-Server-in-Csharp-with-IPv6-Support) that gave me a jump start on how to approach implementing an RFC and how to handle connections in C#. Learned a ton!

# TODO

 - Better logger options (ie turn off failed logins, log ip)
 - Log more (started off ok but then got more into implementation)
 - Make passwords compare against a salt instead if you want to load from a file I guess
 - Finish implementations
 - Validate more for settings
 - I'd really like to refactor it a little bit more
 - UNIT TESTS (and mocking IFtpConnection)
 - Ensure that it xfers binaries properly. I did some tests and md5 seemed to work.
 - Write something to console like, "server is now listening on "
 - I think it's probably fine but make sure it can handle multiple users and make sure log works fine
 - Make nuget package
 - Actually utilize IDisposable?
 - Bug check more

 # License

 This is under the MIT license.