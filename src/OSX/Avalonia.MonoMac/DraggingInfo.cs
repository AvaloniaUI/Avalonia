using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.DragDrop;
using MonoMac.AppKit;
using MonoMac.Foundation;

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
            return _info.DraggingPasteboard.Types.Select(NSTypeToWellknownType);
        }

        private string NSTypeToWellknownType(string type)
        {
            if (type == NSPasteboard.NSStringType)
                return DataFormats.Text;
            if (type == NSPasteboard.NSFilenamesType)
                return DataFormats.FileNames;
            return type;
        }

        public string GetText()
        {
            return _info.DraggingPasteboard.GetStringForType(NSPasteboard.NSStringType);
        }

        public IEnumerable<string> GetFileNames()
        {
            using(var fileNames = (NSArray)_info.DraggingPasteboard.GetPropertyListForType(NSPasteboard.NSFilenamesType))
            {
                if (fileNames != null)
                    return NSArray.StringArrayFromHandle(fileNames.Handle);
            }

            return Enumerable.Empty<string>();
        }

        public bool Contains(string dataFormat)
        {
            return GetDataFormats().Any(f => f == dataFormat);
        }
    }
}