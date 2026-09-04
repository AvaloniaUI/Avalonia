#include "common.h"
#include "trayicon.h"
#include "menu.h"

@implementation AvnStatusItem
{
    ComObjectWeakPtr<AvnTrayIcon> _owner;
    NSStatusItem* _native;
}

- (NSStatusItem*) statusItem
{
    return _native;
}

- (instancetype) initWithOwner: (AvnTrayIcon*) owner
{
    self = [super init];
    if (self != nullptr)
    {
        _owner = owner;
        _native = [[NSStatusBar systemStatusBar] statusItemWithLength: NSSquareStatusItemLength];

        auto button = [_native button];
        [button setTarget: self];
        [button setAction: @selector(clicked:)];
        [button sendActionOn: NSEventMaskLeftMouseUp | NSEventMaskRightMouseUp];
    }
    return self;
}

- (void) clicked: (id) sender
{
    auto owner = _owner.tryGet();
    if (owner != nullptr)
    {
        owner->OnClicked([NSApp currentEvent]);
    }
}

- (void) dispose
{
    _owner = nullptr;

    if (_native != nullptr)
    {
        [[_native statusBar] removeStatusItem: _native];
        _native = nullptr;
    }
}
@end

extern IAvnTrayIcon* CreateTrayIcon()
{
    @autoreleasepool
    {
        return new AvnTrayIcon();
    }
}

AvnTrayIcon::AvnTrayIcon()
{
    _isTemplateIcon = false;
    _menu = nullptr;
    _native = [[AvnStatusItem alloc] initWithOwner: this];
}

AvnTrayIcon::~AvnTrayIcon()
{
    [_native dispose];
    
    _menu = nullptr;
    _native = nullptr;
    _clickedCallback = nullptr;
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
            size.height = floor([[NSFont menuFontOfSize:0] pointSize] * 1.333333);

            auto scaleFactor = size.height / originalSize.height;
            size.width = floor(originalSize.width * scaleFactor);
            
            [image setSize: size];
            [image setTemplate: _isTemplateIcon];
            [[[_native statusItem] button] setImage: image];
        }
        else
        {
            [[[_native statusItem] button] setImage: nullptr];
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
        _menu = appMenu != nullptr ? appMenu->GetNative() : nullptr;
    }
    
    return  S_OK;
}

HRESULT AvnTrayIcon::SetIsVisible(bool isVisible)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        [[_native statusItem] setVisible: isVisible];
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
            [[[_native statusItem] button] setToolTip: [NSString stringWithUTF8String:(const char*)text]];
        }
    }
    
    return  S_OK;
}

HRESULT AvnTrayIcon::SetIsTemplateIcon(bool isTemplateIcon)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if (_isTemplateIcon != isTemplateIcon)
        {
            _isTemplateIcon = isTemplateIcon;

            NSImage* image = [[[_native statusItem] button] image];
            if (image)
            {
                [image setTemplate: _isTemplateIcon];
            }
        }
    }
    
    return  S_OK;
}

HRESULT AvnTrayIcon::SetClickedCallback(IAvnActionCallback* callback)
{
    START_COM_CALL;

    @autoreleasepool
    {
        _clickedCallback = callback;
    }

    return S_OK;
}

void AvnTrayIcon::OnClicked(NSEvent* event)
{
    if ([event type] == NSEventTypeRightMouseUp)
    {
        if (_menu != nullptr)
        {
            [NSMenu
             popUpContextMenu: _menu
             withEvent: event
             forView: [[_native statusItem] button]];
        }
    }
    else if ([event type] == NSEventTypeLeftMouseUp)
    {
        _clickedCallback->Run();
    }
}
