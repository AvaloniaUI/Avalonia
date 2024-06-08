// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Transports
{
    internal class Transport : IMessageStream
    {
        private struct PendingSend
        {
            public Message Message;
            public TaskCompletionSource<bool> CompletionSource;
        }

        private static readonly byte[] _oneByteArray = new[] { (byte)0 };
        private readonly byte[] _headerReadBuffer = new byte[16];
        private readonly List<UnixFd> _fileDescriptors = new List<UnixFd>();
        private TransportSocket _socket;
        private ConcurrentQueue<PendingSend> _sendQueue;
        private SemaphoreSlim _sendSemaphore;

        public static async Task<IMessageStream> ConnectAsync(AddressEntry entry, ClientSetupResult connectionContext, CancellationToken cancellationToken)
        {
            TransportSocket socket = await TransportSocket.ConnectAsync(entry, cancellationToken, connectionContext.SupportsFdPassing).ConfigureAwait(false);
            try
            {
                Transport transport = new Transport(socket);
                await transport.DoClientAuth(entry.Guid, connectionContext.UserId).ConfigureAwait(false);
                return transport;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        public static async Task<IMessageStream> AcceptAsync(Socket acceptedSocket, bool supportsFdPassing)
        {
            var socket = new TransportSocket(acceptedSocket, supportsFdPassing);
            Transport transport = new Transport(socket);
            socket.SupportsFdPassing = await transport.DoServerAuth(socket.SupportsFdPassing).ConfigureAwait(false);
            return transport;
        }

        private Transport(TransportSocket socket)
        {
            _socket = socket;
            _sendQueue = new ConcurrentQueue<PendingSend>();
            _sendSemaphore = new SemaphoreSlim(1);
        }

        public async Task<Message> ReceiveMessageAsync()
        {
            try
            {
                int bytesRead = await ReadCountAsync(_headerReadBuffer, 0, 16, _fileDescriptors).ConfigureAwait(false);
                if (bytesRead == 0)
                    return null;
                if (bytesRead != 16)
                    throw new ProtocolException("Header read length mismatch: " + bytesRead + " of expected " + "16");

                EndianFlag endianness = (EndianFlag)_headerReadBuffer[0];
                MessageReader reader = new MessageReader(endianness, new ArraySegment<byte>(_headerReadBuffer));

                //discard endian byte, message type and flags, which we don't care about here
                reader.Seek(3);

                byte version = reader.ReadByte();
                if (version != ProtocolInformation.Version)
                    throw new NotSupportedException("Protocol version '" + version.ToString() + "' is not supported");

                uint bodyLength = reader.ReadUInt32();

                //discard _methodSerial
                reader.ReadUInt32();

                uint headerLength = reader.ReadUInt32();

                int bodyLen = (int)bodyLength;
                int toRead = (int)headerLength;

                //we fixup to include the padding following the header
                toRead = ProtocolInformation.Padded(toRead, 8);

                long msgLength = toRead + bodyLen;
                if (msgLength > ProtocolInformation.MaxMessageLength)
                    throw new ProtocolException("Message length " + msgLength + " exceeds maximum allowed " + ProtocolInformation.MaxMessageLength + " bytes");

                byte[] header = new byte[16 + toRead];
                Array.Copy(_headerReadBuffer, header, 16);
                bytesRead = await ReadCountAsync(header, 16, toRead, _fileDescriptors).ConfigureAwait(false);
                if (bytesRead != toRead)
                    throw new ProtocolException("Message header length mismatch: " + bytesRead + " of expected " + toRead);

                var messageHeader = Header.FromBytes(new ArraySegment<byte>(header));

                byte[] body = null;
                //read the body
                if (bodyLen != 0)
                {
                    body = new byte[bodyLen];

                    bytesRead = await ReadCountAsync(body, 0, bodyLen, _fileDescriptors).ConfigureAwait(false);

                    if (bytesRead != bodyLen)
                        throw new ProtocolException("Message body length mismatch: " + bytesRead + " of expected " + bodyLen);
                }

                if (_fileDescriptors.Count < messageHeader.NumberOfFds)
                {
                    throw new ProtocolException("File descriptor length mismatch: " + _fileDescriptors.Count + " of expected " + messageHeader.NumberOfFds);
                }

                Message msg = new Message(
                    messageHeader,
                    body,
                    messageHeader.NumberOfFds == 0 ? null :
                        _fileDescriptors.Count == messageHeader.NumberOfFds ? _fileDescriptors.ToArray() :
                        _fileDescriptors.Take((int)messageHeader.NumberOfFds).ToArray()
                );

                _fileDescriptors.RemoveRange(0, (int)messageHeader.NumberOfFds);

                return msg;
            }
            catch
            {
                foreach(var fd in _fileDescriptors)
                {
                    CloseSafeHandle.close(fd.Handle);
                }
                throw;
            }
        }

        private async Task<int> ReadCountAsync(byte[] buffer, int offset, int count, List<UnixFd> fileDescriptors)
        {
            int read = 0;
            while (read < count)
            {
                int nread = await _socket.ReadAsync(buffer, offset + read, count - read, fileDescriptors).ConfigureAwait(false);
                if (nread == 0)
                    break;
                read += nread;
            }
            return read;
        }

        private struct AuthenticationResult
        {
            public bool IsAuthenticated;
            public bool SupportsFdPassing;
            public Guid Guid;
        }

        class SplitLine
        {
            public readonly string Value;
            private readonly List<string> _args = new List<string>();

            public SplitLine(string value)
            {
                this.Value = value.Trim();
                _args.AddRange(Value.Split(' '));
            }

            public string this[int index]
            {
                get
                {
                    if (index >= _args.Count)
                        return String.Empty;
                    return _args[index];
                }
            }
        }

        private async Task<bool> DoServerAuth(bool supportsFdPassing)
        {
            bool peerSupportsFdPassing = false;
            // receive 1 byte
            byte[] buffer = new byte[1];
            int length = await ReadCountAsync(buffer, 0, buffer.Length, _fileDescriptors).ConfigureAwait(false);
            if (length == 0)
            {
                throw new IOException("Connection closed by peer");
            }
            // auth
            while (true)
            {
                var line = await ReadLineAsync().ConfigureAwait(false);
                if (line[0] == "AUTH")
                {
                    if (line[1] == "ANONYMOUS")
                    {
                        await WriteLineAsync("OK").ConfigureAwait(false);
                    }
                    else
                    {
                        await WriteLineAsync("REJECTED ANONYMOUS").ConfigureAwait(false);
                    }
                }
                else if (line[0] == "CANCEL")
                {
                    await WriteLineAsync("REJECTED ANONYMOUS").ConfigureAwait(false);
                }
                else if (line[0] == "BEGIN")
                {
                    break;
                }
                else if (line[0] == "DATA")
                {
                    throw new ProtocolException("Unexpected DATA message during authentication.");
                }
                else if (line[0] == "ERROR")
                { }
                else if (line[0] == "NEGOTIATE_UNIX_FD" && supportsFdPassing)
                {
                    await WriteLineAsync("AGREE_UNIX_FD").ConfigureAwait(false);
                    peerSupportsFdPassing = true;
                }
                else
                {
                    await WriteLineAsync("ERROR").ConfigureAwait(false);
                }
            }
            return peerSupportsFdPassing;
        }

        private async Task DoClientAuth(Guid guid, string userId)
        {
            // send 1 byte
            await _socket.SendAsync(_oneByteArray, 0, 1).ConfigureAwait(false);
            // auth
            var authenticationResult = await SendAuthCommands(userId).ConfigureAwait(false);
            _socket.SupportsFdPassing = authenticationResult.SupportsFdPassing;
            if (guid != Guid.Empty)
            {
                if (guid != authenticationResult.Guid)
                {
                    throw new ConnectException("Authentication failure: Unexpected GUID");
                }
            }
        }

        private async Task<AuthenticationResult> SendAuthCommands(string userId)
        {
            string initialData = null;
            if (userId != null)
            {
                byte[] bs = Encoding.ASCII.GetBytes(userId);
                initialData = ToHex(bs);
            }
            AuthenticationResult result;
            var commands = new[]
            {
                initialData != null ? "AUTH EXTERNAL " + initialData : null,
                "AUTH ANONYMOUS"
            };

            foreach (var command in commands)
            {
                if (command == null)
                {
                    continue;
                }
                result = await SendAuthCommand(command).ConfigureAwait(false);
                if (result.IsAuthenticated)
                {
                    return result;
                }
            }

            throw new ConnectException("Authentication failure");
        }

        private async Task<AuthenticationResult> SendAuthCommand(string command)
        {
            AuthenticationResult result = default(AuthenticationResult);
            await WriteLineAsync(command).ConfigureAwait(false);
            SplitLine reply = await ReadLineAsync().ConfigureAwait(false);

            if (reply[0] == "OK")
            {
                result.IsAuthenticated = true;
                result.Guid = reply[1] != string.Empty ? Guid.ParseExact(reply[1], "N") : Guid.Empty;

                if (_socket.SupportsFdPassing)
                {
                    await WriteLineAsync("NEGOTIATE_UNIX_FD").ConfigureAwait(false);
                    reply = await ReadLineAsync().ConfigureAwait(false);
                    result.SupportsFdPassing = reply[0] == "AGREE_UNIX_FD";
                }

                await WriteLineAsync("BEGIN").ConfigureAwait(false);
                return result;
            }
            else if (reply[0] == "REJECTED")
            {
                return result;
            }
            else
            {
                await WriteLineAsync("ERROR").ConfigureAwait(false);
                return result;
            }
        }

        private Task WriteLineAsync(string message)
        {
            message += "\r\n";
            var bytes = Encoding.ASCII.GetBytes(message);
            return _socket.SendAsync(bytes, 0, bytes.Length);
        }

        private async Task<SplitLine> ReadLineAsync()
        {
            byte[] buffer = new byte[1];
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                int length = await ReadCountAsync(buffer, 0, buffer.Length, _fileDescriptors).ConfigureAwait(false);
                if (length == 0)
                {
                    throw new IOException("Connection closed by peer");
                }
                byte b = buffer[0];
                if (b == '\r')
                {
                    length = await ReadCountAsync(buffer, 0, buffer.Length, _fileDescriptors).ConfigureAwait(false);
                    b = buffer[0];
                    if (b == '\n')
                    {
                        string ln = sb.ToString();
                        if (ln != string.Empty)
                        {
                            return new SplitLine(ln);
                        }
                        else
                        {
                            throw new ProtocolException("Received empty authentication message from server");
                        }
                    }
                    throw new ProtocolException("Authentication messages from server must end with '\\r\\n'");
                }
                else
                {
                    sb.Append((char) b);
                }
            }
        }

        private static string ToHex(byte[] input)
        {
            StringBuilder result = new StringBuilder(input.Length * 2);
            string alfabeth = "0123456789abcdef";

            foreach (byte b in input)
            {
                result.Append(alfabeth[(int)(b >> 4)]);
                result.Append(alfabeth[(int)(b & 0xF)]);
            }

            return result.ToString();
        }

        public Task SendMessageAsync(Message message)
        {
            var tcs = new TaskCompletionSource<bool>();
            var pendingSend = new PendingSend()
            {
                Message = message,
                CompletionSource = tcs
            };
            _sendQueue.Enqueue(pendingSend);
            SendPendingMessages();
            return tcs.Task;
        }

        public void TrySendMessage(Message message)
        {
            var pendingSend = new PendingSend()
            {
                Message = message
            };
            _sendQueue.Enqueue(pendingSend);
            SendPendingMessages();
        }

        private async void SendPendingMessages()
        {
            try
            {
                await _sendSemaphore.WaitAsync().ConfigureAwait(false);
                PendingSend pendingSend;
                while (_sendQueue.TryDequeue(out pendingSend))
                {
                    try
                    {
                        await _socket.SendAsync(pendingSend.Message).ConfigureAwait(false);
                        pendingSend.CompletionSource?.SetResult(true);
                    }
                    catch (System.Exception)
                    {
                        pendingSend.CompletionSource?.SetResult(false);
                    }
                }
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}