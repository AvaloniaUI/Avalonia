// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

namespace Tmds.DBus.Protocol
{
    internal class Message
    {
        public Message(Header header, byte[] body, UnixFd[] unixFds)
        {
            _header = header;
            _body = body;
            _fds = unixFds;
            _header.Length = _body != null ? (uint)_body.Length : 0;
            _header.NumberOfFds = (uint)(_fds?.Length ?? 0);
        }

        private Header _header;
        private byte[] _body;
        private UnixFd[] _fds;

        public UnixFd[] UnixFds
        {
            get => _fds;
            set => _fds = value;
        }

        public byte[] Body => _body;

        public Header Header => _header;
    }
}
