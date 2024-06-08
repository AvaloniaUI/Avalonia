// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Tmds.DBus.Transports
{
    /// <summary>Represents a Unix Domain Socket endpoint as a path.</summary>
    internal sealed class UnixDomainSocketEndPoint : EndPoint
    {
        private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

        private static readonly Encoding s_pathEncoding = Encoding.UTF8;
        private const int s_nativePathOffset = 2;

        private readonly string _path;
        private readonly byte[] _encodedPath;

        public UnixDomainSocketEndPoint(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            _path = path;
            _encodedPath = s_pathEncoding.GetBytes(_path);

            if (path.Length == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(path), path,
                    string.Format("The path '{0}' is of an invalid length for use with domain sockets on this platform. The length must be at least 1 characters.", path));
            }
        }

        internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
        {
            if (socketAddress == null)
            {
                throw new ArgumentNullException(nameof(socketAddress));
            }

            if (socketAddress.Family != EndPointAddressFamily)
            {
                throw new ArgumentOutOfRangeException(nameof(socketAddress));
            }

            if (socketAddress.Size > s_nativePathOffset)
            {
                _encodedPath = new byte[socketAddress.Size - s_nativePathOffset];
                for (int i = 0; i < _encodedPath.Length; i++)
                {
                    _encodedPath[i] = socketAddress[s_nativePathOffset + i];
                }

                _path = s_pathEncoding.GetString(_encodedPath, 0, _encodedPath.Length);
            }
            else
            {
                _encodedPath = Array.Empty<byte>();
                _path = string.Empty;
            }
        }

        public override SocketAddress Serialize()
        {
            var result = new SocketAddress(AddressFamily.Unix, _encodedPath.Length + s_nativePathOffset);

            for (int index = 0; index < _encodedPath.Length; index++)
            {
                result[s_nativePathOffset + index] = _encodedPath[index];
            }

            return result;
        }

        public override EndPoint Create(SocketAddress socketAddress) => new UnixDomainSocketEndPoint(socketAddress);

        public override AddressFamily AddressFamily => EndPointAddressFamily;

        public string Path => _path;

        public override string ToString() => _path;
    }
}
