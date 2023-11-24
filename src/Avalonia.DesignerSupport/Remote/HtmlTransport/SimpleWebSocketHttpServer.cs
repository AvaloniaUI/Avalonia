using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.DesignerSupport.Remote.HtmlTransport
{
    public class SimpleWebSocketHttpServer : IDisposable
    {
        private readonly IPAddress _address;
        private readonly int _port;
        private TcpListener _listener;

        public async Task<SimpleWebSocketHttpRequest> AcceptAsync()
        {
            while (true)
            {
                var res = await AcceptAsyncImpl();
                if (res != null)
                    return res;
            }
        }
        async Task<SimpleWebSocketHttpRequest> AcceptAsyncImpl()
        {
            if (_listener == null)
                throw new InvalidOperationException("Currently not listening");
            var socket = await _listener.AcceptSocketAsync();
            var stream = new NetworkStream(socket);
            bool error = true;
            async Task<string> ReadLineAsync()
            {
                var readBuffer = new byte[1];
                var lineBuffer = new byte[1024];
                for (var c = 0; c < 1024; c++)
                {
                    if (await stream.ReadAsync(readBuffer, 0, 1) == 0)
                        throw new EndOfStreamException();
                    if (readBuffer[0] == 10)
                    {
                        if (c == 0)
                            return "";
                        if (lineBuffer[c - 1] == 13)
                            c--;
                        if (c == 0)
                            return "";
                        
                        return Encoding.UTF8.GetString(lineBuffer, 0, c);
                    }
                    lineBuffer[c] = readBuffer[0];
                }

                throw new InvalidDataException("Header is too large");
            }

            var headers = new Dictionary<string, string>();
            string line = null;
            try
            {

                line = await ReadLineAsync();
                var sp = line.Split(' ');
                if (sp.Length != 3 || !sp[2].StartsWith("HTTP") || sp[0] != "GET")
                    return null;
                var path = sp[1];

                while (true)
                {
                    line = await ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                        break;
                    sp = line.Split(new[] {':'}, 2);
                    headers[sp[0]] = sp[1].TrimStart();
                }

                error = false;
                
                return new SimpleWebSocketHttpRequest(stream, path, headers);
            }
            catch
            {
                error = true;
                return null;
            }
            finally
            {
                if (error)
                    stream.Dispose();
            }
            
        }

        public void Listen()
        {
            var listener = new TcpListener(_address, _port);
            listener.Start();
            _listener = listener;
        }
        
        public SimpleWebSocketHttpServer(IPAddress address, int port)
        {
            _address = address;
            _port = port;
        }

        public void Dispose()
        {
            _listener?.Stop();
            _listener = null;
        }
    }

    
    public class SimpleWebSocketHttpRequest : IDisposable
    {
        public Dictionary<string, string> Headers { get; }
        public string Path { get; }
        private NetworkStream _stream;
        public bool IsWebsocketRequest { get; }
        public IReadOnlyList<string> WebSocketProtocols { get; }
        private string _websocketKey;

        public SimpleWebSocketHttpRequest(NetworkStream stream, string path, Dictionary<string, string> headers)
        {
            Path = path;
            Headers = headers;

            _stream = stream;
            if (headers.TryGetValue("Connection", out var h)
                && h.Contains("Upgrade")
                && headers.TryGetValue("Upgrade", out h) &&
                h == "websocket"
                && headers.TryGetValue("Sec-WebSocket-Key", out _websocketKey))
            {
                IsWebsocketRequest = true;
                if (headers.TryGetValue("Sec-WebSocket-Protocol", out h))
                    WebSocketProtocols = h.Split(',').Select(x => x.Trim()).ToArray();
                else WebSocketProtocols = Array.Empty<string>();
            }
        }

        public async Task RespondAsync(int code, byte[] data, string contentType)
        {
            var headers = Encoding.UTF8.GetBytes(FormattableString.Invariant($"HTTP/1.1 {code} {(HttpStatusCode)code}\r\nConnection: close\r\nContent-Type: {contentType}\r\nContent-Length: {data.Length}\r\n\r\n"));
            await _stream.WriteAsync(headers, 0, headers.Length);
            await _stream.WriteAsync(data, 0, data.Length);
            _stream.Dispose();
            _stream = null;

        }


        public async Task<SimpleWebSocket> AcceptWebSocket(string protocol = null)
        {
            
            var handshakeSource = _websocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string handshake;
            using (var sha1 = SHA1.Create())
                handshake = Convert.ToBase64String(sha1.ComputeHash(Encoding.UTF8.GetBytes(handshakeSource)));
            var headers =
                "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: "
                + handshake + "\r\n";
            if (protocol != null)
                headers += protocol + "\r\n";
            headers += "\r\n";
            var bheaders = Encoding.UTF8.GetBytes(headers);
            await _stream.WriteAsync(bheaders, 0, bheaders.Length);
            
            var s = _stream;
            _stream = null;
            return new SimpleWebSocket(s);
        }

        public void Dispose() => _stream?.Dispose();
    }

    
    
    public class SimpleWebSocket : IDisposable
    {
        class AsyncLock
        {
            private object _syncRoot = new object();
            private Queue<TaskCompletionSource<IDisposable>> _queue = new Queue<TaskCompletionSource<IDisposable>>();
            private bool _locked;
            public Task<IDisposable> LockAsync()
            {
                lock (_syncRoot)
                {
                    if (!_locked)
                    {
                        _locked = true;
                        return Task.FromResult<IDisposable>(new Lock(this));
                    }
                    else
                    {
                        var tcs = new TaskCompletionSource<IDisposable>();
                        _queue.Enqueue(tcs);
                        return tcs.Task;
                    }
                }
            }

            private void Unlock()
            {
                lock (_syncRoot)
                {
                    if (_queue.Count != 0)
                        _queue.Dequeue().SetResult(new Lock(this));
                    else
                        _locked = false;
                }
            }

            class Lock : IDisposable
            {
                private  AsyncLock _parent;
                private object _syncRoot = new object();

                public Lock(AsyncLock parent)
                {
                    _parent = parent;
                }
            
                public void Dispose()
                {
                    lock (_syncRoot)
                    {
                        if (_parent == null)
                            return;
                        var p = _parent;
                        _parent = null;
                        p.Unlock();
                    }
                }
            }
        }
        
        private Stream _stream;
        private AsyncLock _sendLock = new AsyncLock();
        private AsyncLock _recvLock = new AsyncLock();
        private const int WebsocketInitialHeaderLength = 2;
        private const int WebsocketLen16Length = 4;
        private const int WebsocketLen64Length = 10;

        private const int WebsocketLen16Code = 126;
        private const int WebsocketLen64Code = 127;

        [StructLayout(LayoutKind.Explicit)]
        struct WebSocketHeader
        {
            [FieldOffset(0)] public byte Mask;
            [FieldOffset(1)] public byte Length8;
            [FieldOffset(2)] public ushort Length16;
            [FieldOffset(2)] public ulong Length64;
        }

        readonly byte[] _sendHeaderBuffer = new byte[10];
        readonly MemoryStream _receiveFrameStream = new MemoryStream();
        readonly MemoryStream _receiveMessageStream = new MemoryStream();
        private FrameType _currentMessageFrameType;

        enum FrameType
        {
            Continue = 0x0,
            Text = 0x1,
            Binary = 0x2,
            Close = 0x8,
            Ping = 0x9,
            Pong = 0xA
        }

        internal SimpleWebSocket(Stream stream)
        {
            _stream = stream;
        }

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }

        public Task SendMessage(string text)
        {
            var data = Encoding.UTF8.GetBytes(text);
            return SendMessage(true, data);
        }
        public Task SendMessage(bool isText, byte[] data) => SendMessage(isText, data, 0, data.Length);


        public Task SendMessage(bool isText, byte[] data, int offset, int length) 
            => SendFrame(isText ? FrameType.Text : FrameType.Binary, data, offset, length);

        async Task SendFrame(FrameType type, byte[] data, int offset, int length)
        {
            using (var l = await _sendLock.LockAsync())
            {
                var header = new WebSocketHeader();

                int headerLength;
                if (data.Length <= 125)
                {
                    headerLength = WebsocketInitialHeaderLength;
                    header.Length8 = (byte) length;
                }
                else if (length <= 0xffff)
                {
                    headerLength = WebsocketLen16Length;
                    header.Length8 = WebsocketLen16Code;
                    header.Length16 = (ushort) IPAddress.HostToNetworkOrder((short) (ushort) length);

                }
                else
                {
                    headerLength = WebsocketLen64Length;
                    header.Length8 = WebsocketLen64Code;
                    header.Length64 = (ulong) IPAddress.HostToNetworkOrder((long) length);
                }

                const byte endOfMessageBit = (byte)1u << 7;
                header.Mask = (byte) (endOfMessageBit | ((byte) type & 0xf));
                unsafe
                {
                    Marshal.Copy(new IntPtr(&header), _sendHeaderBuffer, 0, headerLength);
                }

                await _stream.WriteAsync(_sendHeaderBuffer, 0, headerLength);
                await _stream.WriteAsync(data, offset, length);
            }
        }

        struct Frame
        {
            public byte[] Data;
            public bool EndOfMessage;
            public FrameType FrameType;
        }

        byte[] _recvHeaderBuffer = new byte[8];
        byte[] _maskBuffer = new byte[4];
        async Task<Frame> ReadFrame()
        {
            _receiveFrameStream.Position = 0;
            _receiveFrameStream.SetLength(0);
            await ReadExact(_stream, _recvHeaderBuffer, 0, 2);
            var masked = (_recvHeaderBuffer[1] & 0x80) != 0;
            var len0 = (_recvHeaderBuffer[1] & 0x7F);
            var endOfMessage = (_recvHeaderBuffer[0] & 0x80) != 0;
            var frameType = (FrameType) (_recvHeaderBuffer[0] & 0xf);
            int length;
            if (len0 <= 125)
                length = len0;
            else if (len0 == WebsocketLen16Code)
            {
                await ReadExact(_stream, _recvHeaderBuffer, 0, 2);
                length = (ushort) IPAddress.NetworkToHostOrder(BitConverter.ToInt16(_recvHeaderBuffer, 0));
            }
            
            else
            {
                await ReadExact(_stream, _recvHeaderBuffer, 0, 8);
                length = (int) (ulong) IPAddress.NetworkToHostOrder((long) BitConverter.ToUInt64(_recvHeaderBuffer, 0));
            }

            if (masked)
                await ReadExact(_stream, _maskBuffer, 0, 4);
            await ReadExact(_stream, _receiveFrameStream, length);
            var data = _receiveFrameStream.ToArray();
            if(masked)
                for (var c = 0; c < data.Length; c++)
                    data[c] = (byte) (data[c] ^ _maskBuffer[c % 4]);

            return new Frame
            {
                Data = data,
                EndOfMessage = endOfMessage,
                FrameType = frameType
            };
        }

       
        public async Task<SimpleWebSocketMessage> ReceiveMessage()
        {
            using (await _recvLock.LockAsync())
            {
                while (true)
                {
                    var frame = await ReadFrame();
                    
                    if (frame.FrameType == FrameType.Close)
                        return null;
                    if (frame.FrameType == FrameType.Ping)
                        await SendFrame(FrameType.Pong, frame.Data, 0, frame.Data.Length);
                    if (frame.FrameType == FrameType.Text || frame.FrameType == FrameType.Binary)
                    {
                        var isText = frame.FrameType == FrameType.Text;
                        if (_receiveMessageStream.Length == 0 && frame.EndOfMessage)
                            return new SimpleWebSocketMessage
                            {
                                IsText = isText,
                                Data = frame.Data
                            };

                        _receiveMessageStream.Write(frame.Data, 0, frame.Data.Length);
                        _currentMessageFrameType = frame.FrameType;
                    }
                    if (frame.FrameType == FrameType.Continue)
                    {
                        frame.FrameType = _currentMessageFrameType;
                        _receiveMessageStream.Write(frame.Data, 0, frame.Data.Length);
                        if (frame.EndOfMessage)
                        {
                            var isText = frame.FrameType == FrameType.Text;
                            var data = _receiveMessageStream.ToArray();
                            _receiveMessageStream.Position = 0;
                            _receiveMessageStream.SetLength(0);
                            return new SimpleWebSocketMessage
                            {
                                IsText = isText,
                                Data = data
                            };
                        }
                    }
                }
            }
        }


        byte[] _readExactBuffer = new byte[4096];
        async Task ReadExact(Stream from, MemoryStream to, int length)
        {
            while (length>0)
            {
                var toRead = Math.Min(length, _readExactBuffer.Length);
                var read = await from.ReadAsync(_readExactBuffer, 0, toRead);
                to.Write(_readExactBuffer, 0, read);
                if (read <= 0)
                    throw new EndOfStreamException();
                length -= read;
            }
        }

        async Task ReadExact(Stream from, byte[] to, int offset, int length)
        {
            while (length > 0)
            {
                var read = await from.ReadAsync(to, offset, length);
                if (read <= 0)
                    throw new EndOfStreamException();
                length -= read;
                offset += read;
            }
        }
    }

    public class SimpleWebSocketMessage
    {
        public bool IsText { get; set; }
        public byte[] Data { get; set; }

        public string AsString()
        {
            return Encoding.UTF8.GetString(Data);
        }
    }
}
