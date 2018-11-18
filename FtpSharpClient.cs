using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FtpSharp.ClientConnection;
using FtpSharp.Settings;
using Serilog;
using Serilog.Core;

namespace FtpSharp
{
    public class FtpSharpClient
    {
        private TcpListener _tcpListener;
        private readonly FtpSharpSettings _settings;
        private bool _listening;
        private readonly Logger _logger;

        public FtpSharpClient(FtpSharpSettings settings, bool start = false)
        {
            if (settings.UseIp6)
            {
                _tcpListener = new TcpListener(new IPEndPoint(IPAddress.IPv6Any, _settings.Port));
            }
            _tcpListener = new TcpListener(IPAddress.Any, settings.Port);

            _settings = settings;

            if (_settings.LoggerSettings != null)
            {
                _logger = new LoggerConfiguration()
                    .WriteTo.File(Path.Combine(settings.LoggerSettings.LogDirectory, settings.LoggerSettings.FileName),
                        rollingInterval: _settings.LoggerSettings.RollingInterval)
                    .CreateLogger();
            }

            if (start)
                StartClient();
        }

        public void StartClient()
        {
            _listening = true;
            _tcpListener?.Start();
            _tcpListener?.BeginAcceptTcpClient(HandleFtpClient, _tcpListener);
        }

        public void StopClient()
        {
            _listening = false;
            _tcpListener?.Stop();
            _tcpListener = null;
        }

        public void RestartClient()
        {
            StopClient();
            StartClient();
        }

        private void HandleFtpClient(IAsyncResult result)
        {
            if (!_listening) return;
            _tcpListener.BeginAcceptTcpClient(HandleFtpClient, _tcpListener);
            var tcpClient = _tcpListener.EndAcceptTcpClient(result);
            var sharpClient = new FtpSharpClientConnection(tcpClient, _settings, _logger);
            ThreadPool.QueueUserWorkItem(sharpClient.HandleConnection, tcpClient);

        }

    }
}