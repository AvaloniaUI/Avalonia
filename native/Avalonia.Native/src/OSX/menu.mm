#include "common.h"


class AvnAppMenu : public ComSingleObject<IAvnAppMenu, &IID_IAvnAppMenu>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual HRESULT AddItem (IAvnAppMenuItem* item) override
    {
        return S_OK;
    }
    
    virtual HRESULT RemoveItem (IAvnAppMenuItem* item) override
    {
        return S_OK;
    }
    
    virtual HRESULT SetTitle (const char* title) override
    {
        return S_OK;
    }
};

class AvnAppMenuItem : public ComSingleObject<IAvnAppMenuItem, &IID_IAvnAppMenuItem>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual HRESULT SetSubMenu (IAvnAppMenu* menu) override
    {
        return S_OK;
    }
    
    virtual HRESULT SetTitle (const char* title) override
    {
        return S_OK;
    }
    
    virtual HRESULT SetGesture (const char* gesture) override
    {
        return S_OK;
    }
    
    virtual HRESULT SetAction (IAvnActionCallback* callback) override
    {
        return S_OK;
    }
};
