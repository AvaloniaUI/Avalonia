#include "common.h"
#include "trayicon.h"
#include "menu.h"

extern IAvnTrayIcon* CreateTrayIcon()
{
    @autoreleasepool
    {
        return new AvnTrayIcon();
    }
}

AvnTrayIcon::AvnTrayIcon()
{
    _native = [[NSStatusBar systemStatusBar] statusItemWithLength: NSSquareStatusItemLength];
    
}

AvnTrayIcon::~AvnTrayIcon()
{
    if(_native != nullptr)
    {
        [[_native statusBar] removeStatusItem:_native];
        _native = nullptr;
    }
}

HRESULT AvnTrayIcon::SetIcon (void* data, size_t length)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if(data != nullptr)
        {
            NSData *imageData = [NSData dataWithBytes:data length:length];
            NSImage *image = [[NSImage alloc] initWithData:imageData];
            
            NSSize originalSize = [image size];
             
            NSSize size;
            size.height = [[NSFont menuFontOfSize:0] pointSize] * 1.333333;
            
            auto scaleFactor = size.height / originalSize.height;
            size.width = originalSize.width * scaleFactor;
            
            [image setSize: size];
            [_native setImage:image];
        }
        else
        {
            [_native setImage:nullptr];
        }
        return S_OK;
    }
}

HRESULT AvnTrayIcon::SetMenu (IAvnMenu* menu)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        auto appMenu = dynamic_cast<AvnAppMenu*>(menu);
        
        if(appMenu != nullptr)
        {
            [_native setMenu:appMenu->GetNative()];	
        }
    }
    
    return  S_OK;
}

HRESULT AvnTrayIcon::SetIsVisible(bool isVisible)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        [_native setVisible:isVisible];
    }
    
    return  S_OK;
}

HRESULT AvnTrayIcon::SetToolTipText(char* text)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if (text != nullptr)
        {
            [[_native button] setToolTip:[NSString stringWithUTF8String:(const char*)text]];
        }
    }
    
    return  S_OK;
}
