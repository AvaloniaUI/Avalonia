using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WindowHandle = System.IntPtr;

namespace Avalonia.X11
{
    internal class X11DataTransmitter
    {
        private readonly IntPtr _display;
        private readonly WindowHandle _handle;
        private readonly X11Atoms _atoms;

        private readonly int _maxSize = 8 * 1024; //8Kb

        private IncrementalTransfer? _activeTransfer;

        public X11DataTransmitter(IntPtr handle, X11Info info)
        {
            _handle = handle;
            _display = info.Display;
            _atoms = info.Atoms;
        }

        public void StartTransfer(ref XSelectionRequestEvent requestEvent, Input.IDataObject dataObject)
        {
            byte[] dataBytes = X11DataObject.ToTransfer(dataObject,
                    X11DataObject.MimeFormatToDataFormat(_atoms.GetAtomName(requestEvent.target) ?? string.Empty));

            if (dataBytes.Length > _maxSize)
            {
                StartIncrementalTransfer(requestEvent.requestor, requestEvent.property, requestEvent.target, dataBytes);

            }
            else
            {

                // Set property with data
                XLib.XChangeProperty(
                    _display,
                    requestEvent.requestor,
                    requestEvent.property,
                    requestEvent.target,
                    8,
                    PropertyMode.Replace,
                    dataBytes,
                    dataBytes.Length
                );
            }
            
        }

        public void HandlePropertyNotify(ref XPropertyEvent propEvent)
        {           
            if (_activeTransfer.HasValue)
            {
                if (propEvent.state == (int)PropertyNotification.Delete)
                {
                    var transfer = _activeTransfer.Value;
                    transfer.WaitEvent.Set();
                }
            }           
        }

        public void Dispose()
        {
            if (_activeTransfer.HasValue)
            {
                var transfer = _activeTransfer.Value;
                transfer.IsAborted = true;
                transfer.WaitEvent.Set();
            }               
        }

        private void StartIncrementalTransfer(IntPtr requestor, IntPtr property, IntPtr target, byte[] data)
        {
            var transfer = new IncrementalTransfer(data, target, property, requestor);

           _activeTransfer = transfer;

            XLib.XChangeProperty(
                _display,
                requestor,
                property,
                _atoms.INCR,
                32,
                PropertyMode.Replace,
                Array.Empty<byte>(),
                0
            );

            Task.Run(() => ProcessIncrementalTransfer(transfer));
        }

        private void ProcessIncrementalTransfer(IncrementalTransfer transfer)
        {
            try
            {
                int chunkSize = Math.Max(1024, (int)(_maxSize * 0.75));

                while (transfer.Offset < transfer.Data.Length && !transfer.IsAborted)
                {
                    int remaining = transfer.Data.Length - transfer.Offset;
                    int currentChunkSize = Math.Min(chunkSize, remaining);

                    byte[] chunk = new byte[currentChunkSize];
                    Buffer.BlockCopy(transfer.Data, transfer.Offset, chunk, 0, currentChunkSize);

                    XLib.XChangeProperty(
                        _display,
                        transfer.Requestor,
                        transfer.Property,
                        transfer.Target,
                        8,
                        PropertyMode.Replace,
                        chunk,
                        currentChunkSize
                    );

                    transfer.Offset += currentChunkSize;
                    transfer.WaitEvent.Reset();

                    if (!transfer.WaitEvent.Wait(TimeSpan.FromSeconds(5)))
                    {
                        transfer.IsAborted = true;
                        break;
                    }
                }

                if (!transfer.IsAborted)
                {
                    XLib.XChangeProperty(
                        _display,
                        transfer.Requestor,
                        transfer.Property,
                        transfer.Target,
                        8,
                        PropertyMode.Replace,
                        Array.Empty<byte>(),
                        0
                    );
                }
            }
            finally
            {
               _activeTransfer = null;
            }
        }


        private struct IncrementalTransfer
        {
            public IncrementalTransfer(byte[] data, WindowHandle target, IntPtr property, IntPtr requestor)
            {
                Data = data;
                Target = target;
                Property = property;
                Requestor = requestor;
                Offset = 0;
            }

            public byte[] Data { get; }
            public WindowHandle Target { get; }
            public IntPtr Property { get; }
            public IntPtr Requestor { get; }
            public int Offset { get; set; }
            public ManualResetEventSlim WaitEvent { get; } = new ManualResetEventSlim();
            public bool IsAborted { get; set; } = false;
        }

    }
}
