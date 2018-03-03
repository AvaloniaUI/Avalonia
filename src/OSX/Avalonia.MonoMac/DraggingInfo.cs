using System.Collections.Generic;
using Avalonia.Controls.DragDrop;
using MonoMac.AppKit;

namespace Avalonia.MonoMac
{
    class DraggingInfo : IDragData
    {
        private readonly NSDraggingInfo _info;

        public DraggingInfo(NSDraggingInfo info)
        {
            _info = info;
        }


        internal static NSDragOperation ConvertDragOperation(DragOperation d)
        {
            NSDragOperation result = NSDragOperation.None;
            if (d.HasFlag(DragOperation.Copy))
                result |= NSDragOperation.Copy;
            if (d.HasFlag(DragOperation.Link))
                result |= NSDragOperation.Link;
            if (d.HasFlag(DragOperation.Move))
                result |= NSDragOperation.Move;
            return result;
        }
        
        internal static DragOperation ConvertDragOperation(NSDragOperation d)
        {
            DragOperation result = DragOperation.None;
            if (d.HasFlag(NSDragOperation.Copy))
                result |= DragOperation.Copy;
            if (d.HasFlag(NSDragOperation.Link))
                result |= DragOperation.Link;
            if (d.HasFlag(NSDragOperation.Move))
                result |= DragOperation.Move;
            return result;
        }

        public Point Location => new Point(_info.DraggingLocation.X, _info.DraggingLocation.Y);
        
        public IEnumerable<string> GetDataFormats()
        {
            yield break;
        }

        public string GetText()
        {
            return null;
        }

        public IEnumerable<string> GetFileNames()
        {
            yield break;
        }

        public bool Contains(string dataFormat)
        {
            return false;
        }
    }
}