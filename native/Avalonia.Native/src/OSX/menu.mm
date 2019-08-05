
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

static IAvnAppMenu* s_AppBar = nullptr;

static IAvnAppMenu* s_AppMenu = nullptr;

extern IAvnAppMenu* GetAppMenu()
{
    @autoreleasepool
    {
        if(s_AppMenu == nullptr)
        {
            NSMenu* const mainMenu = [[NSMenu alloc] initWithTitle:@"NSMainMenu"];
            
            NSMenuItem* menuItem = [mainMenu addItemWithTitle:@"Apple" action:nil keyEquivalent:@""];
            NSMenu* submenu = [[NSMenu alloc] initWithTitle:@"Apple"];
            
            // todo populate app menu submenu!!!
            NSString * const applicationName = @"root";
            
            NSMenuItem* menuItem1 = [submenu addItemWithTitle : [NSString stringWithFormat : @"%@ %@",
                                                    NSLocalizedString(@"About", nil), applicationName]
                                          action : @selector(orderFrontStandardAboutPanel:) keyEquivalent : @""];
            [menuItem1 setTarget : NSApp];
            [submenu addItem : [NSMenuItem separatorItem]];
            
            menuItem1 = [submenu addItemWithTitle : [NSString stringWithFormat : @"%@ %@",
                                                    NSLocalizedString(@"Hide", nil), applicationName] action : @selector(hide:) keyEquivalent : @"h"];
            [menuItem1 setTarget : NSApp];
            
            //menuItem = [submenu addItemWithTitle : NSLocalizedString(@"Hide Others", nil)
              //                            action : @selector(hideOtherApplications:) keyEquivalent : @"h"];
            //[menuItem setKeyEquivalentModifierMask : Details::kCommandKeyMask | Details::kAlternateKeyMask];
            //[menuItem setTarget : NSApp];
            
            menuItem1 = [submenu addItemWithTitle : NSLocalizedString(@"Show All", nil)
                                          action : @selector(unhideAllApplications:) keyEquivalent : @""];
            [menuItem1 setTarget : NSApp];
            
            [submenu addItem : [NSMenuItem separatorItem]];
            menuItem1 = [submenu addItemWithTitle : [NSString stringWithFormat : @"%@ %@",
                                                    NSLocalizedString(@"Quit", nil), applicationName] action : @selector(terminate:) keyEquivalent : @"q"];
            //[menuItem1 setTarget : NSApp];
            //AvnMenuItem* testItem = [[AvnMenuItem alloc] initWithAvnAppMenuItem:this];
            //[testItem setTitle:@"TestItem"];
            //[submenu addItem: testItem];
            //
            
            [mainMenu setSubmenu:submenu forItem:menuItem];
            
            menuItem = [mainMenu addItemWithTitle : @"Window" action : nil keyEquivalent : @""];
            submenu = [[NSMenu alloc] initWithTitle : NSLocalizedString(@"Window", @"The Window menu")];
            
            //PopulateWindowMenu(submenu);
            [mainMenu setSubmenu : submenu forItem : menuItem];
            [NSApp setWindowsMenu : submenu];
            
            menuItem = [mainMenu addItemWithTitle:@"Help" action:NULL keyEquivalent:@""];
            submenu = [[NSMenu alloc] initWithTitle:NSLocalizedString(@"Help", @"The Help menu")];
            
            //PopulateHelpMenu(submenu);
            [mainMenu setSubmenu : submenu forItem : menuItem];
            
            [NSApp setMainMenu : mainMenu];
            [NSMenu setMenuBarVisible : YES];
            /*id appMenu = [AvnMenu new];
             [appMenu setTitle:@"AppMenu"];
             
             s_AppMenu = new AvnAppMenu(appMenu);
             
             id appName = [[NSProcessInfo processInfo] processName];
             
             id quitTitle = [@"Quit " stringByAppendingString:appName];
             id quitMenuItem = [[NSMenuItem alloc] initWithTitle:quitTitle
             action:@selector(terminate:) keyEquivalent:@"q"];
             
             
             id testMenuItem = [[NSMenuItem alloc] initWithTitle:@"Test" action:NULL keyEquivalent:@""];
             [appMenu addItem:testMenuItem];
             [appMenu addItem:quitMenuItem];
             
             [NSApp setMainMenu:appMenu];
             
             
             
             //[appMenuItem setSubmenu:appMenu];*/
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
