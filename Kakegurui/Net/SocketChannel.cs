using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Kakegurui.Core;
using Kakegurui.Log;
using Microsoft.Extensions.Logging;

namespace Kakegurui.Net
{
    /// <summary>
    /// 套接字发送结果
    /// </summary>
    public enum SocketResult
    {
        /// <summary>
        /// 发送成功
        /// </summary>
        Success = 0,
        /// <summary>
        /// 发送失败
        /// </summary>
        SendFailed = 1,
        /// <summary>
        /// 发送超时
        /// </summary>
        Timeout = 2,
        /// <summary>
        /// 未找到套接字
        /// </summary>
        NotFoundSocket = 3,
        /// <summary>
        /// 尚未连接到服务
        /// </summary>
        Disconnection = 4
    }

    /// <summary>
    /// 套接字类型
    /// </summary>
    public enum SocketType
    {
        /// <summary>
        /// Tcp监听
        /// </summary>
        Listen,
        /// <summary>
        /// Tcp客户端
        /// </summary>
        Accept,
        /// <summary>
        /// Tcp服务端
        /// </summary>
        Connect,
        /// <summary>
        /// Udp服务
        /// </summary>
        Udp_Server,
        /// <summary>
        /// udp客户端
        /// </summary>
        Udp_Client
    }

    /// <summary>
    /// 接受客户端连入事件参数
    /// </summary>
    public class AcceptedEventArgs : EventArgs
    {
        public Socket Socket { get; set; }
        public SocketChannel Channel { get; set; }
    }

    /// <summary>
    /// 接受客户端连入事件参数
    /// </summary>
    public class ConnectedEventArgs : EventArgs
    {
        public Socket Socket { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
    }

    /// <summary>
    /// 接受客户端连入事件参数
    /// </summary>
    public class ReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 套接字
        /// </summary>
        public Socket Socket { get; set; }

        /// <summary>
        /// udp远程地址
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; set; }

        /// <summary>
        /// 套接字通道
        /// </summary>
        public SocketChannel Channel { get; set; }

        /// <summary>
        /// 字节流
        /// </summary>
        public List<byte> Buffer { get; set; }

        /// <summary>
        /// 发送协议编号
        /// </summary>
        public ushort ProtocolId { get; set; }

