using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Windowing.Bindings;

namespace Avalonia.Windowing
{
    public class DragSource : IPlatformDragSource
    {
        [DllImport("winit_wrapper")]
        private static extern void winit_wrapper_begin_drag(IntPtr nsview, LogicalPosition position);

        private const string NSPasteboardTypeString = "public.utf8-plain-text";
        private const string NSPasteboardTypeFileUrl = "public.file-url";

        private readonly Subject<DragDropEffects> _result = new Subject<DragDropEffects>();
        private readonly IInputManager _inputManager;
        private DragDropEffects _allowedEffects;

        //public override bool IgnoreModifierKeysWhileDragging => false;

        public DragSource()
        {
            _inputManager = AvaloniaLocator.Current.GetService<IInputManager>();
        }

        private string DataFormatToUTI(string s)
        {
            if (s == DataFormats.FileNames)
                return NSPasteboardTypeFileUrl;
            if (s == DataFormats.Text)
                return NSPasteboardTypeString;
            return s;
        }

        /*private NSDraggingItem CreateDraggingItem(string format, object data)
        {
            var pasteboardItem = new NSPasteboardItem();
            NSData nsData;
            if (data is string s)
            {
                if (format == DataFormats.FileNames)
                    s = new Uri(s).AbsoluteUri; // Ensure file uris...
                nsData = NSData.FromString(s);
            }
            else if (data is Stream strm)
                nsData = NSData.FromStream(strm);
            else if (data is byte[] bytes)
                nsData = NSData.FromArray(bytes);
            else
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    bf.Serialize(ms, data);
                    ms.Position = 0;
                    nsData = NSData.FromStream(ms);
                }
            }
            pasteboardItem.SetDataForType(nsData, DataFormatToUTI(format));

            NSPasteboardWriting writing = new NSPasteboardWriting(pasteboardItem.Handle);

            return new NSDraggingItem(writing);
        }

        public IEnumerable<NSDraggingItem> CreateDraggingItems(string format, object data)
        {
            if (format == DataFormats.FileNames && data is IEnumerable<string> files)
            {
                foreach (var file in files)
                    yield return CreateDraggingItem(format, file);

                yield break;
            }

            yield return CreateDraggingItem(format, data);
        }*/


        public async Task<DragDropEffects> DoDragDrop(IDataObject data, DragDropEffects allowedEffects)
        {
            // We need the TopLevelImpl + a mouse location so we just wait for the next event.
            var mouseEv = await _inputManager.PreProcess.OfType<RawMouseEventArgs>().FirstAsync();

            var view = ((mouseEv.Root as TopLevel)?.PlatformImpl as WindowImpl);
            if (view == null)
                return DragDropEffects.None;

            // 1) convert the mouseEv.Position to LocalPoint on the view.
            // Prepare the source event:
            var pt = new LogicalPosition { X =  mouseEv.Position.X, Y = mouseEv.Position.Y };

            // 2) generate an ns mouse event;
            //var ev = NSEvent.MouseEvent(NSEventType.LeftMouseDown, pt, 0, 0, 0, null, 0, 0, 0);

            _allowedEffects = allowedEffects;

            // 3) create NSDraggingItem from the data.
            //var items = data.GetDataFormats().SelectMany(fmt => CreateDraggingItems(fmt, data.Get(fmt))).ToArray();

            // 4) Call BeginDraggingSession on the view.
            //view.BeginDraggingSession(items, ev, this);
            winit_wrapper_begin_drag(view.WindowWrapper.NSView, pt);

            return await _result;
        }

        /*public override NSDragOperation DraggingSourceOperationMaskForLocal(bool flag)
        {
            return DraggingInfo.ConvertDragOperation(_allowedEffects);
        }*/

        /*public override void DraggedImageEndedAtOperation(NSImage image, CGPoint screenPoint, NSDragOperation operation)
        {
            _result.OnNext(DraggingInfo.ConvertDragOperation(operation));
            _result.OnCompleted();
        }*/
    }

}
