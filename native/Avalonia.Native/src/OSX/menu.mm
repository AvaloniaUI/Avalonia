
#include "common.h"
#include "menu.h"
#include "KeyTransform.h"
#include <CoreFoundation/CoreFoundation.h>
#include <Carbon/Carbon.h> /* For kVK_ constants, and TIS functions. */

@implementation AvnMenu
{
    bool _isReparented;
    NSObject<NSMenuDelegate>* _wtf;
}

- (id) initWithDelegate: (NSObject<NSMenuDelegate>*)del
{
    self = [super init];
    self.delegate = del;
    _wtf = del;
    _isReparented = false;
    return self;
}

- (bool)hasGlobalMenuItem
{
    return _isReparented;
}

- (void)setHasGlobalMenuItem:(bool)value
{
    _isReparented = value;
}

@end

@implementation AvnMenuItem
{
    AvnAppMenuItem* _item;
}

- (id) initWithAvnAppMenuItem: (AvnAppMenuItem*)menuItem
{
    if(self != nil)
    {
        _item = menuItem;
        self = [super initWithTitle:@""
                             action:@selector(didSelectItem:)
                      keyEquivalent:@""];
        
        [self setEnabled:YES];
        
        [self setTarget:self];
    }
    
    return self;
}

- (BOOL)validateMenuItem:(NSMenuItem *)menuItem
{
    if([self submenu] != nil)
    {
        return YES;
    }
    
    return _item->EvaluateItemEnabled();
}

- (void)didSelectItem:(nullable id)sender
{
    _item->RaiseOnClicked();
}
@end

AvnAppMenuItem::AvnAppMenuItem(bool isSeparator)
{
    _isCheckable = false;

    if(isSeparator)
    {
        _native = [NSMenuItem separatorItem];
    }
    else
    {
        _native = [[AvnMenuItem alloc] initWithAvnAppMenuItem: this];
    }
    
    _callback = nullptr;
}

NSMenuItem* AvnAppMenuItem::GetNative()
{
    return _native;
}

HRESULT AvnAppMenuItem::SetSubMenu (IAvnMenu* menu)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if(menu != nullptr)
        {
            auto nsMenu = dynamic_cast<AvnAppMenu*>(menu)->GetNative();
            
            [_native setSubmenu: nsMenu];
        }
        else
        {
            [_native setSubmenu: nullptr];
        }
        
        return S_OK;
    }
}

HRESULT AvnAppMenuItem::SetTitle (char* utf8String)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if (utf8String != nullptr)
        {
            [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        }
        
        return S_OK;
    }
}


HRESULT AvnAppMenuItem::SetGesture (AvnKey key, AvnInputModifiers modifiers)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if(key != AvnKeyNone)
        {
            NSEventModifierFlags flags = 0;
            
            if (modifiers & Control)
                flags |= NSEventModifierFlagControl;
            if (modifiers & Shift)
                flags |= NSEventModifierFlagShift;
            if (modifiers & Alt)
                flags |= NSEventModifierFlagOption;
            if (modifiers & Windows)
                flags |= NSEventModifierFlagCommand;
            
            auto it = s_UnicodeKeyMap.find(key);
            
            if(it != s_UnicodeKeyMap.end())
            {
                auto keyString= [NSString stringWithFormat:@"%C", (unsigned short)it->second];
                
                [_native setKeyEquivalent: keyString];
                [_native setKeyEquivalentModifierMask:flags];
                
                return S_OK;
            }
            else
            {
                auto it = s_AvnKeyMap.find(key); // check if a virtual key is mapped.
                
                if(it != s_AvnKeyMap.end())
                {
                    auto it1 = s_QwertyKeyMap.find(it->second); // convert virtual key to qwerty string.
                    
                    if(it1 != s_QwertyKeyMap.end())
                    {
                        [_native setKeyEquivalent: [NSString  stringWithUTF8String: it1->second]];
                        [_native setKeyEquivalentModifierMask:flags];
                        
                        return S_OK;
                    }
                }
            }
        }
        
        // Nothing matched... clear.
        [_native setKeyEquivalent: @""];
        [_native setKeyEquivalentModifierMask: 0];
        
        return S_OK;
    }
}

HRESULT AvnAppMenuItem::SetAction (IAvnPredicateCallback* predicate, IAvnActionCallback* callback)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        _predicate = predicate;
        _callback = callback;
        return S_OK;
    }
}

HRESULT AvnAppMenuItem::SetIsChecked (bool isChecked)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        [_native setState:(isChecked && _isCheckable ? NSOnState : NSOffState)];
        return S_OK;
    }
}

HRESULT AvnAppMenuItem::SetToggleType(AvnMenuItemToggleType toggleType)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        switch(toggleType)
        {
            case AvnMenuItemToggleType::None:
                [_native setOnStateImage: [NSImage imageNamed:@"NSMenuCheckmark"]];
                
                _isCheckable = false;
                break;
                
            case AvnMenuItemToggleType::CheckMark:
                [_native setOnStateImage: [NSImage imageNamed:@"NSMenuCheckmark"]];
                
                _isCheckable = true;
                break;
                
            case AvnMenuItemToggleType::Radio:
                [_native setOnStateImage: [NSImage imageNamed:@"NSMenuItemBullet"]];
                
                _isCheckable = true;
                break;
        }
        
        return S_OK;
    }
}