        /// <summary>
        /// 发送时间戳
        /// </summary>
        public long TimeStamp { get; set; }

    }

    /// <summary>
    /// 套接字关闭事件参数
    /// </summary>
    public class ClosedEventArgs : EventArgs
    {
        public Socket Socket { get; set; }
    }

    /// <summary>
    /// 套接字通道
    /// </summary>
    public class SocketChannel
    {
        /// <summary>
        /// 一次等待响应
        /// </summary>
        private class ResponseItem
        {
            /// <summary>
            /// 响应匹配函数
            /// </summary>
            public Func<ReceivedEventArgs, bool> Match { get; set; }
            /// <summary>
            /// 成功后的回调
            /// </summary>
            public Action<ReceivedEventArgs> Action { get; set; }
            /// <summary>
            /// 响应字节流
            /// </summary>
            public List<byte> ReceiveBuffer { get; set; }
            /// <summary>
            /// 成功通知
            /// </summary>
            public AutoResetEvent ResetEvent { get; set; }
            /// <summary>
            /// 超时时间戳
            /// </summary>
            public long TimeStamp { get; set; }
        }

        /// <summary>
        /// 缓冲容量
        /// </summary>
        private const int BufferLength = 65536;

        /// <summary>
        /// 客户端断线重连时间(毫秒)
        /// </summary>
        private const int ConnectionSpan = 10 * 1000;

        /// <summary>
        /// 日志接口
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// 套接字包处理实例
        /// </summary>
        private readonly ISocketHandler _handler;

        /// <summary>
        /// 表示是否调用过Close方法
        /// </summary>
        private bool _closed;

        /// <summary>
        /// 残包
        /// </summary>
        private readonly List<byte> _residueBuffer = new List<byte>();

        /// <summary>
        /// 同步锁
        /// </summary>
        private readonly object _lockObj = new object();

        /// <summary>
        /// 当前等待响应的集合
        /// </summary>
        private readonly LinkedList<ResponseItem> _responseItems = new LinkedList<ResponseItem>();

        /// <summary>
        /// 套接字
        /// </summary>
        public Socket Socket { get; private set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; }

        /// <summary>
        /// 套接字类型
        /// </summary>
        public SocketType Type { get; }

        /// <summary>
        /// 套接字标识 accept
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// 套接字远程地址
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// 套接字本地地址
        /// </summary>
        public IPEndPoint LocalEndPoint { get; private set; }

        /// <summary>
        /// 发送总字节数
        /// </summary>
        public ulong TransmitSize { get; private set; }

        /// <summary>
        /// 接收总字节数
        /// </summary>
        public ulong ReceiveSize { get; private set; }

        /// <summary>
        /// 客户端连入事件
        /// </summary>
        public event EventHandler<AcceptedEventArgs> Accepted;

        /// <summary>
        /// 连入到服务事件
        /// </summary>
        public event EventHandler<ConnectedEventArgs> Connected;

        /// <summary>
        /// 接收字节流事件
        /// </summary>
        public event EventHandler<ReceivedEventArgs> Received;

        /// <summary>
        /// 关闭套接字事件
        /// </summary>
        public event EventHandler<ClosedEventArgs> Closed;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="acceptSocket">客户端连入套接字</param>
        /// <param name="type">套接字类型</param>
        /// <param name="localEndPoint">套接字本地地址</param>
        /// <param name="remoteEndPoint">套接字远程地址</param>
        /// <param name="handler">套接字处理实例</param>
        public SocketChannel(Socket acceptSocket, SocketType type, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, ISocketHandler handler)
        {
            Type = type;
            StartTime = DateTime.Now;
            TransmitSize = 0;
            ReceiveSize = 0;
            _handler = handler;

            if (type == SocketType.Listen)
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    socket.Bind(localEndPoint);
                    socket.Listen(10);
                }
                catch (SocketException ex)
                {
                    socket.Close();
                    LogPool.Logger.LogError((int)LogEvent.套接字, ex, $"监听端口错误 {localEndPoint}");
                    return;
                }
                Socket = socket;
                LocalEndPoint = localEndPoint;
                RemoteEndPoint = null;
                Tag = null;
                LogPool.Logger.LogInformation((int)LogEvent.套接字, $"listen {socket.Handle} {localEndPoint}");
                SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += AcceptedEventHandler;
                if (!socket.AcceptAsync(acceptArgs))
                {
                    AcceptedEventHandler(socket, acceptArgs);
                }
            }
            else if (type == SocketType.Accept)
            {
                if (acceptSocket == null)
                {
                    return;
                }
                Socket = acceptSocket;
                LocalEndPoint = localEndPoint;
                RemoteEndPoint = remoteEndPoint;
                Tag = localEndPoint.Port.ToString();
                SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.Completed += ReceivedHandler;
                receiveArgs.SetBuffer(new byte[BufferLength], 0, BufferLength);
                receiveArgs.AcceptSocket = acceptSocket;
                if (!acceptSocket.ReceiveAsync(receiveArgs))
                {
                    ReceivedHandler(acceptSocket, receiveArgs);
                }
            }
            else if (type == SocketType.Udp_Server || type == SocketType.Udp_Client)
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, ProtocolType.Udp);
                try
                {
                    socket.Bind(localEndPoint);
                }
                catch (SocketException ex)
                {
                    socket.Close();
                    LogPool.Logger.LogError((int)LogEvent.套接字, ex, $"绑定端口错误 {socket.Handle}");
                    return;
                }

                Socket = socket;
                LocalEndPoint = localEndPoint;
                RemoteEndPoint = null;
                Tag = localEndPoint.ToString();
                LogPool.Logger.LogInformation((int)LogEvent.套接字, $"套接字类型:{type} 套接字:{socket.Handle} 本地地址:{localEndPoint}");
                SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.Completed += ReceivedHandler;
                receiveArgs.SetBuffer(new byte[BufferLength], 0, BufferLength);
                receiveArgs.RemoteEndPoint = localEndPoint;
                if (!socket.ReceiveFromAsync(receiveArgs))
                {
                    ReceivedHandler(socket, receiveArgs);
                }
            }
            else if (type == SocketType.Connect)
            {
                RemoteEndPoint = remoteEndPoint;
                Tag = remoteEndPoint.ToString();
                LogPool.Logger.LogInformation((int)LogEvent.套接字, $"connect {remoteEndPoint}");
                ConnectAsync(remoteEndPoint);
            }
        }

        /// <summary>
        /// 关闭通道
        /// </summary>
        public void Close()
        {
            _closed = true;
            LogPool.Logger.LogInformation((int)LogEvent.套接字,
                $"active_close {Socket.Handle} {Type} {LocalEndPoint} {RemoteEndPoint} {Tag} {StartTime:yyyy-MM-dd HH:mm:ss.fff} {TransmitSize} {ReceiveSize}");
            Socket?.Close();
        }

        /// <summary>
        /// 客户端连入事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptedEventHandler(object sender, SocketAsyncEventArgs e)
        {
            Socket listenSocket = (Socket)sender;
            SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
            acceptArgs.Completed += AcceptedEventHandler;
            try
            {
                if (!listenSocket.AcceptAsync(acceptArgs))
                {
                    AcceptedEventHandler(listenSocket, acceptArgs);
                }
                LogPool.Logger.LogInformation((int)LogEvent.套接字, $"accepted { e.AcceptSocket.Handle} {(IPEndPoint)e.AcceptSocket.LocalEndPoint} {(IPEndPoint)e.AcceptSocket.RemoteEndPoint}");
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            SocketChannel channel = new SocketChannel(
                e.AcceptSocket,
                SocketType.Accept,
                (IPEndPoint)e.AcceptSocket.LocalEndPoint,
                (IPEndPoint)e.AcceptSocket.RemoteEndPoint,
                _handler);
            channel.Received += Received;
            Accepted?.Invoke(this, new AcceptedEventArgs
            {
                Socket = e.AcceptSocket,
                Channel = channel
            });
        }

        /// <summary>
        /// 异步连接到服务器
        /// </summary>
        /// <param name="remoteEndPoint">远程地址</param>
        private void ConnectAsync(IPEndPoint remoteEndPoint)
        {
            Socket socket = new Socket(
                AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.Completed += ConnectedEventHandler;
            connectArgs.RemoteEndPoint = remoteEndPoint;
            if (!socket.ConnectAsync(connectArgs))
            {
                ConnectedEventHandler(socket, connectArgs);
            }
        }

        /// <summary>
        /// 连接到服务事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectedEventHandler(object sender, SocketAsyncEventArgs e)
        {
            if (e.ConnectSocket == null)
            {
                Thread.Sleep(ConnectionSpan);
                ConnectAsync((IPEndPoint)e.RemoteEndPoint);
            }
            else
            {
                LogPool.Logger.LogInformation((int)LogEvent.套接字, $"connected {e.ConnectSocket.Handle} {(IPEndPoint)e.ConnectSocket.LocalEndPoint} {RemoteEndPoint}");
                _residueBuffer.Clear();
                Socket = e.ConnectSocket;
                LocalEndPoint = (IPEndPoint)e.ConnectSocket.LocalEndPoint;
                SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.Completed += ReceivedHandler;
                receiveArgs.SetBuffer(new byte[BufferLength], 0, BufferLength);
                receiveArgs.AcceptSocket = e.ConnectSocket;
                if (!e.ConnectSocket.ReceiveAsync(receiveArgs))
                {
                    ReceivedHandler(e.ConnectSocket, receiveArgs);
                }

                Connected?.Invoke(this, new ConnectedEventArgs
                {
                    RemoteEndPoint = RemoteEndPoint,
                    Socket = e.ConnectSocket
                });
            }
        }

        /// <summary>
        /// 接收事件函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceivedHandler(object sender, SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                try
                {
                    if (Type == SocketType.Udp_Server || Type == SocketType.Udp_Client)
                    {
                        Handle(e.Buffer, e.BytesTransferred, (IPEndPoint)e.RemoteEndPoint);
                        if (!Socket.ReceiveFromAsync(e))
                        {
                            ReceivedHandler(Socket, e);
                        }
                    }
                    else
                    {
                        Handle(e.Buffer, e.BytesTransferred);
                        if (!Socket.ReceiveAsync(e))
                        {
                            ReceivedHandler(Socket, e);
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }
            else
            {
                if (!_closed)
                {
                    LogPool.Logger.LogInformation((int)LogEvent.套接字,
                        $"passive_close {Socket.Handle} {Type} {LocalEndPoint} {RemoteEndPoint} {Tag} {StartTime:yyyy-MM-dd HH:mm:ss.fff} {TransmitSize} {ReceiveSize}");
                    Closed?.Invoke(this, new ClosedEventArgs
                    {
                        Socket = Socket
                    });
                    Socket.Close();
                    if (Type == SocketType.Connect)
                    {
                        Socket = null;
                        LocalEndPoint = null;
                        //在和tomcat中对接的时候发现在tomcat启动和
                        //结束的时候，服务会释放连入的连接
                        //但是此时服务还可以连接，就造成了短时间内的多次连接
                        Thread.Sleep(ConnectionSpan);
                        ConnectAsync(RemoteEndPoint);
                    }
                }
            }
        }

        /// <summary>
        /// 设置日志
        /// </summary>
        /// <param name="logName">日志名</param>
        public void SetLogger(string logName)
        {
            _logger = new FileLogger(LogLevel.Debug,LogLevel.Error,logName, LogPool.Directory, LogPool.HoldDays);
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="message">日志格式</param>
        private void Log(string message)
        {
            LogPool.Logger.LogDebug((int)LogEvent.字节流, message);
            _logger?.LogInformation((int)LogEvent.字节流, message);
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="remoteEndPoint">udp远程地址，如果为null表示tcp发送</param>
        /// <param name="buffer">字节流</param>
        /// <param name="match">响应匹配函数，不使用匹配标识不等待响应</param>
        /// <param name="action">成功响应后的异步回调，默认为null，表示同步等待响应</param>
        /// <param name="receiveBuffer">同步等待响应后得到的响应字节流，默认为null</param>
        /// <param name="timeout">等待响应时间，默认为3秒</param>
        /// <returns>发送结果</returns>
        public SocketResult Send(IPEndPoint remoteEndPoint, List<byte> buffer, Func<ReceivedEventArgs, bool> match = null, Action<ReceivedEventArgs> action = null, List<byte> receiveBuffer = null, int timeout = 3000)
        {
            if (match == null)
            {
                return SendCore(remoteEndPoint, buffer);
            }
            else
            {
                long timeStamp = TimeStampConvert.ToUtcTimeStamp();
                if (action == null)
                {
                    AutoResetEvent resetEvent = new AutoResetEvent(false);
                    lock (_lockObj)
                    {
                        _responseItems.AddLast(new ResponseItem
                        {
                            Match = match,
                            Action = null,
                            ReceiveBuffer = receiveBuffer,
                            ResetEvent = resetEvent,
                            TimeStamp = timeStamp + timeout
                        });
                    }

                    SocketResult result = SendCore(remoteEndPoint, buffer);
                    if (result == SocketResult.Success)
                    {
                        result = resetEvent.WaitOne(timeout) ? SocketResult.Success : SocketResult.Timeout;
                    }
                    return result;
                }
                else
                {
                    lock (_lockObj)
                    {
                        _responseItems.AddLast(new ResponseItem
                        {
                            Match = match,
                            Action = action,
                            ReceiveBuffer = receiveBuffer,
                            ResetEvent = null,
                            TimeStamp = timeStamp + timeout
                        });
                    }
                    return SendCore(remoteEndPoint, buffer);
                }
            }
        }

        /// <summary>
        /// 发送字节流
        /// </summary>
        /// <param name="remoteEndPoint">udp远程地址，如果为null表示tcp发送</param>
        /// <param name="buffer">字节流</param>
        /// <returns>发送结果</returns>
        private SocketResult SendCore(IPEndPoint remoteEndPoint, List<byte> buffer)
        {
            if (Socket == null)
            {
                return SocketResult.Disconnection;
            }

            if (buffer == null || buffer.Count == 0)
            {
                return SocketResult.SendFailed;
            }
            byte[] temp = buffer.ToArray();
            TransmitSize += Convert.ToUInt32(temp.Length);
            try
            {
                if (remoteEndPoint == null
                    || remoteEndPoint.Address.Equals(IPAddress.Any) && remoteEndPoint.Port == 0)
                {
                    Log($"{Socket.Handle} - {temp.Length} {ByteConvert.ToHex(temp)}");
                    int written = 0;
                    while (written != temp.Length)
                    {
                        int n;
                        if ((n = Socket.Send(temp, written, temp.Length - written, SocketFlags.None)) <= 0)
                        {
                            return SocketResult.SendFailed;
                        }
                        written += n;
                    }
                }
                else
                {
                    Log($"{Socket.Handle} {remoteEndPoint} - {temp.Length} {ByteConvert.ToHex(temp)}");
                    int written = 0;
                    while (written != temp.Length)
                    {
                        int n;
                        if ((n = Socket.SendTo(temp, written, temp.Length - written, SocketFlags.None, remoteEndPoint)) <= 0)
                        {
                            return SocketResult.SendFailed;
                        }
                        written += n;
                    }
                }
                return SocketResult.Success;
            }
            catch (SocketException)
            {
                return SocketResult.SendFailed;
            }
            catch (ObjectDisposedException)
            {
                return SocketResult.SendFailed;
            }
        }

        /// <summary>
        /// 处理接收字节流
        /// </summary>
        /// <param name="buffer">字节流</param>
        /// <param name="size">字节流长度</param>
        /// <param name="remoteEndPoint">远程地址</param>
        public void Handle(byte[] buffer, int size, IPEndPoint remoteEndPoint = null)
        {
            ReceiveSize += Convert.ToUInt32(size);

            Log(remoteEndPoint == null
                ? $"{Socket.Handle} + {size} {ByteConvert.ToHex(buffer, size)}"
                : $"{Socket.Handle} {remoteEndPoint} + {size} {ByteConvert.ToHex(buffer, size)}");

            _residueBuffer.AddRange(buffer.Take(size));

            int offset = 0;
            do
            {
                SocketPack packet = _handler.Unpack(Socket, remoteEndPoint, _residueBuffer, offset);
                if (packet.Result == AnalysisResult.Full)
                {
                    ReceivedEventArgs args = new ReceivedEventArgs
                    {
                        Socket = Socket,
                        RemoteEndPoint = remoteEndPoint,
                        Channel = this,
                        Buffer = _residueBuffer.GetRange(packet.Offset, packet.Size),
                        ProtocolId = packet.ProtocolId,
                        TimeStamp = packet.TimeStamp
                    };
                    long timeStamp = TimeStampConvert.ToUtcTimeStamp();
                    lock (_lockObj)
                    {
                        LinkedListNode<ResponseItem> node = _responseItems.First;
                        while (node != null)
                        {
                            if (node.Value.TimeStamp >= timeStamp)
                            {
                                if (node.Value.Match(args))
                                {
                                    if (node.Value.Action == null)
                                    {
                                        node.Value.ReceiveBuffer?.AddRange(args.Buffer);
                                        node.Value.ResetEvent.Set();
                                    }
                                    else
                                    {
                                        node.Value.Action(args);
                                    }
                                    LinkedListNode<ResponseItem> nextNode = node.Next;
                                    _responseItems.Remove(node);
                                    node = nextNode;
                                }
                                else
                                {
                                    node = node.Next;
                                }
                            }
                            else
                            {
                                LinkedListNode<ResponseItem> nextNode = node.Next;
                                _responseItems.Remove(node);
                                node = nextNode;
                            }
                        }
                    }


                    Received?.Invoke(this, args);
                }
                else if (packet.Result == AnalysisResult.Half)
                {
                    _residueBuffer.RemoveRange(0, offset);
                    return;
                }
                offset += packet.Offset + packet.Size;
            } while (offset < _residueBuffer.Count);
            _residueBuffer.Clear();
        }

        public override string ToString()
        {
            return $"{Socket?.Handle} local:{(LocalEndPoint == null ? "-" : LocalEndPoint.ToString())} remote:{(RemoteEndPoint == null ? "-" : RemoteEndPoint.ToString())} tag:{Tag ?? "-"} t:{TransmitSize} r:{ReceiveSize} time:{StartTime:yyyy-MM-dd HH:mm:ss.fff}";
        }
    }
}
