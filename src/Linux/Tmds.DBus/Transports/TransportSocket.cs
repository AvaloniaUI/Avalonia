// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Transports
{
    using SizeT = System.UIntPtr;
    internal class TransportSocket
    {
        // Issue https://github.com/dotnet/corefx/issues/6807
        private static readonly PropertyInfo s_handleProperty = typeof(Socket).GetTypeInfo().GetDeclaredProperty("Handle");
        private static readonly PropertyInfo s_safehandleProperty = typeof(Socket).GetTypeInfo().GetDeclaredProperty("SafeHandle");

        const int SOL_SOCKET = 1;
        const int EINTR = 4;
        const int EBADF = 9;
        static readonly int EAGAIN = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 35 : 11;
        const int SCM_RIGHTS = 1;

        private unsafe struct msghdr
        {
            public IntPtr msg_name; //optional address
            public uint msg_namelen; //size of address
            public IOVector* msg_iov; //scatter/gather array
            public SizeT msg_iovlen; //# elements in msg_iov
            public void* msg_control; //ancillary data, see below
            public SizeT msg_controllen; //ancillary data buffer len
            public int msg_flags; //flags on received message
        }

        private unsafe struct IOVector
        {
            public void* Base;
            public SizeT Length;
        }

        private struct cmsghdr
        {
            public SizeT cmsg_len; //data byte count, including header
            public int cmsg_level; //originating protocol
            public int cmsg_type; //protocol-specific type
        }

        private unsafe struct cmsg_fd
        {
            public cmsghdr hdr;
            public fixed int fds[64];
        }

        private class ReadContext
        {
            public TaskCompletionSource<int> Tcs;
            public byte[] Buffer;
            public int Offset;
            public int Count;
            public List<UnixFd> FileDescriptors;
        }

        private class SendContext
        {
            public TaskCompletionSource<object> Tcs;
        }

        private int _socketFd;
        private readonly object _gate = new object();
        private readonly SocketAsyncEventArgs _waitForData;
        private readonly SocketAsyncEventArgs _receiveData;
        private readonly SocketAsyncEventArgs _sendArgs;
        private readonly List<ArraySegment<byte>> _bufferList = new List<ArraySegment<byte>>();
        private readonly Socket _socket;
        private bool _supportsFdPassing;

        public TransportSocket(Socket socket, bool supportsFdPassing)
        {
            _socket = socket;
            _socketFd = GetFd(socket);
            _supportsFdPassing = supportsFdPassing && _socketFd != -1;

            _waitForData = new SocketAsyncEventArgs();
            _waitForData.SetBuffer(Array.Empty<byte>(), 0, 0);
            _waitForData.Completed += DataAvailable;
            _waitForData.UserToken = new ReadContext();

            _receiveData = new SocketAsyncEventArgs();
            _receiveData.Completed += ReadCompleted;
            _receiveData.UserToken = new ReadContext();

            _sendArgs = new SocketAsyncEventArgs();
            _sendArgs.BufferList = new List<ArraySegment<byte>>();
            _sendArgs.UserToken = new SendContext();
            _sendArgs.Completed += SendCompleted;
        }

        public bool SupportsFdPassing { get => _supportsFdPassing; set { _supportsFdPassing = value; } }

        public void Dispose()
        {
            lock (_gate)
            {
                _socketFd = -1;
                _socket.Dispose();
            }
        }

        private void ReadCompleted(object sender, SocketAsyncEventArgs e)
        {
            var readContext = _receiveData.UserToken as ReadContext;
            var tcs = readContext.Tcs;
            readContext.Tcs = null;
            if (e.SocketError == SocketError.Success)
            {
                tcs.SetResult(e.BytesTransferred);
            }
            else
            {
                tcs.SetException(new SocketException((int)e.SocketError));
            }
        }

        private void DataAvailable(object sender, SocketAsyncEventArgs e)
        {
            var readContext = _waitForData.UserToken as ReadContext;
            int rv = DoRead(readContext.Buffer, readContext.Offset, readContext.Count, readContext.FileDescriptors);
            var tcs = readContext.Tcs;
            if (rv >= 0)
            {
                readContext.Tcs = null;
                tcs.SetResult(rv);
            }
            else
            {
                int errno = -rv;
                if (errno == EAGAIN)
                {
                    if (!_socket.ReceiveAsync(_waitForData))
                        DataAvailable(null, null);
                }
                else
                {
                    readContext.Tcs = null;
                    tcs.SetException(CreateExceptionForErrno(errno));
                }
            }
        }

        private Exception CreateExceptionForErrno(int errno)
        {
            if (errno == EBADF)
            {
                return new ObjectDisposedException(typeof(Socket).FullName);
            }
            else
            {
                return new SocketException(errno);
            }
        }

        private unsafe int DoRead(byte[] buffer, int offset, int count, List<UnixFd> fileDescriptors)
        {
            fixed (byte* buf = buffer)
            {
                do
                {
                    IOVector iov = new IOVector ();
                    iov.Base = buf + offset;
                    iov.Length = (SizeT)count;
                    
                    msghdr msg = new msghdr ();
                    msg.msg_iov = &iov;
                    msg.msg_iovlen = (SizeT)1;

                    cmsg_fd cm = new cmsg_fd ();
                    msg.msg_control = &cm;
                    msg.msg_controllen = (SizeT)sizeof (cmsg_fd);

                    int rv;
                    lock (_gate)
                    {
                        if (_socketFd == -1)
                        {
                            return -EBADF;
                        }
                        rv = (int)Interop.recvmsg(_socketFd, new IntPtr(&msg), 0);
                    }
                    if (rv >= 0)
                    {
                        if (cm.hdr.cmsg_level == SOL_SOCKET && cm.hdr.cmsg_type == SCM_RIGHTS)
                        {
                            int msgFdCount = ((int)cm.hdr.cmsg_len - sizeof(cmsghdr)) / sizeof(int);
                            for (int i = 0; i < msgFdCount; i++)
                            {
                                fileDescriptors.Add(new UnixFd(cm.fds[i]));
                            }
                        }
                        return rv;
                    }
                    else
                    {
                        var errno = Marshal.GetLastWin32Error();
                        if (errno != EINTR)
                        {
                            return -errno;
                        }
                    }
                } while (true);
            }
        }

        public unsafe Task<int> ReadAsync(byte[] buffer, int offset, int count, List<UnixFd> fileDescriptors)
        {
            if (!_supportsFdPassing)
            {
                var readContext = _receiveData.UserToken as ReadContext;
                TaskCompletionSource<int> tcs = readContext.Tcs ?? new TaskCompletionSource<int>();
                readContext.Tcs = tcs;
                _receiveData.SetBuffer(buffer, offset, count);
                readContext.FileDescriptors = fileDescriptors;
                if (!_socket.ReceiveAsync(_receiveData))
                {
                    if (_receiveData.SocketError == SocketError.Success)
                    {
                        return Task.FromResult(_receiveData.BytesTransferred);
                    }
                    else
                    {
                        return Task.FromException<int>(new SocketException((int)_receiveData.SocketError));
                    }
                }
                else
                {
                    return tcs.Task;
                }
            }
            else
            {
                var readContext = _waitForData.UserToken as ReadContext;
                TaskCompletionSource<int> tcs = readContext.Tcs ?? new TaskCompletionSource<int>();
                readContext.Tcs = tcs;
                readContext.Buffer = buffer;
                readContext.Offset = offset;
                readContext.Count = count;
                readContext.FileDescriptors = fileDescriptors;
                while (true)
                {
                    if (!_socket.ReceiveAsync(_waitForData))
                    {
                        int rv = DoRead(buffer, offset, count, fileDescriptors);
                        if (rv >= 0)
                        {
                            return Task.FromResult(rv);
                        }
                        else
                        {
                            int errno = -rv;
                            if (errno == EAGAIN)
                            {
                                continue;
                            }
                            else
                            {
                                return Task.FromException<int>(CreateExceptionForErrno(errno));
                            }
                        }
                    }
                    else
                    {
                        return tcs.Task;
                    }
                }
            }
        }

        public Task SendAsync(Message message)
        {
            if (!_supportsFdPassing && message.Header.NumberOfFds > 0)
            {
                foreach (var unixFd in message.UnixFds)
                {
                    unixFd.SafeHandle.Dispose();
                }
                message.Header.NumberOfFds = 0;
                message.UnixFds = null;
            }

            if (message.UnixFds != null && message.UnixFds.Length > 0)
            {
                return SendMessageWithFdsAsync(message);
            }
            else
            {
                _bufferList.Clear();
                var headerBytes = message.Header.ToArray();
                _bufferList.Add(new ArraySegment<byte>(headerBytes, 0, headerBytes.Length));
                if (message.Body != null)
                {
                    _bufferList.Add(new ArraySegment<byte>(message.Body, 0, message.Body.Length));
                }
                return SendBufferListAsync(_bufferList);
            }
        }

        private unsafe int SendMsg(msghdr* msg, int length)
        {
            // This method does NOT handle splitting msg and EAGAIN
            do
            {
                IntPtr rv;
                lock (_gate)
                {
                    if (_socketFd == -1)
                    {
                        throw new ObjectDisposedException(typeof(Socket).FullName);
                    }
                    rv = Interop.sendmsg(_socketFd, new IntPtr(msg), 0);
                }
                if (rv == new IntPtr(length))
                {
                    return length;
                }
                if (rv == new IntPtr(-1))
                {
                    var errno = Marshal.GetLastWin32Error();
                    if (errno != EINTR)
                    {
                        return -errno;
                    }
                }
                else
                {
                    return -EAGAIN;
                }
            } while (true);
        }

        private unsafe Task SendMessageWithFdsAsync(Message message)
        {
            var headerBytes = message.Header.ToArray();
            fixed (byte* bufHeader = headerBytes)
            {
                fixed (byte* bufBody = message.Body)
                {
                    IOVector* iovs = stackalloc IOVector[2];
                    iovs[0].Base = bufHeader;
                    iovs[0].Length = (SizeT)headerBytes.Length;
                    iovs[1].Base = bufBody;
                    int bodyLength = message.Body?.Length ?? 0;
                    iovs[1].Length = (SizeT)bodyLength;

                    msghdr msg = new msghdr ();
                    msg.msg_iov = iovs;
                    msg.msg_iovlen = (SizeT)2;

                    var fdm = new cmsg_fd ();
                    int size = sizeof(cmsghdr) + 4 * message.UnixFds.Length;
                    msg.msg_control = &fdm;
                    msg.msg_controllen = (SizeT)size;
                    fdm.hdr.cmsg_len = (SizeT)size;
                    fdm.hdr.cmsg_level = SOL_SOCKET;
                    fdm.hdr.cmsg_type = SCM_RIGHTS;
                    for (int i = 0, j = 0; i < message.UnixFds.Length; i++)
                    {
                        fdm.fds[j++] = message.UnixFds[i].Handle;
                    }

                    int rv = SendMsg(&msg, headerBytes.Length + bodyLength);

                    if (message.UnixFds != null)
                    {
                        foreach (var fd in message.UnixFds)
                        {
                            fd.SafeHandle.Dispose();
                        }
                    }
                    if (rv >= 0)
                    {
                        return Task.CompletedTask;
                    }
                    else
                    {
                        var errno = -rv;
                        return Task.FromException(CreateExceptionForErrno(errno));
                    }
                }
            }
        }

        public unsafe Task SendAsync(byte[] buffer, int offset, int count)
        {
            _bufferList.Clear();
            _bufferList.Add(new ArraySegment<byte>(buffer, offset, count));
            return SendBufferListAsync(_bufferList);
        }

        private Task SendBufferListAsync(List<ArraySegment<byte>> bufferList)
        {
            var sendContext = _sendArgs.UserToken as SendContext;
            TaskCompletionSource<object> tcs = sendContext.Tcs ?? new TaskCompletionSource<object>();
            sendContext.Tcs = tcs;
            _sendArgs.BufferList = bufferList;
            if (!_socket.SendAsync(_sendArgs))
            {
                if (_sendArgs.SocketError == SocketError.Success)
                {
                    return Task.CompletedTask;
                }
                else
                {
                    return Task.FromException(new SocketException((int)_sendArgs.SocketError));
                }
            }
            else
            {
                return tcs.Task;
            }
        }

        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            var sendContext = e.UserToken as SendContext;
            var tcs = sendContext.Tcs;
            sendContext.Tcs = null;
            if (e.SocketError == SocketError.Success)
            {
                tcs.SetResult(null);
            }
            else
            {
                tcs.SetException(new SocketException((int)e.SocketError));
            }
        }

        private static int GetFd(Socket socket)
        {
            if (s_handleProperty != null)
            {
                // netstandard2.0
                return (int)(IntPtr)s_handleProperty.GetValue(socket, null);
            }
            else if (s_safehandleProperty != null)
            {
                // .NET Core 1.x
                return ((SafeHandle)s_safehandleProperty.GetValue(socket, null)).DangerousGetHandle().ToInt32();
            }
            return -1;
        }

        public static Task<TransportSocket> ConnectAsync(AddressEntry entry, CancellationToken cancellationToken, bool supportsFdPassing)
        {
            switch (entry.Method)
            {
                case "tcp":
                    return ConnectTcpAsync(entry, cancellationToken);
                case "unix":
                    return ConnectUnixAsync(entry, cancellationToken, supportsFdPassing);
                default:
                    throw new NotSupportedException("Transport method \"" + entry.Method + "\" not supported");
            }
        }

        private static async Task<TransportSocket> ConnectUnixAsync(AddressEntry entry, CancellationToken cancellationToken, bool supportsFdPassing)
        {
            Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                var transportSocket = new TransportSocket(socket, supportsFdPassing);
                using (cancellationToken.Register(() => transportSocket.Dispose()))
                {
                    var endpoints = await entry.ResolveAsync().ConfigureAwait(false);
                    var endPoint = endpoints[0];

                    await transportSocket.ConnectAsync(endPoint).ConfigureAwait(false);
                    return transportSocket;
                }
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private static async Task<TransportSocket> ConnectTcpAsync(AddressEntry entry, CancellationToken cancellationToken)
        {
            var endpoints = await entry.ResolveAsync().ConfigureAwait(false);
            for (int i = 0; i < endpoints.Length; i++)
            {
                var ipEndPoint = endpoints[i] as IPEndPoint;
                bool lastAddress = i == (endpoints.Length - 1);
                Socket socket = new Socket(ipEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    var transportSocket = new TransportSocket(socket, supportsFdPassing: false);
                    using (cancellationToken.Register(() => transportSocket.Dispose()))
                    {
                        await transportSocket.ConnectAsync(ipEndPoint).ConfigureAwait(false);
                        return transportSocket;
                    }
                }
                catch
                {
                    socket.Dispose();
                    if (lastAddress)
                    {
                        throw;
                    }
                }
            }

            return null;
        }

        private Task ConnectAsync(EndPoint endPoint)
            => _socket.ConnectAsync(endPoint);
    }
}
