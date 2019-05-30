#include "common.h"
#include "IGetNative.h"

class AvnAppMenuItem : public ComSingleObject<IAvnAppMenuItem, &IID_IAvnAppMenuItem>, public IGetNative
{
private:
    NSMenuItem* _native;
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
        
        
        _native = [NSMenuItem new];
    }
    
    void* GetNative() override
    {
        return (__bridge void*) _native;
    }
    
    virtual HRESULT SetSubMenu (IAvnAppMenu* menu) override
    {
        auto nsMenu = (__bridge NSMenu*) dynamic_cast<IGetNative*>(menu)->GetNative();
        
        [_native setSubmenu: nsMenu];
        
        return S_OK;
    }
    
    virtual HRESULT SetTitle (void* utf8String) override
    {
        [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        
        return S_OK;
    }
    
    virtual HRESULT SetGesture (void* utf8String) override
    {
        return S_OK;
    }
    
    virtual HRESULT SetAction (IAvnActionCallback* callback) override
    {
        return S_OK;
    }
};

class AvnAppMenu : public ComSingleObject<IAvnAppMenu, &IID_IAvnAppMenu>, public IGetNative
{
private:
    NSMenu* _native;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenu()
    {
        _native = [NSMenu new];
    }
    
    AvnAppMenu(NSMenu* native)
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
            [_native addItem: (__bridge NSMenuItem*)avnMenuItem->GetNative()];
        }
        
        return S_OK;
    }
    
    virtual HRESULT RemoveItem (IAvnAppMenuItem* item) override
    {
        auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
        
        if(avnMenuItem != nullptr)
        {
            [_native removeItem:(__bridge NSMenuItem*)avnMenuItem->GetNative()];
        }
        
        return S_OK;
    }
    
    virtual HRESULT SetTitle (void* utf8String) override
    {
        [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        
        return S_OK;
    }
};

static IAvnAppMenu* s_MainAppMenu = nullptr;

extern IAvnAppMenu* GetAppMenu()
{
    @autoreleasepool
    {
        if(s_MainAppMenu == nullptr)
        {
            s_MainAppMenu = new AvnAppMenu([[NSApplication sharedApplication] mainMenu]);
            @autoreleasepool {
                
            
            id appMenu = [NSMenu new];
            [appMenu setTitle:@"AppMenu"];
            id appName = [[NSProcessInfo processInfo] processName];
            [[NSProcessInfo processInfo] setProcessName:@"Test"];
            id quitTitle = [@"Quit " stringByAppendingString:appName];
            id quitMenuItem = [[NSMenuItem alloc] initWithTitle:quitTitle
                                                         action:@selector(terminate:) keyEquivalent:@"q"];
            
            
            id testMenuItem = [[NSMenuItem alloc] initWithTitle:@"Test" action:NULL keyEquivalent:@""];
            [appMenu addItem:testMenuItem];
            [appMenu addItem:quitMenuItem];
            
            
            id appMenuItem = [NSMenuItem new];
            [[NSApp mainMenu] addItem:appMenuItem];
            
            [appMenuItem setSubmenu:appMenu];
            }
        }
        
        return s_MainAppMenu;
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
