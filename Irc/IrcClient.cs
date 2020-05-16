using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SKOsuIrc.Network;

namespace SKOsuIrc.Irc
{
    /// <summary>
    /// Do NOT use this, use <see cref="OsuIrcClient"/>
    /// </summary>
    public class IrcClient : IDisposable
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public bool IPv6 { get; set; }

        private TcpClient _client;
        private Stream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;

        private Task _readerTask;
        private CancellationToken _readerToken;
        private CancellationTokenSource _readerSource;

        public event EventHandler<string> OnMessageRecieved;

        public IrcClient(string host, int port, string user, string password, bool ipv6)
        {
            Host = host;
            Port = port;
            User = user;
            Password = password;
            IPv6 = ipv6;
        }

        internal void Login()
        {
            WriteAsync("PASS " + Password).Wait();
            WriteAsync("NICK " + User).Wait();
        }

        public bool IsValid => !string.IsNullOrEmpty(Host) && Port > 0 &&
                                !string.IsNullOrEmpty(User) &&
                                !string.IsNullOrEmpty(Password);

        internal void Connect()
        {
            AddressFamily addrFam = IPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            _client = new TcpClient(addrFam);

            IPAddress ip = Dns.GetHostEntry(Host).AddressList.First(i => i.AddressFamily == addrFam);

            _client.Connect(new IPEndPoint(ip, Port));
            _stream = NetworkStream.Synchronized(_client.GetStream());
            _reader = new StreamReader(_stream);
            _writer = new StreamWriter(_stream);
        }

        internal void Disconnect()
        {
            Dispose();
        }

        private bool _isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                StopReading();
                _reader.Dispose();
                _writer.Dispose();

                _client.Close();
            }

            _isDisposed = true;
        }

        internal async Task<string> ReadAsync()
        {
            string line = await _reader.ReadLineAsync().ConfigureAwait(true);

            if (line == null)
                return null;

            return line;
        }

        internal void StartReadingAsync()
        {
            _readerSource = new CancellationTokenSource();
            _readerToken = _readerSource.Token;

            _readerTask = new Task(() =>
            {
                while (true)
                {
                    string line = ReadAsync().Result;

                    if (string.IsNullOrEmpty(line))
                        continue;

                    OnMessageRecieved?.Invoke(this, line);
                }
            }, _readerToken);
            _readerTask.Start();
        }

        internal void StopReading()
        {
            if (_readerTask != null && _readerTask.Status != TaskStatus.Running)
                return;

            _readerSource.Cancel();
        }

        internal async Task WriteAsync(string line)
        {
            await _writer.WriteLineAsync(line).ConfigureAwait(true);
            _writer.Flush();
        }

    }
}
