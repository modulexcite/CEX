﻿#region Directives
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using VTDev.Libraries.CEXEngine.Crypto.Prng;
using VTDev.Libraries.CEXEngine.CryptoException;
#endregion

namespace VTDev.Libraries.CEXEngine.Networking
{
    /// <summary>
    /// State object for receiving data from remote device
    /// </summary>
    public class StateToken
    {
        /// <summary>
        /// The Client socket
        /// </summary>
        public Socket Client;
        /// <summary>
        /// The Receive buffer
        /// </summary>
        public byte[] Buffer;
        /// <summary>
        /// Received data container
        /// </summary>
        public MemoryStream Data;

        /// <summary>
        /// The state token constructor
        /// </summary>
        /// 
        /// <param name="Client">The client socket instance</param>
        /// <param name="BufferSize">Size of the Tcp buffer; default is 8 Kib (8192 bytes)</param>
        public StateToken(Socket Client, int BufferSize = 8192)
        {
            this.Client = Client;
            Buffer = new byte[BufferSize];
            Data = new MemoryStream();
        }
    }

    /// <summary>
    /// An event arguments class containing the error state information.
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        #region Fields
        /// <summary>
        /// The option flag containing optional state information
        /// </summary>
        public long OptionFlag = 0;
        /// <summary>
        /// The owner object
        /// </summary>
        public StateToken Owner;
        #endregion