HRESULT AvnAppMenuItem::SetIcon(void *data, size_t length)
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

bool AvnAppMenuItem::EvaluateItemEnabled()
{
    if(_predicate != nullptr)
    {
        auto result = _predicate->Evaluate ();
        
        return result;
    }
    
    return false;
}

void AvnAppMenuItem::RaiseOnClicked()
{
    if(_callback != nullptr)
    {
        _callback->Run();
    }
}

AvnAppMenu::AvnAppMenu(IAvnMenuEvents* events)
{
    _baseEvents = events;
    _delegate = [[AvnMenuDelegate alloc] initWithParent: this];
    _native = [[AvnMenu alloc] initWithDelegate: _delegate];
}

AvnAppMenu::~AvnAppMenu()
{
    [_delegate parentDestroyed];
}


AvnMenu* AvnAppMenu::GetNative()
{
    return _native;
}

void AvnAppMenu::RaiseNeedsUpdate()
{
    if(_baseEvents != nullptr)
    {
        _baseEvents->NeedsUpdate();
    }
}

void AvnAppMenu::RaiseOpening()
{
    if(_baseEvents != nullptr)
    {
        _baseEvents->Opening();
    }
}

void AvnAppMenu::RaiseClosed()
{
    if(_baseEvents != nullptr)
    {
        _baseEvents->Closed();
    }
}


HRESULT AvnAppMenu::InsertItem(int index, IAvnMenuItem *item)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if([_native hasGlobalMenuItem])
        {
            index++;
        }
        
        auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
        
        if(avnMenuItem != nullptr)
        {
            [_native insertItem: avnMenuItem->GetNative() atIndex:index];
        }
        
        return S_OK;
    }
}

HRESULT AvnAppMenu::RemoveItem (IAvnMenuItem* item)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
        
        if(avnMenuItem != nullptr)
        {
            [_native removeItem:avnMenuItem->GetNative()];
        }
        
        return S_OK;
    }
}

HRESULT AvnAppMenu::SetTitle (char* utf8String)
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        if (utf8String != nullptr)
        {
            [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        }
        
        return S_OK;
    }
}

HRESULT AvnAppMenu::Clear()
{
    START_COM_CALL;
    
    @autoreleasepool
    {
        [_native removeAllItems];
        return S_OK;
    }
}

@implementation AvnMenuDelegate
{
    AvnAppMenu* _parent;
}
- (id) initWithParent:(AvnAppMenu *)parent
{
    self = [super init];
    _parent = parent;
    return self;
}

- (void) parentDestroyed
{
    _parent = nullptr;
}

- (BOOL)menu:(NSMenu *)menu updateItem:(NSMenuItem *)item atIndex:(NSInteger)index shouldCancel:(BOOL)shouldCancel
{
    if(shouldCancel)
        return NO;
    return YES;
}

- (NSInteger)numberOfItemsInMenu:(NSMenu *)menu
{
    return [menu numberOfItems];
}

- (void)menuNeedsUpdate:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseNeedsUpdate();
}

- (void)menuWillOpen:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseOpening();
}

- (void)menuDidClose:(NSMenu *)menu
{
    if(_parent)
        _parent->RaiseClosed();
}

@end

extern IAvnMenu* CreateAppMenu(IAvnMenuEvents* cb)
{
    @autoreleasepool
    {
        return new AvnAppMenu(cb);
    }
}

extern IAvnMenuItem* CreateAppMenuItem()
{
    @autoreleasepool
    {
        return new AvnAppMenuItem(false);
    }
}

extern IAvnMenuItem* CreateAppMenuItemSeparator()
{
    @autoreleasepool
    {
        return new AvnAppMenuItem(true);
    }
}

static IAvnMenu* s_appMenu = nullptr;
static NSMenuItem* s_appMenuItem = nullptr;

extern void SetAppMenu(IAvnMenu *menu)
{
    s_appMenu = menu;
    
    if(s_appMenu != nullptr)
    {
        auto nativeMenu = dynamic_cast<AvnAppMenu*>(s_appMenu);
        
        auto currentMenu = [s_appMenuItem menu];
        
        if (currentMenu != nullptr)
        {
            [currentMenu removeItem:s_appMenuItem];
        }
        
        s_appMenuItem = [nativeMenu->GetNative() itemAtIndex:0];
        
        if (currentMenu == nullptr)
        {
            currentMenu = [s_appMenuItem menu];
        }
        
        [[s_appMenuItem menu] removeItem:s_appMenuItem];
        
        [currentMenu insertItem:s_appMenuItem atIndex:0];
        
        if([s_appMenuItem submenu] == nullptr)
        {
            [s_appMenuItem setSubmenu:[NSMenu new]];
        }
    }
    else
    {
        s_appMenuItem = nullptr;
    }
}

extern void SetServicesMenu (IAvnMenu* menu)
{
    auto nativeMenu = dynamic_cast<AvnAppMenu*>(menu);
    [NSApplication sharedApplication].servicesMenu = nativeMenu->GetNative();
}

extern IAvnMenu* GetAppMenu ()
{
    return s_appMenu;
}

extern NSMenuItem* GetAppMenuItem ()
{
    return s_appMenuItem;
}


