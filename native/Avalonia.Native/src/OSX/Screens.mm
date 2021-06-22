#include "common.h"

class Screens : public ComSingleObject<IAvnScreens, &IID_IAvnScreens>
{
    public:
    FORWARD_IUNKNOWN()
    
public:
    virtual HRESULT GetScreenCount (int* ret) override
    {
        @autoreleasepool
        {
            *ret = (int)[NSScreen screens].count;
            
            return S_OK;
        }
    }
    
    virtual HRESULT GetScreen (int index, AvnScreen* ret) override
    {
        @autoreleasepool
        {
            if(index < 0 || index >= [NSScreen screens].count)
            {
                return E_INVALIDARG;
            }
            
            auto screen = [[NSScreen screens] objectAtIndex:index];
            
            ret->Bounds.Height = [screen frame].size.height;
            ret->Bounds.Width = [screen frame].size.width;
            ret->Bounds.X = [screen frame].origin.x;
            ret->Bounds.Y = ConvertPointY(ToAvnPoint([screen frame].origin)).Y - ret->Bounds.Height;
            
            ret->WorkingArea.Height = [screen visibleFrame].size.height;
            ret->WorkingArea.Width = [screen visibleFrame].size.width;
            ret->WorkingArea.X = [screen visibleFrame].origin.x;
            ret->WorkingArea.Y = ConvertPointY(ToAvnPoint([screen visibleFrame].origin)).Y - ret->WorkingArea.Height;
            
            ret->PixelDensity = [screen backingScaleFactor];
            
            ret->Primary = index == 0;
            
            return S_OK;
        }
    }
};

extern IAvnScreens* CreateScreens()
{
    return new Screens();
}
