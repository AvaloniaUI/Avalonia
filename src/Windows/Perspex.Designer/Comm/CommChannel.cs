using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Perspex.Designer.Comm
{
    internal class CommChannel : IDisposable
    {
        private readonly BinaryReader _input;
        private readonly BinaryWriter _output;
        private SynchronizationContext _dispatcher;
        TaskCompletionSource<bool> _terminating = new TaskCompletionSource<bool>();
        private BlockingCollection<byte[]> _outputQueue = new BlockingCollection<byte[]>();
        public event Action<object> OnMessage;
        public event Action Disposed;
        public CommChannel(Stream input, Stream output)
        {
            _input = new BinaryReader(input);
            _output = new BinaryWriter(output);
            _dispatcher = SynchronizationContext.Current;

        }

        public void Start()
        {
            new Thread(WriterThread) { IsBackground = true }.Start();
            new Thread(ReaderThread) { IsBackground = true }.Start();
        }

        public void SendMessage(object message)
        {
            var fmt = new BinaryFormatter();
            var ms = new MemoryStream();
            fmt.Serialize(ms, message);
            _outputQueue.Add(ms.ToArray());
        }

        void WriterThread()
        {
            while (!_terminating.Task.IsCompleted)
            {
                var item = _outputQueue.Take();
                if(_terminating.Task.IsCompleted)
                    return;
                try
                {
                    _output.Write(item.Length);
                    _output.Write(item);
                    _output.Flush();
                }
                catch (Exception e)
                {
                    Dispose();
                }
            }
        }

        private async Task<byte[]> ReadAsyncOrNull(int count)
        {
            var readTask = Task.Factory.StartNew(() => _input.ReadBytes(count));
            await Task.WhenAny(readTask, _terminating.Task);
            if (_terminating.Task.IsCompleted)
                return null;
            return await readTask;
        }

        async void ReaderThread()
        {
            var fmt = new BinaryFormatter();
            while (!_terminating.Task.IsCompleted)
            {
                try
                {
                    var lenb = await ReadAsyncOrNull(4);
                    if (lenb == null)
                        return;
                    var data = await ReadAsyncOrNull(BitConverter.ToInt32(lenb, 0));
                    if (data == null)
                        return;
                    var message = fmt.Deserialize(new MemoryStream(data));
                    _dispatcher.Post(_ => OnMessage?.Invoke(message), null);
                }
                catch
                {
                    Dispose();
                    return;
                }
            }

        }

        public void Dispose()
        {
            if(_terminating.Task.IsCompleted)
                return;
            _terminating.TrySetResult(true);
            _outputQueue.Add(null);
            Disposed?.Invoke();
        }
    }
}
