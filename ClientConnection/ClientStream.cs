using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FtpSharp.ClientConnection
{
    public class ClientStream// : IDisposable
    {
        public StreamReader Reader { get; private set; }
        private StreamWriter _writer;
        private readonly NetworkStream _networkStream;

        private string _outBuffer;

        public ClientStream(NetworkStream stream, bool useAscii = false)
        {
            Reader = useAscii ? new StreamReader(stream, Encoding.ASCII) : new StreamReader(stream);
            _writer = useAscii ? new StreamWriter(stream, Encoding.ASCII) : new StreamWriter(stream);

            _networkStream = stream;
        }

        public void SendFtpResult(FtpCode ftpCode, string message, bool flush = false)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _outBuffer = $"{(int)ftpCode} {message}";
            }

            if (flush)
                FlushBuffer();
        }

        public void SendFileResult(FileInfo fileInfo, bool flush = true)
        {
            _outBuffer = $"-rw-r--r--    2 2003     2003     {fileInfo.Length} {GetDate(fileInfo)} {fileInfo.Name}";
            if (flush)
                FlushBuffer();
        }

        public void SendDirectoryResult(DirectoryInfo directoryInfo, bool flush = true)
        {
            _outBuffer = $"drwxr-xr-x    2 2003     2003     4096 {GetDate(directoryInfo)} {directoryInfo.Name}";
            if (flush)
                FlushBuffer();
        }

        public void SendFile(FileStream fileStream, string transferType)
        {
            if (transferType == "I")
                CopyStream<byte>(fileStream);
            else
                CopyStream<char>(fileStream);
        }

        private void CopyStream<T>(Stream fileStream) where T : struct
        {
            var buffer = new T[4096];
            var count = 0;

            if (typeof(T) == typeof(char))
            {
                Reader = new StreamReader(fileStream);
                _writer = new StreamWriter(_networkStream, Encoding.ASCII);

                while ((count = Reader.Read(buffer as char[], 0, buffer.Length)) > 0)
                {
                    _writer.Write(buffer as char[], 0, count);
                }
            }
            else
            {
                while ((count = fileStream.Read(buffer as byte[], 0, buffer.Length)) > 0)
                {
                    _networkStream.Write(buffer as byte[], 0, count);
                }
            }
        }

        private static string GetDate(FileSystemInfo info) => info.LastWriteTime < DateTime.Now - TimeSpan.FromDays(180)
                                                        ? info.LastWriteTime.ToString("MMM dd  yyyy")
                                                        : info.LastWriteTime.ToString("MMM dd HH:mm");

        public void FlushBuffer()
        {
            if (!string.IsNullOrEmpty(_outBuffer))
            {
                _writer.WriteLine(_outBuffer);
                _writer.Flush();
            }
            _outBuffer = null;
        }

        public void UseSsl(X509Certificate cert)
        {
            var sslStream = new SslStream(_networkStream);
            sslStream.AuthenticateAsServer(cert);
            Reader = new StreamReader(sslStream);
            _writer = new StreamWriter(sslStream);
        }

        // #region Implement IDisposable

        // private bool disposedValue = false; // To detect redundant calls

        // protected virtual void Dispose(bool disposing)
        // {
        //     if (!disposedValue)
        //     {
        //         if (disposing)
        //         {
        //             Reader.Close();
        //             _writer.Close();
        //             Reader.Dispose();
        //             _writer.Dispose();
        //             _outBuffer = null;
        //         }

        //         disposedValue = true;
        //     }
        // }

        // ~ClientStream()
        // {
        //     Dispose(false);
        // }

        // public void Dispose()
        // {
        //     Dispose(true);
        // }

        // #endregion
    }
}