#include "common.h"

class AvnAppMenuItem : public ComSingleObject<IAvnAppMenuItem, &IID_IAvnAppMenuItem>
{
private:
    NSMenuItem* _native;
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenuItem()
    {
        _native = [NSMenuItem new];
        
        id fileMenu = [NSMenu new];
        [fileMenu setTitle:@"File"];
        
        [_native setSubmenu:fileMenu];
        
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Generate Wallet" action:NULL keyEquivalent:@""]];
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Recover Wallet" action:NULL keyEquivalent:@""]];
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Load Wallet" action:NULL keyEquivalent:@""]];
        
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Open" action:NULL keyEquivalent:@""]];
        
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Exit" action:NULL keyEquivalent:@""]];
    }
    
    NSMenuItem* Native()
    {
        return _native;
    }
    
    virtual HRESULT SetSubMenu (IAvnAppMenu* menu) override
    {
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

class AvnAppMenu : public ComSingleObject<IAvnAppMenu, &IID_IAvnAppMenu>
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
    
    virtual HRESULT AddItem (IAvnAppMenuItem* item) override
    {
        auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
        
        if(avnMenuItem != nullptr)
        {
            [_native addItem:avnMenuItem->Native()];
        }
        
        return S_OK;
    }
    
    virtual HRESULT RemoveItem (IAvnAppMenuItem* item) override
    {
        auto avnMenuItem = dynamic_cast<AvnAppMenuItem*>(item);
        
        if(avnMenuItem != nullptr)
        {
            [_native removeItem:avnMenuItem->Native()];
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
    if(s_MainAppMenu == nullptr)
    {
        s_MainAppMenu = new AvnAppMenu([[NSApplication sharedApplication] mainMenu]);
    }
    
    return s_MainAppMenu;
}

extern IAvnAppMenu* CreateAppMenu()
{   
    return new AvnAppMenu();
}

extern IAvnAppMenuItem* CreateAppMenuItem()
{
    return new AvnAppMenuItem();
}
