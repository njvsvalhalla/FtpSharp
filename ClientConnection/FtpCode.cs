namespace FtpSharp.ClientConnection
{
    public enum FtpCode
    {
        ServiceReady = 220,
        SecurityDataExchangeComplete = 234,
        UnrecognizedAuthMode = 504,
        CloseConnection = 221,
        CommandNotImplemented = 502,
        CommandNotImplementedForParameter = 504,
        EnteringPassiveMode = 227,
        OpeningDataConnection = 150,
        ClosingDataConnection = 226,
        FileActionCompleted = 250,
        FileActionNotTaken = 450,
        CommandOk = 200,
        PathCreated = 257,
        UserOkNeedPassword = 331,
        UserLoggedIn = 230,
        NotLoggedIn = 530,
        FileUnavailable = 550
    }
}