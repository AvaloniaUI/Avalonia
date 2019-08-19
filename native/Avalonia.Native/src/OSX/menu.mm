
#include "common.h"
#include "IGetNative.h"
#include "menu.h"

@implementation AvnMenu
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

AvnAppMenuItem::AvnAppMenuItem()
{
    _native = [[AvnMenuItem alloc] initWithAvnAppMenuItem: this];
    _callback = nullptr;
}

AvnMenuItem* AvnAppMenuItem::GetNative()
{
    return _native;
}

HRESULT AvnAppMenuItem::SetSubMenu (IAvnAppMenu* menu)
{
    auto nsMenu = dynamic_cast<AvnAppMenu*>(menu)->GetNative();
    
    [_native setSubmenu: nsMenu];
    
    return S_OK;
}

HRESULT AvnAppMenuItem::SetTitle (void* utf8String)
{
    [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
    
    return S_OK;
}

HRESULT AvnAppMenuItem::SetGesture (void* utf8String)
{
    return S_OK;
}

HRESULT AvnAppMenuItem::SetAction (IAvnPredicateCallback* predicate, IAvnActionCallback* callback)
{
    _predicate = predicate;
    _callback = callback;
    return S_OK;
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

AvnAppMenu::AvnAppMenu()
{
    _native = [AvnMenu new];
}

AvnAppMenu::AvnAppMenu(AvnMenu* native)
{
    _native = native;
}

AvnMenu* AvnAppMenu::GetNative()
{
    return _native;
}

HRESULT AvnAppMenu::AddItem (IAvnAppMenuItem* item)
{
    auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
    
    if(avnMenuItem != nullptr)
    {
        
        [_native addItem: avnMenuItem->GetNative()];
    }
    
    return S_OK;
}

HRESULT AvnAppMenu::RemoveItem (IAvnAppMenuItem* item)
{
    auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
    
    if(avnMenuItem != nullptr)
    {
        [_native removeItem:avnMenuItem->GetNative()];
    }
    
    return S_OK;
}

HRESULT AvnAppMenu::SetTitle (void* utf8String)
{
    [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
    
    return S_OK;
}

HRESULT AvnAppMenu::Clear()
{
    [_native removeAllItems];
    return S_OK;
}

static IAvnAppMenu* s_AppBar = nullptr;

static IAvnAppMenu* s_AppMenu = nullptr;

extern IAvnAppMenu* GetAppMenu()
{
    @autoreleasepool
    {
        if(s_AppMenu == nullptr)
        {
            id appMenu = [AvnMenu new];
            [appMenu setTitle:@"AppMenu"];
            
            s_AppMenu = new AvnAppMenu(appMenu);
            
            id appName = [[NSProcessInfo processInfo] processName];
            
            //id quitTitle = [@"Quit " stringByAppendingString:appName];
            //d quitMenuItem = [[NSMenuItem alloc] initWithTitle:quitTitle
            //                                          action:@selector(terminate:) keyEquivalent:@"q"];
            
            
            //id testMenuItem = [[NSMenuItem alloc] initWithTitle:@"Test" action:NULL keyEquivalent:@""];
            //[appMenu addItem:testMenuItem];
            //   [appMenu addItem:quitMenuItem];
            
            id appMenuItem = [AvnMenuItem new];
            [[NSApp mainMenu] addItem:appMenuItem];
            
            [appMenuItem setSubmenu:appMenu];
        }
        
        return s_AppMenu;
    }
}

extern IAvnAppMenu* GetAppBar()
{
    @autoreleasepool
    {
        if(s_AppBar == nullptr)
        {
            s_AppBar = new AvnAppMenu([[NSApplication sharedApplication] mainMenu]);
        }
        
        return s_AppBar;
    }
}

extern IAvnAppMenu* CreateAppMenu()
{
    @autoreleasepool
    {
        return new AvnAppMenu();
    }
}

extern IAvnAppMenuItem* CreateAppMenuItem()
{
    @autoreleasepool
    {
        return new AvnAppMenuItem();
    }
}
