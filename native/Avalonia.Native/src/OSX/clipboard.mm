#import <UniformTypeIdentifiers/UniformTypeIdentifiers.h>
#include "common.h"
#include "clipboard.h"
#include "AvnString.h"

class Clipboard : public ComSingleObject<IAvnClipboard, &IID_IAvnClipboard>
{
private:
    NSPasteboard* _pasteboard;
public:
    FORWARD_IUNKNOWN()
    
    Clipboard(NSPasteboard* pasteboard)
    {
        if (pasteboard == nil)
            pasteboard = [NSPasteboard generalPasteboard];

        _pasteboard = pasteboard;
    }
    
    virtual HRESULT GetFormats(int64_t changeCount, ComPtr<IAvnStringArray>& ret) override
    {
        if (changeCount != [_pasteboard changeCount])
            return COR_E_OBJECTDISPOSED;

        ret = ConvertPasteboardTypes([_pasteboard types]);
        return S_OK;
    }

    virtual HRESULT GetItemCount(int64_t changeCount, int* ret) override
    {
        if (ret == nullptr)
            return E_POINTER;
        
        if (changeCount != [_pasteboard changeCount])
            return COR_E_OBJECTDISPOSED;
        
        auto items = [_pasteboard pasteboardItems];
        *ret = items == nil ? 0 : (int)[items count];
        return S_OK;
    }
    
    virtual HRESULT GetItemFormats(int index, int64_t changeCount, ComPtr<IAvnStringArray>& ret) override
    {
        if (changeCount != [_pasteboard changeCount])
            return COR_E_OBJECTDISPOSED;

        auto item = [[_pasteboard pasteboardItems] objectAtIndex:index];
        
        ret = ConvertPasteboardTypes([item types]);
        return S_OK;
    }

    static ComPtr<IAvnStringArray> ConvertPasteboardTypes(NSArray<NSPasteboardType> *types)
    {
        if (types != nil)
        {
            NSMutableArray<NSString *> *mutableTypes = [types mutableCopy];

            // Add png if format list doesn't have PNG,
            // but has any other image type that can be converter into PNG
            if (![mutableTypes containsObject:NSPasteboardTypePNG])
            {
                if ([mutableTypes containsObject:NSPasteboardTypeTIFF]
                    || [mutableTypes containsObject:@"public.jpeg"])
                {
                    [mutableTypes addObject: NSPasteboardTypePNG];
                }
            }

            return CreateAvnStringArray(mutableTypes);
        }

        return nullptr;
    }

    virtual HRESULT GetItemValueAsString(int index, int64_t changeCount, const char* format, ComPtr<IAvnString>& ret) override
    {
        if (changeCount != [_pasteboard changeCount])
            return COR_E_OBJECTDISPOSED;
        
        auto item = [[_pasteboard pasteboardItems] objectAtIndex:index];
        auto value = [item stringForType:[NSString stringWithUTF8String:format]];
        ret = value == nil ? nullptr : CreateAvnString(value);
        return S_OK;
    }
    
    virtual HRESULT GetItemValueAsBytes(int index, int64_t changeCount, const char* format, ComPtr<IAvnString>& ret) override
    {
        if (changeCount != [_pasteboard changeCount])
            return COR_E_OBJECTDISPOSED;
        
        auto item = [[_pasteboard pasteboardItems] objectAtIndex:index];
        auto formatStr = [NSString stringWithUTF8String:format];
        
        auto value = [item dataForType: formatStr];

        // If PNG wasn't found, try to convert TIFF or JPEG to PNG
        if (value == nil && [formatStr isEqualToString: NSPasteboardTypePNG])
        {
            NSData *imageData = nil;

            // Try TIFF first
            imageData = [item dataForType:NSPasteboardTypeTIFF];

            // If no TIFF, try JPEG
            if (imageData == nil) {
                imageData = [item dataForType:@"public.jpeg"];
            }

            if (imageData != nil)
            {
                auto image = [[NSImage alloc] initWithData:imageData];

                NSBitmapImageRep *bitmapRep = nil;
                for (NSImageRep *rep in image.representations) {
                    if ([rep isKindOfClass:[NSBitmapImageRep class]]) {
                        bitmapRep = (NSBitmapImageRep *)rep;
                        break;
                    }
                }

                if (!bitmapRep) {
                    [image lockFocus];
                    bitmapRep = [[NSBitmapImageRep alloc] initWithFocusedViewRect:NSMakeRect(0, 0, image.size.width, image.size.height)];
                    [image unlockFocus];
                }

                value = [bitmapRep representationUsingType:NSBitmapImageFileTypePNG properties:@{}];
            }
        }

        ret = value == nil || [value length] == 0
            ? nullptr
            : CreateByteArray((void*)[value bytes], (int)[value length]);
        return S_OK;
    }

    virtual HRESULT Clear(int64_t* ret) override
    {
        *ret = [_pasteboard clearContents];
        return S_OK;
    }
    
    virtual HRESULT GetChangeCount(int64_t* ret) override
    {
        *ret = [_pasteboard changeCount];
        return S_OK;
    }
    