        #region Constructor
        /// <summary>
        /// Returned when data has been received from a TCP Receive operation
        /// </summary>
        /// 
        /// <param name="Owner">The <see cref="StateToken"/> owner object</param>
        /// <param name="OptionFlag">The option flag containing additional state information</param>
        public DataReceivedEventArgs(StateToken Owner, int OptionFlag = 0)
        {
            this.Owner = Owner;
            this.OptionFlag = OptionFlag;
        }
        #endregion
    }

    /// <summary>
    /// A disposable socket class that wraps asychronous and sychronous network operations
    /// </summary>
    public class TcpSocket : IDisposable
    {
        #region Constants
        /// <summary>
        /// Maximum send or receive packet size, default is 240mb
        /// </summary>
        private const long MAXRCVBUFFER = 1024 * 1000 * 240;
        /// <summary>
        /// The maximum time to wait for a receive operation before the connection times out, default is infinite
        /// </summary>
        private const int RECEIVETIMEOUT = 0;
        /// <summary>
        /// The maximum time to wait for a send operation before the connection times out, default is infinite
        /// </summary>
        private const int SENDTIMEOUT = 0;
        /// <summary>
        /// The size of a receive buffer element, default is 8kib
        /// </summary>
        private const int RECEIVEBUFFER = 8192;
        /// <summary>
        /// The size of a send buffer element, default is 8kib
        /// </summary>
        private const int SENDBUFFER = 8192;
        #endregion

        #region Fields
        private Socket _cltSocket =  null;
        private ManualResetEvent _opDone = new ManualResetEvent(false);
        private bool _isDisposed = false;
        private bool _isListening = false;
        private bool _isServer = false;
        private Socket _lsnSocket;
        private long _maxAllocation = MAXRCVBUFFER;
        private int _rcvBufferSize = RECEIVEBUFFER;
        private int _rcvTimeout = RECEIVETIMEOUT;
        private int _sndBufferSize = SENDBUFFER;
        private int _sndTimeout = SENDTIMEOUT;
        private NetworkStream _tcpStream = null;
        #endregion

        #region Delegates/Events
        /// <summary>
        /// The Client Connected delegate
        /// </summary>
        /// <param name="owner">The owner object</param>
        /// <param name="args">A <see cref="SocketAsyncEventArgs"/> class</param>
        public delegate void ConnectedDelegate(object owner, SocketAsyncEventArgs args);
        /// <summary>
        /// The Client Connected event; fires each time a connection has been established
        /// </summary>
        public event ConnectedDelegate Connected;

        /// <summary>
        /// The Packet Received delegate
        /// </summary>
        /// <param name="owner">The owner object</param>
        /// <param name="Flag">The socket error flag</param>
        public delegate void DisConnectedDelegate(object owner, SocketError Flag);
        /// <summary>
        /// The Client Connected event; fires each time a connection has been established
        /// </summary>
        public event DisConnectedDelegate DisConnected;

        /// <summary>
        /// The Packet Received delegate
        /// </summary>
        /// <param name="args">A <see cref="DataReceivedEventArgs"/> class</param>
        public delegate void DataReceivedDelegate(DataReceivedEventArgs args);
        /// <summary>
        /// The Data Received event; fires each time data has been received
        /// </summary>
        public event DataReceivedDelegate DataReceived;
        #endregion

        #region Properties
        /// <summary>
        /// Get: The TCP Client Socket instance
        /// </summary>
        public Socket Client 
        {
            get { return _cltSocket; }
            private set {_cltSocket = value; } 
        }

        /// <summary>
        /// Get: The TCP Client is connected
        /// </summary>
        public bool IsConnected
        {
            get 
            {
                if (_cltSocket == null)
                    return false;

                return _cltSocket.Connected; 
            }
        }

        /// <summary>
        /// Get: The TCP Client is actively listening for a connection
        /// </summary>
        public bool IsListening
        {
            get { return _isListening; }
            private set { _isListening = value; }
        }

        /// <summary>
        /// Get: The TCP Client was started as a Listener
        /// </summary>
        public bool IsServer 
        {
            get { return _isServer; }
            private set { _isServer = value; }
        }

        /// <summary>
        /// Get: The local ip address
        /// </summary>
        public IPAddress LocalAddress
        {
            get
            {
                if (_cltSocket != null)
                {
                    if (_cltSocket.Connected)
                        return ((IPEndPoint)_cltSocket.LocalEndPoint).Address;
                }

                return IPAddress.None;
            }
        }

        /// <summary>
        /// Get: The local port number
        /// </summary>
        public int LocalPort
        {
            get
            {
                if (_cltSocket != null)
                {
                    if (_cltSocket.Connected)
                        return ((IPEndPoint)_cltSocket.LocalEndPoint).Port;
                }

                return 0;
            }
        }

        /// <summary>
        /// Get: Maximum allocation size of a Send or Receive buffer
        /// </summary>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the size is less than 1, or greater than 1GB</exception>
        public long MaxAllocation
        {
            get { return _maxAllocation; }
            set
            {
                if (value < 1)
                    throw new CryptoSocketException("TcpSocket:MaxAllocation", "The maximum buffer allocation size must be at least 1 byte!", new ArgumentException());
                if (value > MAXRCVBUFFER)
                    throw new CryptoSocketException("TcpSocket:MaxAllocation", string.Format("The maximum buffer allocation size must be less than {0} bytes!", MAXRCVBUFFER), new ArgumentException());

                _maxAllocation = value;
            }
        }

        /// <summary>
        /// Get/Set: The size of the TCP receive buffer
        /// </summary>
        public int ReceiveBufferSize
        {
            get { return _rcvBufferSize; }
            set 
            {
                _rcvBufferSize = value;

                if (_cltSocket != null)
                    _cltSocket.ReceiveBufferSize = value; 
            }
        }

        /// <summary>
        /// Get/Set: The synchronous read time out
        /// </summary>
        public int ReceiveTimeout
        {
            get { return _rcvTimeout; }
            set 
            {
                _rcvTimeout = value;

                if (_cltSocket != null)
                    _cltSocket.ReceiveTimeout = value; 
            }
        }

        /// <summary>
        /// Get/Set: The size of the TCP transmission buffer
        /// </summary>
        public int SendBufferSize
        {
            get { return _sndBufferSize; }
            set 
            {
                _sndBufferSize = value;

                if (_cltSocket != null)
                    _cltSocket.SendBufferSize = value; 
            }
        }

        /// <summary>
        /// Get/Set: The synchronous send time out
        /// </summary>
        public int SendTimeout
        {
            get { return _sndTimeout; }
            set 
            {
                _sndTimeout = value;

                if (_cltSocket != null)
                    _cltSocket.SendTimeout = value; 
            }
        }

        /// <summary>
        /// Get: The remote ip address
        /// </summary>
        public IPAddress RemoteAddress
        {
            get 
            {
                if (_cltSocket != null)
                {
                    if (Client.Connected)
                        return ((IPEndPoint)_cltSocket.RemoteEndPoint).Address;
                }

                return IPAddress.None;
            }
        }

        /// <summary>
        /// Get: The remote port number
        /// </summary>
        public int RemotePort
        {
            get 
            {
                if (_cltSocket != null)
                {
                    if (_cltSocket.Connected)
                        return ((IPEndPoint)_cltSocket.RemoteEndPoint).Port;
                }

                return 0;
            }
        }

        /// <summary>
        /// Get: The underlying Client NetworkStream instance
        /// </summary>
        public NetworkStream TcpStream 
        {
            get 
            {
                if (_tcpStream == null && _cltSocket != null)
                    _tcpStream = new NetworkStream(_cltSocket);

                return _tcpStream; 
            }
            private set { _tcpStream = value; }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize this class
        /// </summary>
        public TcpSocket()
        {
        }

        /// <summary>
        /// Finalize objects
        /// </summary>
        ~TcpSocket()
        {
            Dispose(false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Blocking method returns when the specified length of data has been read from the NetworkStream
        /// </summary>
        /// 
        /// <param name="Length">The number of bytes to read</param>
        /// <param name="TimeOut">The timeout period</param>
        /// 
        /// <returns>A MemoryStream containing the data</returns>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the MaxAllocation size is exceeded, or the socket is in an error state</exception>
        public MemoryStream GetStreamData(int Length, int TimeOut = Timeout.Infinite)
        {
            int len = 0;
            long total = 0;
            int oldTme = TcpStream.ReadTimeout;
            byte[] data = new byte[1024];
            MemoryStream stream = new MemoryStream();

            try
            {
                TcpStream.ReadTimeout = TimeOut;

                do
                {
                    if (Length - total < data.Length)
                        data = new byte[Length - total];

                    len = TcpStream.Read(data, 0, data.Length);
                    total += len;

                    // this can only happen if the client is misconfigured or under attack
                    if (total > MaxAllocation)
                        throw new CryptoSocketException("TcpSocket:GetStreamData", string.Format("The Tcp stream is larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());

                    stream.Write(data, 0, len);

                } while (total < Length);

                stream.Seek(0, SeekOrigin.Begin);
                TcpStream.ReadTimeout = oldTme;
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.ConnectionReset);
            }
            catch (Exception)
            {
                throw;
            }

            return stream;
        }

        /// <summary>
        /// Test if an application is listening on a port
        /// </summary>
        /// 
        /// <param name="Port">The port to test</param>
        /// 
        /// <returns>Returns <c>true</c> if an application is listening on the port, otherwise <c>false</c></returns>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the operation is in an error state</exception>
        public bool IsPortOpen(int Port)
        {
            var listener = default(TcpListener);

            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start();

                return true;
            }
            catch (SocketException)
            {
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (listener != null)
                    listener.Stop();
            }

            return false;
        }

        /// <summary>
        /// Get an open and randomly selected port number within a range
        /// </summary>
        /// 
        /// <param name="From">The minimum port number (default is 49152)</param>
        /// <param name="To">The maximum port number (default is 65535)</param>
        /// <returns>An open port number</returns>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the operation is in an error state</exception>
        public int NextOpenPort(int From = 49152, int To = 65535)
        {
            CSPRng rnd = new CSPRng();

            int port = -1;

            do
            {
                if (IsPortOpen((port = rnd.Next(From, To))))
                    break;

            } while (true);

            return port;
        }
        #endregion

        #region Connect
        /// <summary>
        /// Close the Client connection
        /// </summary>
        public void Close()
        {
            try
            {
                if (_cltSocket.Connected)
                {
                    _cltSocket.Close(1);
                    _opDone.WaitOne(10);
                }
            }
            catch (SocketException)
            {
            }
        }

        /// <summary>
        /// Start Blocking connect to a remote host
        /// </summary>
        /// 
        /// <param name="HostName">The remote machines Host Name</param>
        /// <param name="Port">The remote machines Port number</param>
        /// <param name="Timeout">The connection time out in milliseconds (default is 5 seconds)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp connect operation has failed</exception>
        public void Connect(string HostName, int Port, int Timeout = 5000)
        {
            IPHostEntry host;
            IPAddress[] ipList;
            IPAddress ip;

            try
            {
                // address of the host
                host = Dns.GetHostEntry(HostName);
                ipList = host.AddressList;
                ip = ipList[ipList.Length - 1];
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:Connect", "The Tcp connect operation has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }

            Connect(ip, Port, Timeout);
        }

        /// <summary>
        /// Start Blocking connect to a remote host
        /// </summary>
        /// 
        /// <param name="Address">The remote machines IP address</param>
        /// <param name="Port">The remote machines Port number</param>
        /// <param name="Timeout">The connection time out in milliseconds (default is 5 seconds)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp connect operation has failed</exception>
        public void Connect(IPAddress Address, int Port, int Timeout = 5000)
        {
            try
            {
                _isServer = false;
                IPEndPoint remEP = new IPEndPoint(Address, Port);
                _cltSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // set timeouts and buffer sizes
                SetClientParams();
                // wait for the connection
                IAsyncResult result = _cltSocket.BeginConnect(remEP, null, null);

                if (result.AsyncWaitHandle.WaitOne(Timeout, true))
                {
                    _cltSocket.EndConnect(result);
                }
                else
                {
                    // connection timed out
                    _cltSocket.Close();
                    throw new SocketException((int)SocketError.TimedOut);
                }

                // get the socket for the accepted client connection and put it into the ReadEventArg object user token
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs();
                // store the socket
                readEventArgs.UserToken = _cltSocket;

                if (Connected != null)
                    Connected(this, readEventArgs);
            }
            catch (SocketException se)
            {
                if (_cltSocket != null)
                    _cltSocket.Close();

                throw new CryptoSocketException("TcpSocket:Connect", "The Tcp connect operation has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Start Non-Blocking connect to a remote host
        /// </summary>
        /// 
        /// <param name="HostName">The remote machines Host Name</param>
        /// <param name="Port">The remote machines Port number</param>
        /// <param name="Timeout">The connection time out in milliseconds (default is 5 seconds)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp connect operation has failed</exception>
        public void ConnectAsync(string HostName, int Port, int Timeout = 5000)
        {
            IPHostEntry host;
            IPAddress[] ipList;
            IPAddress ip;
            
            try
            {
                host = Dns.GetHostEntry(HostName);
                ipList = host.AddressList;
                ip = ipList[ipList.Length - 1];
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:ConnectAsync", "The Tcp connect operation has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }

            ConnectAsync(ip, Port, Timeout);
        }

        /// <summary>
        /// Start Non-Blocking connect to a remote host
        /// </summary>
        /// 
        /// <param name="Address">The remote machines IP address</param>
        /// <param name="Port">The remote machines Port number</param>
        /// <param name="Timeout">The connection time out in milliseconds (default is 5 seconds)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp connect operation has failed</exception>
        public void ConnectAsync(IPAddress Address, int Port, int Timeout = 5000)
        {
            try
            {
                _isServer = false;
                IPEndPoint remEP = new IPEndPoint(Address, Port);
                _cltSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // set timeouts and buffer sizes
                SetClientParams();
                // connect to the remote endpoint.
                _cltSocket.BeginConnect(remEP, new AsyncCallback(ConnectCallback), _cltSocket);
                // block and wait
                _opDone.WaitOne(Timeout);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:ConnectAsync", "The Tcp connect operation attempt has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// The async connect callback
        /// </summary>
        private void ConnectCallback(IAsyncResult Ar)
        {
            // Retrieve the socket from the state object.
            Socket clt = (Socket)Ar.AsyncState;

            try
            {
                // complete the connection.
                clt.EndConnect(Ar);
                // get the socket for the accepted client connection and put it into the ReadEventArg object user token
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs();

                // store the socket
                readEventArgs.UserToken = _cltSocket;
                if (Connected != null)
                    Connected(this, readEventArgs);

                // signal that the connection has been made
                if (_opDone != null)
                    _opDone.Set();
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:ConnectCallback", "The Tcp connect operation has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.ConnectionAborted);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void SetClientParams()
        {
            _cltSocket.ReceiveBufferSize = ReceiveBufferSize;
            _cltSocket.ReceiveTimeout = ReceiveTimeout;
            _cltSocket.SendBufferSize = SendBufferSize;
            _cltSocket.SendTimeout = SendTimeout;
        }
        #endregion

        #region Listen
        /// <summary>
        /// Start Blocking listen on a port for an incoming connection
        /// </summary>
        /// 
        /// <param name="HostName">The Host Name assigned to this server</param>
        /// <param name="Port">The Port number assigned to this server</param>
        /// <param name="MaxConnections">The maximum number of connections</param>
        /// <param name="Timeout">The wait timeout period</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp listen operation has failed</exception>
        public void Listen(string HostName, int Port, int MaxConnections = 10, int Timeout = Timeout.Infinite)
        {
            IPHostEntry host;
            IPAddress[] ipList;
            IPAddress ip;

            try
            {
                // address of the host
                host = Dns.GetHostEntry(HostName);
                ipList = host.AddressList;
                ip = ipList[ipList.Length - 1];
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:Listen", "The Tcp listener has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }

            Listen(ip, Port, MaxConnections);
        }

        /// <summary>
        /// Start Blocking listen on a port for an incoming connection
        /// </summary>
        /// 
        /// <param name="Address">The IP address assigned to this server</param>
        /// <param name="Port">The Port number assigned to this server</param>
        /// <param name="MaxConnections">The maximum number of connections</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp listen operation has failed</exception>
        public void Listen(IPAddress Address, int Port, int MaxConnections = 10)
        {
            try
            {
                _isServer = true;
                IPEndPoint ipEP = new IPEndPoint(Address, Port);
                _lsnSocket = new Socket(ipEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                if (ipEP.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _lsnSocket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _lsnSocket.Bind(ipEP);
                }
                else
                {
                    // associate the socket with the local endpoint
                    _lsnSocket.Bind(ipEP);
                }

                _isListening = true;
                // accept the incoming client
                _lsnSocket.Listen(MaxConnections);
                // assign client and stream
                _cltSocket = _lsnSocket.Accept();
                _isListening = false;

                // get the socket for the accepted client connection and put it into the ReadEventArg object user token
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs();
                // store the socket
                readEventArgs.UserToken = _cltSocket;

                if (Connected != null)
                    Connected(this, readEventArgs);

                // not sure why this is needed..
                _opDone.WaitOne(100);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:Listen", "The Tcp listener has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Start Non-Blocking listen on a port for an incoming connection
        /// </summary>
        /// 
        /// <param name="HostName">The Host Name assigned to this server</param>
        /// <param name="Port">The Port number assigned to this server</param>
        /// <param name="MaxConnections">The maximum number of connections</param>
        /// <param name="Timeout">The wait timeout period</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp listen operation has failed</exception>
        public void ListenAsync(string HostName, int Port, int MaxConnections = 10, int Timeout = Timeout.Infinite)
        {
            IPHostEntry host;
            IPAddress[] ipList;
            IPAddress ip;

            try
            {
                host = Dns.GetHostEntry(HostName);
                ipList = host.AddressList;
                ip = ipList[ipList.Length - 1];
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:Listen", "The Tcp listener has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }

            ListenAsync(ip, Port, MaxConnections, Timeout);
        }

        /// <summary>
        /// Start Non-Blocking listen on a port for an incoming connection
        /// </summary>
        /// 
        /// <param name="Address">The IP address assigned to this server</param>
        /// <param name="Port">The Port number assigned to this server</param>
        /// <param name="MaxConnections">The maximum number of simultaneous connections allowed (default is 10)</param>
        /// <param name="Timeout">The wait timeout period</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp listen operation has failed</exception>
        public void ListenAsync(IPAddress Address, int Port, int MaxConnections = 10, int Timeout = Timeout.Infinite)
        {
            try
            {
                _isServer = true;
                IPEndPoint ipEP = new IPEndPoint(Address, Port);
                _lsnSocket = new Socket(ipEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                if (ipEP.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _lsnSocket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
                    _lsnSocket.Bind(ipEP);
                }
                else
                {
                    // associate the socket with the local endpoint
                    _lsnSocket.Bind(ipEP);
                }

                _isListening = true;
                _lsnSocket.Listen(MaxConnections);
                // create the state object.
                StateToken state = new StateToken(_lsnSocket);
                // accept the incoming clients
                _lsnSocket.BeginAccept(new AsyncCallback(ListenCallback), state);
                // blocks the current thread to receive incoming messages
                _opDone.WaitOne(Timeout);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:ListenAsync", "The Tcp listener has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// The Listen callback
        /// </summary>
        /// 
        /// <param name="Ar">The IAsyncResult</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp listen operation has failed</exception>
        private void ListenCallback(IAsyncResult Ar)
        {
            // retrieve the state object and the client socket from the asynchronous state object
            StateToken state = (StateToken)Ar.AsyncState;
            Socket srv = state.Client;

            try
            {
                // get the socket for the accepted client connection and put it into the ReadEventArg object user token
                _cltSocket = srv.EndAccept(Ar);

                // store the socket
                SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs();
                readEventArgs.UserToken = _cltSocket;

                if (Connected != null)
                    Connected(this, readEventArgs);

                _isListening = false;
                // signal done
                _opDone.Set();
                // once the client connects then start receiving the commands
                StateToken cstate = new StateToken(_cltSocket);
                // start receiving
                _cltSocket.BeginReceive(cstate.Buffer, 0, cstate.Buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), cstate);     
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.ConnectionAborted);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:ListenCallback", "The Tcp listener has failed!", se);
            }
            catch (Exception)
            {
                if (_isListening)
                    throw;
            }
        }

        /// <summary>
        /// Stop listening for a connection
        /// </summary>
        public void ListenStop()
        {
            try
            {
                _isServer = false;
                _lsnSocket.Close(1);
                _opDone.WaitOne(10);
                _isListening = false;
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:ListenStop", "The Tcp listen stop had an error!", se);
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region Receive
        /// <summary>
        /// Begin Non-Blocking receiver of incoming messages (called after a connection is made)
        /// </summary>
        /// 
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp receive operation has failed</exception>
        public void ReceiveAsync(SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                // once the client connects then start receiving the commands
                StateToken cstate = new StateToken(_cltSocket, _cltSocket.ReceiveBufferSize);
                _cltSocket.BeginReceive(cstate.Buffer, 0, cstate.Buffer.Length, SocketFlag, new AsyncCallback(ReceiveCallback), cstate);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:ReceiveAsync", "The Tcp receiver has failed!", se);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// The ReceiveAsync callback
        /// </summary>
        /// 
        /// <param name="Ar">The IAsyncResult</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown on socket error or if the Tcp stream is larger than the maximum allocation size</exception>
        private void ReceiveCallback(IAsyncResult Ar)
        {
            // Retrieve the state object and the client socket from the asynchronous state object
            StateToken state = (StateToken)Ar.AsyncState;
            Socket clt = state.Client;

            try
            {
                // Read data from the remote device.
                int bytesRead = clt.EndReceive(Ar);

                if (bytesRead > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:ReceiveCallback", string.Format("The Tcp stream is larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());

                if (bytesRead > 0)
                {
                    // there might be more data, so store the data received so far
                    state.Data.Write(state.Buffer, 0, bytesRead);
                    // all the data has arrived; put it in response.
                    if (state.Data.Length > 1)
                    {
                        // return the data
                        if (DataReceived != null)
                        {
                            state.Data.Seek(0, SeekOrigin.Begin);
                            DataReceivedEventArgs args = new DataReceivedEventArgs(state, 0);
                            DataReceived(args);
                        }
                    }

                    if (state.Client.Connected)
                    {
                        // clear the state
                        state.Data = new MemoryStream();
                        // get more data
                        clt.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                }
            }
            catch (SocketException se)
            {
                // expected when remote connection goes down
                if (se.ErrorCode != (int)SocketError.ConnectionReset && se.ErrorCode != (int)SocketError.Disconnecting)
                    throw new CryptoSocketException("TcpSocket:ReceiveCallback", "The Tcp receiver has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (NullReferenceException)
            {
                // expected when shutting down
            }
            catch
            {
                throw;
            }
        }
        #endregion

        #region Send
        /// <summary>
        /// Blocking transmission of a byte array to the remote host
        /// </summary>
        /// 
        /// <param name="DataArray">The data byte array to send</param>
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp Send operation has failed, or the maximum allocation size is exceeded</exception>
        public void Send(byte[] DataArray, SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                if (DataArray.Length > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:Send", string.Format("The Data bytes are larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());
                
                // send the data to the remote device
                _cltSocket.Send(DataArray, SocketFlag);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:Send", "The Tcp Send operation has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Blocking transmission of a stream to the remote host
        /// </summary>
        /// 
        /// <param name="DataStream">The data stream to send</param>
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp Send operation has failed, or the maximum allocation size is exceeded</exception>
        public void Send(MemoryStream DataStream, SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                if (DataStream.Length > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:Send", string.Format("The Data bytes are larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());

                // send the data to the remote device
                _cltSocket.Send(DataStream.ToArray(), SocketFlag);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:Send", "The Tcp Send operation has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Blocking transmission of a byte array to the remote host
        /// </summary>
        /// 
        /// <param name="DataArray">The data byte array to send</param>
        /// <param name="Offset">The start position within the buffer array</param>
        /// <param name="Size">The number of bytes to send</param>
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp Send operation has failed, or the maximum allocation size is exceeded</exception>
        public void Send(byte[] DataArray, int Offset, int Size, SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                if (DataArray.Length > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:Send", string.Format("The Data bytes are larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());

                // send the data to the remote device
                _cltSocket.Send(DataArray, Offset, Size, SocketFlag);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:Send", "The Tcp Send operation has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Blocking transmission of a stream to the remote host
        /// </summary>
        /// 
        /// <param name="DataStream">The data stream to send</param>
        /// <param name="Offset">The start position within the stream</param>
        /// <param name="Size">The number of bytes to send</param>
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp Send operation has failed, or the maximum allocation size is exceeded</exception>
        public void Send(MemoryStream DataStream, int Offset, int Size, SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                if (DataStream.Length > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:Send", string.Format("The Data bytes are larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());

                // send the data to the remote device
                _cltSocket.Send(DataStream.ToArray(), Offset, Size, SocketFlag);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:Send", "The Tcp Send operation has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Non-Blocking transmission of a byte array to the remote host
        /// </summary>
        /// 
        /// <param name="DataArray">The byte array data to send</param>
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp Send operation has failed, or the maximum allocation size is exceeded</exception>
        public void SendAsync(byte[] DataArray, SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                if (DataArray.Length > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:SendAsync", string.Format("The Data bytes are larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());
                
                // begin sending the data to the remote device
                _cltSocket.BeginSend(DataArray, 0, DataArray.Length, SocketFlag, new AsyncCallback(SendCallback), _cltSocket);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:SendAsync", "The Tcp Send has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Non-Blocking transmission of a stream to the remote host
        /// </summary>
        /// 
        /// <param name="DataStream">The stream data to send</param>
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp Send operation has failed, or the maximum allocation size is exceeded</exception>
        public void SendAsync(MemoryStream DataStream, SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                if (DataStream.Length > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:SendAsync", string.Format("The Data bytes are larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());

                // begin sending the data to the remote device
                _cltSocket.BeginSend(DataStream.ToArray(), 0, (int)DataStream.Length, SocketFlag, new AsyncCallback(SendCallback), _cltSocket);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:SendAsync", "The Tcp Send has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Non-Blocking transmission of a byte array to the remote host
        /// </summary>
        /// 
        /// <param name="DataArray">The byte array data to send</param>
        /// <param name="Offset">The start position within the buffer</param>
        /// <param name="Size">The number of bytes to send</param>
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp Send operation has failed, or the maximum allocation size is exceeded</exception>
        public void SendAsync(byte[] DataArray, int Offset, int Size, SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                if (DataArray.Length > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:SendAsync", string.Format("The Data bytes are larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());

                // begin sending the data to the remote device
                _cltSocket.BeginSend(DataArray, Offset, Size, SocketFlag, new AsyncCallback(SendCallback), _cltSocket);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:SendAsync", "The Tcp Send has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Non-Blocking transmission of stream data to the remote host
        /// </summary>
        /// 
        /// <param name="DataStream">The data stream to send</param>
        /// <param name="Offset">The start position within the stream</param>
        /// <param name="Size">The number of bytes to send</param>
        /// <param name="SocketFlag">The bitwise combination of Socket Flags (default is None)</param>
        /// 
        /// <exception cref="CryptoSocketException">Thrown if the Tcp Send operation has failed, or the maximum allocation size is exceeded</exception>
        public void SendAsync(MemoryStream DataStream, int Offset, int Size, SocketFlags SocketFlag = SocketFlags.None)
        {
            try
            {
                if (DataStream.Length > MaxAllocation)
                    throw new CryptoSocketException("TcpSocket:SendAsync", string.Format("The Data bytes are larger than the maximum allocation size of {0} bytes!", MaxAllocation), new InvalidOperationException());

                // begin sending the data to the remote device
                _cltSocket.BeginSend(DataStream.ToArray(), Offset, Size, SocketFlag, new AsyncCallback(SendCallback), _cltSocket);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:SendAsync", "The Tcp Send has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// The Send callback
        /// </summary>
        /// 
        /// <param name="Ar">the IAsyncResult</param>
        private void SendCallback(IAsyncResult Ar)
        {
            try
            {
                // retrieve the socket from the state object
                Socket clt = (Socket)Ar.AsyncState;
                if (!clt.Connected)
                    return;

                // complete sending the data to the remote device
                int bytesSent = clt.EndSend(Ar);
            }
            catch (SocketException se)
            {
                throw new CryptoSocketException("TcpSocket:SendCallback", "The Tcp Send has failed!", se);
            }
            catch (ObjectDisposedException)
            {
                // disconnected
                if (DisConnected != null)
                    DisConnected(this, SocketError.NotConnected);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Synchronous file transmission operation
        /// </summary>
        /// 
        /// <param name="FilePath">The full path to the file</param>
        public void SendFile(string FilePath)
        {
            // establish the local endpoint for the socket
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 11000);

            // create a TCP socket.
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint
            client.Connect(ipEndPoint);

            // send file fileName to remote device
            client.SendFile(FilePath);

            // release the socket
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        #endregion

        #region IDispose
        /// <summary>
        /// Dispose of this class
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool Disposing)
        {
            if (!_isDisposed && Disposing)
            {
                try
                {
                    if (_lsnSocket != null)
                    {
                        if (_lsnSocket.Connected)
                        {
                            _lsnSocket.Shutdown(SocketShutdown.Both);
                            _lsnSocket.Close();
                        }
                        _lsnSocket.Dispose();
                        _lsnSocket = null;
                    }
                    if (_cltSocket != null)
                    {
                        if (_cltSocket.Connected)
                        {
                            _cltSocket.Shutdown(SocketShutdown.Both);
                            _cltSocket.Close();
                        }
                        _cltSocket.Dispose();
                        _cltSocket = null;
                    }
                    if (_opDone != null)
                    {
                        _opDone.Close();
                        _opDone.Dispose();
                        _opDone = null;
                    }
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }
        #endregion
    }
}
