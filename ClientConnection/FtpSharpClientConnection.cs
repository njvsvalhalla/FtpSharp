using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using FtpSharp.Interfaces;
using FtpSharp.Settings;
using Serilog.Core;

namespace FtpSharp.ClientConnection
{
    public class FtpSharpClientConnection : IFtpConnection
    {
        private readonly TcpClient _tcpClient;
        private readonly ClientStream _clientStream;
        private readonly FtpSharpSettings _settings;
        private readonly Logger _logger;
        private string _transferType;
        private DataConnectionType _dataConnectionType;
        private TcpListener _passiveListener;
        private IFtpSharpUser _user;
        private TcpClient _dataClient;

        private IPEndPoint _dataEndpoint;
        private bool _userAuthenticated;
        private string _currentDirectory;

        public FtpSharpClientConnection(TcpClient client, FtpSharpSettings settings, Logger logger = null)
        {
            _tcpClient = client;

            _clientStream = new ClientStream(client.GetStream());

            _settings = settings;
            _logger = logger;
        }

        public void HandleConnection(object obj)
        {
            _clientStream.SendFtpResult(FtpCode.ServiceReady, $"{_settings.ServerName} Ready", true);

            try
            {
                var line = "";
                while (!string.IsNullOrEmpty(line = _clientStream.Reader.ReadLine()))
                {
                    var isQuitting = false;
                    var command = line.Split(' ');

                    var cmd = command[0].ToUpperInvariant();
                    var arguments = command.Length > 1 ? line.Substring(command[0].Length + 1) : null;

                    if (string.IsNullOrWhiteSpace(arguments))
                        arguments = null;

                    switch (cmd)
                    {
                        case "AUTH":
                            HandleAuth(arguments);
                            break;
                        case "USER":
                            HandleUser(arguments);
                            break;
                        case "PASS":
                            HandlePassword(arguments);
                            break;
                        case "CWD":
                            HandleChangeWorkingDirectory(arguments);
                            break;
                        case "CDUP":
                            HandleChangeWorkingDirectory("..");
                            break;
                        case "PORT":
                            HandlePort(arguments);
                            break;
                        case "PASV":
                            HandlePassive();
                            break;
                        case "PWD":
                            HandlePrintWorkingDirectory();
                            break;
                        case "LIST":
                            HandleList(arguments);
                            break;
                        case "RETR":
                            HandleRetrieve(arguments);
                            break;
                        case "TYPE":
                            if (arguments != null)
                            {
                                var splitArgs = arguments.Split(' ');
                                HandleType(splitArgs[0], splitArgs.Length > 1 ? splitArgs[1] : null);
                            }
                            break;
                        case "QUIT":
                            isQuitting = true;
                            HandleQuit();
                            break;
                        default:
                            HandleNotImplemented(cmd, arguments);
                            break;
                    }

                    if (_tcpClient == null || !_tcpClient.Connected)
                    {
                        break;
                    }

                    _clientStream.FlushBuffer();

                    if (cmd == "AUTH")
                    {
                        if (SslCertExists)
                            _clientStream.UseSsl(new X509Certificate(_settings.SslCertFile));
                    }

                    if (isQuitting)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception occured. {ex}");
            }
        }
        private bool SslCertExists =>  File.Exists(_settings.SslCertFile);

        private void HandleAuth(string authMode)
        {
            if (SslCertExists && authMode == "TLS")
                _clientStream.SendFtpResult(FtpCode.SecurityDataExchangeComplete, "Enabling TLS");
            else
                _clientStream.SendFtpResult(FtpCode.UnrecognizedAuthMode, $"Unrecognized AUTH Mode {authMode}");
        }

        private void HandleUser(string userName)
        {
            if (userName.ToLower() == "anonymous" && _settings.AllowAnonymous)
            {
                _user = _settings.AnonymousUser;
                _currentDirectory = _user.BaseDirectory;
                _userAuthenticated = true;
                _clientStream.SendFtpResult(FtpCode.UserLoggedIn, "Logged in as anonymous");
                _logger.Information($"LOGIN - Anonymous: true - IP {GetClientIp()}");
                return;
            }

            _user = _settings.Users.FirstOrDefault(x => x.UserName == userName);

            if (_user == null)
            {
                _clientStream.SendFtpResult(FtpCode.NotLoggedIn, "Username not found. Ensure username is correct.");
                _logger.Information($"FAILED LOGIN - Username not found. IP {GetClientIp()}");
                return;
            }

            _clientStream.SendFtpResult(FtpCode.UserOkNeedPassword, "Username accepted. Waiting for password..");
            _logger.Information($"LOGIN - User Accepted. User: {userName}. IP {GetClientIp()}");
        }

        private void HandlePassword(string password)
        {
            var authenticated = password == _user?.Password;

            //check just in case
            if (_user == null || !authenticated)
            {
                _clientStream.SendFtpResult(FtpCode.NotLoggedIn,
                    "Could not authenticate user. Please check your password.");
                _logger.Information($"FAILED LOGIN - User {_user?.UserName} IP {GetClientIp()}");
                return;
            }

            _userAuthenticated = true;
            _clientStream.SendFtpResult(FtpCode.UserLoggedIn, $"User logged in. Welcome, {_user.UserName}");
            _currentDirectory = _user.BaseDirectory;
            _logger.Information($"LOGIN - User {_user.UserName} IP ({GetClientIp()}");
        }

        private void HandleQuit()
        {
            if (!_userAuthenticated) return;

            _clientStream.SendFtpResult(FtpCode.CloseConnection, "Closing connection.");
            _logger.Information($"User Disconnected {_user.UserName} IP ({GetClientIp()}");
        }

        private void HandleNotImplemented(string commandName, string arguments)
        {
            _clientStream.SendFtpResult(FtpCode.CommandNotImplemented, "Command not implemented");
            _logger.Information($"Unknown command - {commandName} - args {arguments}");
        }

        private void HandlePrintWorkingDirectory()
        {
            var pathToSend = _currentDirectory ?? "/";
            _clientStream.SendFtpResult(FtpCode.PathCreated, $"\"{pathToSend}\" is current directory.");
        }

        private void HandleType(string typeCode, string formatControl)
        {
            if (!_userAuthenticated) return;

            switch (typeCode)
            {
                case "A":
                case "I":
                    _transferType = typeCode;
                    _clientStream.SendFtpResult(FtpCode.CommandOk, "OK");
                    break;
                case "E":
                case "L":
                default:
                    _clientStream.SendFtpResult(FtpCode.CommandNotImplementedForParameter, "Command not implemented for that parameter");
                    break;
            }

            switch (formatControl)
            {
                case "N":
                    _clientStream.SendFtpResult(FtpCode.CommandOk, "OK");
                    break;
                case "T":
                case "C":
                    _clientStream.SendFtpResult(FtpCode.CommandNotImplementedForParameter, "Command not implemented for that parameter");
                    break;
            }
        }

        private void HandlePort(string hostPort)
        {
            if (!_userAuthenticated) return;

            _dataConnectionType = DataConnectionType.Active;

            var ipAndPort = hostPort.Split(',');

            var ipAddress = new byte[4];
            var port = new byte[2];

            for (var i = 0; i < 4; i++)
            {
                ipAddress[i] = Convert.ToByte(ipAndPort[i]);
            }

            for (var i = 4; i < 6; i++)
            {
                port[i - 4] = Convert.ToByte(ipAndPort[i]);
            }

            if (BitConverter.IsLittleEndian)
                Array.Reverse(port);

            _dataEndpoint = new IPEndPoint(new IPAddress(ipAddress), BitConverter.ToInt16(port, 0));

            _clientStream.SendFtpResult(FtpCode.CommandOk, "Data Connection Established");
        }

        private void HandlePassive()
        {
            if (!_userAuthenticated) return;

            _dataConnectionType = DataConnectionType.Passive;

            _passiveListener = new TcpListener(GetClientIp(), 0);
            _passiveListener.Start();

            var passiveListenerEndpoint = (IPEndPoint) _passiveListener.LocalEndpoint;

            var address = passiveListenerEndpoint.Address.GetAddressBytes();
            var port = (short) passiveListenerEndpoint.Port;

            var portArray = BitConverter.GetBytes(port);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(portArray);

            _clientStream.SendFtpResult(FtpCode.EnteringPassiveMode,
                $"Entering Passive Mode ({address[0]},{address[1]},{address[2]},{address[3]},{portArray[0]},{portArray[1]}).");
        }

        private void HandleList(string path)
        {
            if (!_userAuthenticated) return;

            if (path == null)
                path = "";

            var fullPath = new DirectoryInfo(Path.Combine(_currentDirectory, path)).FullName;
            if (ValidPath(fullPath))
            {
                if (_dataConnectionType == DataConnectionType.Active)
                {
                    _dataClient = new TcpClient();
                    _dataClient.BeginConnect(_dataEndpoint.Address, _dataEndpoint.Port, GetList, fullPath);
                }
                else
                {
                    _passiveListener.BeginAcceptTcpClient(GetList, fullPath);
                }

                _clientStream.SendFtpResult(FtpCode.OpeningDataConnection,
                    $"Opening {_dataConnectionType} mode data transfer for LIST");
                return;
            }

            _clientStream.SendFtpResult(FtpCode.FileActionNotTaken, "Requested file action not taken");
        }

        private void GetList(IAsyncResult result)
        {
            if (!_userAuthenticated) return;

            if (_dataConnectionType == DataConnectionType.Active)
            {
                _dataClient.EndConnect(result);
            }
            else
            {
                _dataClient = _passiveListener.EndAcceptTcpClient(result);
            }

            var pathName = (string) result.AsyncState;

            var dataStream = new ClientStream(_dataClient.GetStream(), true);

            foreach (var directory in Directory.EnumerateDirectories(pathName))
            {
                dataStream.SendDirectoryResult(new DirectoryInfo(directory));
            }

            foreach (var file in Directory.EnumerateFiles(pathName))
            {
                dataStream.SendFileResult(new FileInfo(file));
            }

            _dataClient.Close();
            _dataClient = null;

            _clientStream.SendFtpResult(FtpCode.ClosingDataConnection, "Transfer complete", true);
        }

        private void HandleChangeWorkingDirectory(string path)
        {
            if (!_userAuthenticated) return;

            if (!_userAuthenticated) return;
            var newPath = Path.GetFullPath(Path.Combine(_currentDirectory, path));
            if (ValidPath(newPath))
                _currentDirectory = newPath;

            _clientStream.SendFtpResult(FtpCode.FileActionCompleted, "Changed directory");
        }

        private void HandleRetrieve(string filePath)
        {
            if (!_userAuthenticated) return;

            var file = GetValidPath(filePath);
            if (!ValidPath(file)) _clientStream.SendFtpResult(FtpCode.FileUnavailable, "File Not Found");
            if (!File.Exists(file)) return;
            if (_dataConnectionType == DataConnectionType.Active)
            {
                _dataClient = new TcpClient();
                _dataClient.BeginConnect(_dataEndpoint.Address, _dataEndpoint.Port, SendFile, file);
            }
            else
            {
                _passiveListener.BeginAcceptTcpClient(SendFile, file);
            }

            _clientStream.SendFtpResult(FtpCode.OpeningDataConnection,
                $"Opening {_dataConnectionType} mode data transfer for RETR");
        }

        private void SendFile(IAsyncResult result)
        {
            if (!_userAuthenticated) return;

            if (_dataConnectionType == DataConnectionType.Active)
            {
                _dataClient.EndConnect(result);
            }
            else
            {
                _dataClient = _passiveListener.EndAcceptTcpClient(result);
            }

            var fileName = (string) result.AsyncState;

            var dataStream = new ClientStream(_dataClient.GetStream());
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                dataStream.SendFile(fileStream, _transferType);
                _dataClient.Close();
                _dataClient = null;
                _clientStream.SendFtpResult(FtpCode.ClosingDataConnection, "File transfer complete", true);
            }
        }


        private string GetValidPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (path == "/")
                return _user.BaseDirectory;
            return path.StartsWith("/")
                ? new FileInfo(Path.Combine(_user.BaseDirectory, path.Substring(1))).FullName
                : new FileInfo(Path.Combine(_currentDirectory, path)).FullName;
        }

        private bool ValidPath(string path) => path.StartsWith(_user.BaseDirectory);
        private IPAddress GetClientIp() => ((IPEndPoint) _tcpClient.Client.RemoteEndPoint).Address;

        private enum DataConnectionType
        {
            Passive,
            Active
        }


        #region Implement IDisposable

        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            _disposedValue = true;
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FtpSharpClientConnection() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}