    virtual HRESULT SetData(const ComPtr<IAvnClipboardDataSource>& source) override
    {
        auto count = source->GetItemCount();
        auto writeableItems = [NSMutableArray<WriteableClipboardItem*> arrayWithCapacity:count];
        
        for (auto i = 0; i < count; ++i)
        {
            ComPtr<IAvnClipboardDataItem> item;
            if(S_OK != source->GetItem(i, item))
                continue;
            auto writeableItem = [[WriteableClipboardItem alloc] initWithItem:item source:source];
            [writeableItems addObject:writeableItem];
        }
        
        [_pasteboard writeObjects:writeableItems];
        return S_OK;
    }
    
    virtual bool IsTextFormat(const char *format) override
    {
        auto formatString = [NSString stringWithUTF8String:format];
        
        if (@available(macOS 11.0, *))
        {
            auto type = [UTType typeWithIdentifier:formatString];
            return type != nil && [type conformsToType:UTTypeText];
        }
        else
        {
            return UTTypeConformsTo((__bridge CFStringRef)formatString, kUTTypeText);
        }
    }
};


extern ComPtr<IAvnClipboard> CreateClipboard(NSPasteboard* pb)
{
    return comnew<Clipboard>(pb);
}


@implementation WriteableClipboardItem
{
    ComPtr<IAvnClipboardDataItem> _item;
    ComPtr<IAvnClipboardDataSource> _source;
}
    
- (nonnull WriteableClipboardItem*) initWithItem:(const ComPtr<IAvnClipboardDataItem>&)item source:(const ComPtr<IAvnClipboardDataSource>&)source
{
    self = [super init];
    _item = item;
    _source = source;
    
    return self;
}

NSString* TryConvertFormatToUti(NSString* format)
{
    if (@available(macOS 11.0, *))
    {
        auto type = [UTType typeWithIdentifier:format];
        if (type == nil)
        {
            if ([format containsString:@"/"])
                type = [UTType typeWithMIMEType:format];
            else
                type = [UTType exportedTypeWithIdentifier:format];
            
            if (type == nil)
            {
                // For now, we need to use the deprecated UTTypeCreatePreferredIdentifierForTag to create a dynamic UTI for arbitrary strings.
                // This is only necessary because the old IDataObject can provide arbitrary types that aren't UTIs nor mime types.
                // With the new DataFormat:
                //   - If the format is an application format, the managed side provides a UTI like net.avaloniaui.app.uti.xxx.
                //   - If the format is an OS format, the user has been warned that they MUST provide a name which is valid for the OS.
                // TODO12: remove!
                auto fromPasteboardType = UTTypeCreatePreferredIdentifierForTag(kUTTagClassNSPboardType, (__bridge CFStringRef)format, nil);
                if (fromPasteboardType != nil)
                    return (__bridge_transfer NSString*)fromPasteboardType;
            }
        }
        
        return type == nil ? nil : [type identifier];
    }
    else
    {
        auto bridgedFormat = (__bridge CFStringRef)format;
        if (UTTypeIsDeclared(bridgedFormat))
            return format;
        
        auto fromMimeType = UTTypeCreatePreferredIdentifierForTag(kUTTagClassMIMEType, bridgedFormat, nil);
        if (fromMimeType != nil)
            return (__bridge_transfer NSString*)fromMimeType;
        
        auto fromPasteboardType = UTTypeCreatePreferredIdentifierForTag(kUTTagClassNSPboardType, bridgedFormat, nil);
        if (fromPasteboardType != nil)
            return (__bridge_transfer NSString*)fromPasteboardType;
        
        return nil;
    }
}

- (nonnull NSArray<NSPasteboardType>*) writableTypesForPasteboard:(nonnull NSPasteboard*)pasteboard
{
    ComPtr<IAvnStringArray> formats;
    auto hr = _item->ProvideFormats(formats);
    if (hr != S_OK || formats == nullptr)
        return [NSArray array];
    
    auto count = formats->GetCount();
    if (count == 0)
        return [NSArray array];
    
    auto utis = [NSMutableArray arrayWithCapacity:count];
    for (auto i = 0; i < count; ++i)
    {
        ComPtr<IAvnString> format;
        if (formats->Get(i, format) != S_OK)
            continue;
    
        // Only UTIs must be returned from writableTypesForPasteboard or an exception will be thrown
        auto formatString = GetNSString(format);
        auto uti = TryConvertFormatToUti(formatString);
        if (uti != nil)
            [utis addObject:uti];
    }
    
    [utis addObject:GetAvnCustomDataType()];
    
    return utis;
}

- (NSPasteboardWritingOptions) writingOptionsForType:(NSPasteboardType)type pasteboard:(NSPasteboard*)pasteboard
{
    return [type isEqualToString:NSPasteboardTypeString] || [type isEqualToString:GetAvnCustomDataType()]
        ? 0
        : NSPasteboardWritingPromised;
}

- (nullable id) pasteboardPropertyListForType:(nonnull NSPasteboardType)type
{
    if ([type isEqualToString:GetAvnCustomDataType()])
        return @"";
    
    ComPtr<IAvnClipboardDataValue> value;
    HRESULT hr = _item->GetValue([type UTF8String], value);
    if (hr != S_OK || value == nullptr)
        return nil;
    
    if (value->IsString())
        return GetNSString(value->AsString());
    
    auto length = value->GetByteLength();
    auto buffer = malloc(length);
    value->CopyBytesTo(buffer);
    return [NSData dataWithBytesNoCopy:buffer length:length];
}

@end
