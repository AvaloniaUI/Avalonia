#include "common.h"
#include "IGetNative.h"


@interface AvnMenu : NSMenu // for some reason it doesnt detect nsmenu here but compiler doesnt complain
-(void) myaction;

@end

@interface AvnMenuItem : NSMenuItem
-(void) myaction; // added myaction method
@end

@implementation AvnMenu

- (BOOL)validateMenuItem:(NSMenuItem *)menuItem
{
    return YES;
}

- (void)myaction
{
    
}

@end

@implementation AvnMenuItem

- (BOOL)validateMenuItem:(NSMenuItem *)menuItem
{
    return YES;
}

- (void)myaction
{
    // here its implemented...
}

@end

class AvnAppMenuItem : public ComSingleObject<IAvnAppMenuItem, &IID_IAvnAppMenuItem>, public IGetNative
{
private:
    AvnMenuItem* _native; // here we hold a pointer to an AvnMenuItem
    IAvnActionCallback* _callback;
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenuItem()
    {
            /*id appMenu = [NSMenu new];
            [appMenu setTitle:@"MenuTitle"];
            
            id testMenuItem = [[NSMenuItem alloc] initWithTitle:@"Test" action:NULL keyEquivalent:@""];
            [appMenu addItem:testMenuItem];
            
            
            id appMenuItem = [NSMenuItem new];
            [[NSApp mainMenu] addItem:appMenuItem];
            
            [appMenuItem setSubmenu:appMenu];*/
        
        
        _native = [AvnMenuItem new];
        [_native setKeyEquivalent:@" "];
        [_native setEnabled:YES];
        
        _callback = nullptr;
    }
    
    void* GetNative() override
    {
        return (__bridge void*) _native;
    }
    
    virtual HRESULT SetSubMenu (IAvnAppMenu* menu) override
    {
        auto nsMenu = (__bridge AvnMenu*) dynamic_cast<IGetNative*>(menu)->GetNative();
        
        [_native setSubmenu: nsMenu];
        
        return S_OK;
    }
    
    virtual HRESULT SetTitle (void* utf8String) override
    {
        [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        
        // here I want to call my method...
        [_native myaction];
        
        return S_OK;
    }
    
    virtual HRESULT SetGesture (void* utf8String) override
    {
        return S_OK;
    }
    
    virtual HRESULT SetAction (IAvnActionCallback* callback) override
    {
        auto cb = [[ActionCallback alloc] initWithCallback:callback];
        [_native setTarget:_native];
        [_native setAction:@selector(myaction:)];
        [_native setEnabled:YES];
        return S_OK;
    }
};

class AvnAppMenu : public ComSingleObject<IAvnAppMenu, &IID_IAvnAppMenu>, public IGetNative
{
private:
    AvnMenu* _native;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenu()
    {
        _native = [AvnMenu new];
        [_native setAutoenablesItems:YES];
    }
    
    AvnAppMenu(AvnMenu* native)
    {
        _native = native;
    }
    
    void* GetNative() override
    {
        return (__bridge void*) _native;
    }
    
    virtual HRESULT AddItem (IAvnAppMenuItem* item) override
    {
        auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
        
        if(avnMenuItem != nullptr)
        {
            [_native addItem: (__bridge AvnMenuItem*)avnMenuItem->GetNative()];
        }
        
        return S_OK;
    }
    
    virtual HRESULT RemoveItem (IAvnAppMenuItem* item) override
    {
        auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
        
        if(avnMenuItem != nullptr)
        {
            [_native removeItem:(__bridge AvnMenuItem*)avnMenuItem->GetNative()];
        }
        
        return S_OK;
    }
    
    virtual HRESULT SetTitle (void* utf8String) override
    {
        [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        
        return S_OK;
    }
};

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


