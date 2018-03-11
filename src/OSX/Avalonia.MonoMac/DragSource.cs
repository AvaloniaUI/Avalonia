using System.Data;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.DragDrop;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using MonoMac.AppKit;
using MonoMac.CoreGraphics;
using MonoMac.Foundation;
using MonoMac.OpenGL;

namespace Avalonia.MonoMac
{
    public class DragSource : NSDraggingSource, IPlatformDragSource
    {
        class DS : NSDraggingSource
        {
            private readonly DragDropEffects _allowedEffects;
            public ReplaySubject<DragDropEffects> Result { get; } = new ReplaySubject<DragDropEffects>();

            public DS(DragDropEffects allowedEffects)
            {
                _allowedEffects = allowedEffects;
            }
            
            public override bool IgnoreModifierKeysWhileDragging => false;
            
            public override NSDragOperation DraggingSourceOperationMaskForLocal(bool flag)
            {
                return DraggingInfo.ConvertDragOperation(_allowedEffects);
            }
            
            public override void DraggedImageEndedAtOperation(NSImage image, CGPoint screenPoint, NSDragOperation operation)
            {
                Result.OnNext(DraggingInfo.ConvertDragOperation(operation));
                Result.OnCompleted();
            }
        }

        
        
        private readonly IInputManager _inputManager;
        

        public DragSource()
        {
            _inputManager = AvaloniaLocator.Current.GetService<IInputManager>();
        }

        private NSDraggingItem[] ConvertDraggedItems(IDataObject data)
        {
            NSString s = new NSString("FOOBAR");
            NSPasteboardWriting w = new NSPasteboardWriting(s.Handle);
            var text = new NSDraggingItem(w);
            return new[] {text};
        }
        
        
        public async Task<DragDropEffects> DoDragDrop(IDataObject data, DragDropEffects allowedEffects)
        {
            // We need the TopLevelImpl + a mouse location so we just wait for the next event.
            var mouseEv = await _inputManager.PreProcess.OfType<RawMouseEventArgs>().FirstAsync();
            var view = ((mouseEv.Root as TopLevel)?.PlatformImpl as TopLevelImpl)?.View;
            if (view == null)
                return DragDropEffects.None;

            // Prepare the source event:
            var pt = view.TranslateLocalPoint(mouseEv.Position).ToMonoMacPoint();    
            var ev = NSEvent.MouseEvent(NSEventType.LeftMouseDown, pt, 0, 0, 0, null, 0, 0, 0);

            var ds = new DS(allowedEffects);
            view.BeginDraggingSession(ConvertDraggedItems(data) ,ev, ds);

            return await ds.Result;
        }
    }
}