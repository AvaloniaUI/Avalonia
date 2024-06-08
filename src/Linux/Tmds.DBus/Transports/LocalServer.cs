// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.Transports
{
    class LocalServer : IMessageStream
    {
        private readonly object _gate = new object();
        private readonly DBusConnection _connection;
        private IMessageStream[] _clients;
        private Socket _serverSocket;
        private bool _started;

        public LocalServer(DBusConnection connection)
        {
            _connection = connection;
            _clients = Array.Empty<IMessageStream>();
        }

        public async Task<string> StartAsync(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            var entries = AddressEntry.ParseEntries(address);
            if (entries.Length != 1)
            {
                throw new ArgumentException("Address must contain a single entry.", nameof(address));
            }
            var entry = entries[0];
            var endpoints = await entry.ResolveAsync(listen: true).ConfigureAwait(false);
            if (endpoints.Length == 0)
            {
                throw new ArgumentException("Address does not resolve to an endpoint.", nameof(address));
            }

            lock (_gate)
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(typeof(LocalServer).FullName);
                }
                if (_started)
                {
                    throw new InvalidOperationException("Server is already started.");
                }
                _started = true;

                var endpoint = endpoints[0];
                if (endpoint is IPEndPoint ipEndPoint)
                {
                    _serverSocket = new Socket(ipEndPoint.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                }
                else if (endpoint is UnixDomainSocketEndPoint unixEndPoint)
                {
                    _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    if (unixEndPoint.Path[0] == '\0')
                    {
                        address = $"unix:abstract={unixEndPoint.Path.Substring(1)}";
                    }
                    else
                    {
                        address = $"unix:path={unixEndPoint.Path}";
                    }
                }
                _serverSocket.Bind(endpoint);
                _serverSocket.Listen(10);
                AcceptConnections();

                if (endpoint is IPEndPoint)
                {
                    var boundEndPoint = _serverSocket.LocalEndPoint as IPEndPoint;
                    address = $"tcp:host={boundEndPoint.Address},port={boundEndPoint.Port}";
                }
                return address;
            }
        }

        public async void AcceptConnections()
        {
            while (true)
            {
                Socket clientSocket = null;
                try
                {
                    try
                    {
                        clientSocket = await _serverSocket.AcceptAsync().ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    var client = await Transport.AcceptAsync(clientSocket,
                        supportsFdPassing: _serverSocket.AddressFamily == AddressFamily.Unix).ConfigureAwait(false);
                    lock (_gate)
                    {
                        if (IsDisposed)
                        {
                            client.Dispose();
                            break;
                        }
                        var clientsUpdated = new IMessageStream[_clients.Length + 1];
                        Array.Copy(_clients, clientsUpdated, _clients.Length);
                        clientsUpdated[clientsUpdated.Length - 1] = client;
                        _clients = clientsUpdated;
                    }
                    _connection.ReceiveMessages(client, RemoveStream);
                }
                catch
                {
                    clientSocket?.Dispose();
                }
            }
        }

        private void RemoveStream(IMessageStream client, Exception e)
        {
            lock (_gate)
            {
                if (IsDisposed)
                {
                    return;
                }

                var clientsUpdated = new IMessageStream[_clients.Length - 1];
                for (int i = 0, j = 0; i < _clients.Length; i++)
                {
                    if (_clients[i] != client)
                    {
                        clientsUpdated[j++] = _clients[i];
                    }
                }
                _clients = clientsUpdated;
            }
            client.Dispose();
        }

        public void TrySendMessage(Message message)
        {
            var clients = Volatile.Read(ref _clients);
            foreach (var client in clients)
            {
                client.TrySendMessage(message);
            }
        }

        public Task<Message> ReceiveMessageAsync()
        {
            return Task.FromException<Message>(new NotSupportedException("Cannot determine destination peer."));
        }

        public Task SendMessageAsync(Message message)
        {
            return Task.FromException(new NotSupportedException("Cannot determine destination peer."));
        }

        public void Dispose()
        {
            IMessageStream[] clients;
            lock (_gate)
            {
                if (IsDisposed)
                {
                    return;
                }

                _serverSocket?.Dispose();

                clients = _clients;
                _clients = null;
            }

            foreach (var client in clients)
            {
                client.Dispose();
            }
        }

        private bool IsDisposed => _clients == null;
    }
}