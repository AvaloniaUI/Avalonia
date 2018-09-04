using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using MonoMac.AppKit;
using MonoMac.Foundation;

namespace Avalonia.MonoMac
{
    class DraggingInfo : IDataObject
    { 
        private readonly NSDraggingInfo _info;

        public DraggingInfo(NSDraggingInfo info)
        {
            _info = info;
        }
        
        internal static NSDragOperation ConvertDragOperation(DragDropEffects d)
        {
            NSDragOperation result = NSDragOperation.None;
            if (d.HasFlag(DragDropEffects.Copy))
                result |= NSDragOperation.Copy;
            if (d.HasFlag(DragDropEffects.Link))
                result |= NSDragOperation.Link;
            if (d.HasFlag(DragDropEffects.Move))
                result |= NSDragOperation.Move;
            return result;
        }
        
        internal static DragDropEffects ConvertDragOperation(NSDragOperation d)
        {
            DragDropEffects result = DragDropEffects.None;
            if (d.HasFlag(NSDragOperation.Copy))
                result |= DragDropEffects.Copy;
            if (d.HasFlag(NSDragOperation.Link))
                result |= DragDropEffects.Link;
            if (d.HasFlag(NSDragOperation.Move))
                result |= DragDropEffects.Move;
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

        public object Get(string dataFormat)
        {
            if (dataFormat == DataFormats.Text)
                return GetText();
            if (dataFormat == DataFormats.FileNames)
                return GetFileNames();

            return _info.DraggingPasteboard.GetDataForType(dataFormat).ToArray();
        }
    }
}
