using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

//Todo: Add Documentation
namespace SKOsuIrc.Network
{
    public partial class BaseTcpClient : IDisposable
    {
        public string Host { get; }
        public int Port { get; }
        public State CurrentState { get; set; }

        private Socket _socket;

        public BaseTcpClient(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public void ConnectAsync()
        {
            try
            {
                CurrentState = State.Connecting;

                if (_socket != null && _socket.Connected)
                    Disconnect();

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                if (!IPAddress.TryParse(Host, out IPAddress hostIP))
                {
                    IPAddress[] addresses = Dns.GetHostAddresses(Host);

                    if (addresses == null || addresses.Length == 0)
                        throw new Exception("Could not find host " + Host);

                    hostIP = addresses[0];
                }

                _socket.BeginConnect(new IPEndPoint(hostIP, Port), new AsyncCallback(EndConnect), null);
            }
            catch (Exception ex)
            {
                CurrentState = State.Failed;
                throw ex;
            }
        }

        private void EndConnect(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);

                if (_socket.Connected)
                    CurrentState = State.Connected;
                else
                    CurrentState = State.Failed;
            }
            catch (Exception ex)
            {
                CurrentState = State.Failed;
                throw ex;
            }
        }

        public void Disconnect()
        {
            CurrentState = State.Disconnecting;
            _socket.Dispose();
            CurrentState = State.Disconnected;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public enum State
        {
            Disconnected,
            Connecting,
            Connected,
            Disconnecting,
            Reading,
            Authenticating,
            Failed
        }
    }

    public partial class BaseTcpClient
    {
        /// <summary>
        /// Use <see cref="UseReaderQueueLock"/> if you want to block reading until previous data has been processed
        /// </summary>
        public event EventHandler<byte[]> OnDataRecieved;
        /// <summary>
        /// If true waits until every byte has been process before calling <see cref="OnDataRecieved"/> again
        /// </summary>
        public bool UseReaderQueueLock;

        private Task _readerTask;
        private CancellationToken _readerToken;
        private CancellationTokenSource _readerSource;
        private EventWaitHandle _readerHandle;

        private ConcurrentQueue<byte[]> _readerQueue;
        private readonly object _queueReadLock = new object();

        public void StartReading()
        {
            CurrentState = State.Reading;

            _readerQueue = new ConcurrentQueue<byte[]>();
            _readerSource = new CancellationTokenSource();
            _readerToken = _readerSource.Token;
            _readerTask = new Task(ReadAsync, _readerToken);
            _readerHandle = new EventWaitHandle(true, EventResetMode.AutoReset);
            _readerTask.Start();
        }

        private void ReadAsync()
        {
            while (!_readerSource.IsCancellationRequested && _socket.Connected && CurrentState == State.Reading)
            {
                _readerHandle.WaitOne();

                StateObject so = new StateObject(2048);
                _socket.BeginReceive(so.Array, 0, 2048, SocketFlags.None, new AsyncCallback(EndRead), so);
            }
        }

        private void EndRead(IAsyncResult ar)
        {
            try
            {
                int length = _socket.EndReceive(ar);
                StateObject so = (StateObject)ar.AsyncState;

                if (length <= 0)
                {
                    _readerHandle.Set();
                    return;
                }

                so.Resize(length);

                string dataStr = "";

                foreach (byte b in so.Array)
                    dataStr += b;

                _readerQueue.Enqueue(so.Array);
                _readerHandle.Set();

                Task.Run(() => HandleData());
            }
            catch (Exception ex)
            {
                CurrentState = State.Failed;
                throw ex;
            }
        }

        private void HandleData()
        {
            Action handleDataAc = new Action(() =>
            {
                if (!_readerQueue.TryDequeue(out byte[] data))
                    return;

                OnDataRecieved?.Invoke(this, data);
            });

            if (UseReaderQueueLock)
            {
                lock(_queueReadLock)
                {
                    handleDataAc();
                    return;
                }
            }

            handleDataAc();
        }

        public void WriteAsync(byte[] data)
        {
            try
            {
                byte[] tosend = data;

                string dataStr = "";

                foreach (byte b in tosend)
                    dataStr += b;

                _socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(EndWrite), null);
            }
            catch (Exception ex)
            {
                CurrentState = State.Failed;
                throw ex;
            }
        }

        private void EndWrite(IAsyncResult ar)
        {
            try
            {
                _socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                CurrentState = State.Failed;
                throw ex;
            }
        }

        private class StateObject
        {
            public byte[] Array;

            public StateObject(byte[] array)
            {
                Array = array;
            }

            public StateObject(int length)
            {
                Array = new byte[length];
            }

            public void Resize(int newLength)
            {
                if (Array.Length == newLength)
                    return;

                System.Array.Resize(ref Array, newLength);
            }
        }
    }
}
