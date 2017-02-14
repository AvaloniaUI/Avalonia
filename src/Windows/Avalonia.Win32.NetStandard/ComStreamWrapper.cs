//
// System.Drawing.ComIStreamWrapper.cs
//
// Author:
//   Kornél Pál <http://www.kornelpal.hu/>
//
// Copyright (C) 2005-2008 Kornél Pál
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace Avalonia.Win32
{
    // Stream to IStream wrapper for COM interop
    internal sealed class ComIStreamWrapper : IStream
    {
        private const int STG_E_INVALIDFUNCTION = unchecked((int)0x80030001);

        private readonly Stream baseStream;
        private long position = -1;

        internal ComIStreamWrapper(Stream stream)
        {
            baseStream = stream;
        }

        private void SetSizeToPosition()
        {
            if (position != -1)
            {
                if (position > baseStream.Length)
                    baseStream.SetLength(position);
                baseStream.Position = position;
                position = -1;
            }
        }

        public void Read(byte[] pv, int cb, IntPtr pcbRead)
        {
            int read = 0;

            if (cb != 0)
            {
                SetSizeToPosition();
                read = baseStream.Read(pv, 0, cb);
            }

            if (pcbRead != IntPtr.Zero)
                Marshal.WriteInt32(pcbRead, read);
        }

        public void Write(byte[] pv, int cb, IntPtr pcbWritten)
        {
            if (cb != 0)
            {
                SetSizeToPosition();
                baseStream.Write(pv, 0, cb);
            }

            if (pcbWritten != IntPtr.Zero)
                Marshal.WriteInt32(pcbWritten, cb);
        }

        public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
        {
            long length = baseStream.Length;
            long newPosition;

            switch ((SeekOrigin)dwOrigin)
            {
                case SeekOrigin.Begin:
                    newPosition = dlibMove;
                    break;
                case SeekOrigin.Current:
                    if (position == -1)
                        newPosition = baseStream.Position + dlibMove;
                    else
                        newPosition = position + dlibMove;
                    break;
                case SeekOrigin.End:
                    newPosition = length + dlibMove;
                    break;
                default:
                    throw new COMException(null, STG_E_INVALIDFUNCTION);
            }

            if (newPosition > length)
                position = newPosition;
            else
            {
                baseStream.Position = newPosition;
                position = -1;
            }

            if (plibNewPosition != IntPtr.Zero)
                Marshal.WriteInt64(plibNewPosition, newPosition);
        }

        public void SetSize(long libNewSize)
        {
            baseStream.SetLength(libNewSize);
        }

        public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
        {
            byte[] buffer;
            long written = 0;
            int read;
            int count;

            if (cb != 0)
            {
                if (cb < 4096)
                    count = (int)cb;
                else
                    count = 4096;
                buffer = new byte[count];
                SetSizeToPosition();
                while (true)
                {
                    if ((read = baseStream.Read(buffer, 0, count)) == 0)
                        break;
                    pstm.Write(buffer, read, IntPtr.Zero);
                    written += read;
                    if (written >= cb)
                        break;
                    if (cb - written < 4096)
                        count = (int)(cb - written);
                }
            }

            if (pcbRead != IntPtr.Zero)
                Marshal.WriteInt64(pcbRead, written);
            if (pcbWritten != IntPtr.Zero)
                Marshal.WriteInt64(pcbWritten, written);
        }

        public void Commit(int grfCommitFlags)
        {
            baseStream.Flush();
            SetSizeToPosition();
        }

        public void Revert()
        {
            throw new COMException(null, STG_E_INVALIDFUNCTION);
        }

        public void LockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new COMException(null, STG_E_INVALIDFUNCTION);
        }

        public void UnlockRegion(long libOffset, long cb, int dwLockType)
        {
            throw new COMException(null, STG_E_INVALIDFUNCTION);
        }

        public void Stat(out STATSTG pstatstg, int grfStatFlag)
        {
            pstatstg = new STATSTG();
            pstatstg.cbSize = baseStream.Length;
        }

        public void Clone(out IStream ppstm)
        {
            ppstm = null;
            throw new COMException(null, STG_E_INVALIDFUNCTION);
        }
    }
}