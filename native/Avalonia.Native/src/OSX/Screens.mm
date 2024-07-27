#include "common.h"
#include "AvnString.h"

class Screens : public ComSingleObject<IAvnScreens, &IID_IAvnScreens>
{
private:
    ComPtr<IAvnScreenEvents> _events;
public:
    FORWARD_IUNKNOWN()

    Screens(IAvnScreenEvents* events) {
        _events = events;
        CGDisplayRegisterReconfigurationCallback(CGDisplayReconfigurationCallBack, this);
    }

    virtual HRESULT GetScreenIds (
        unsigned int* ptrFirstResult,
        int* screenCound) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto screens = [NSScreen screens];
            *screenCound = (int)screens.count;

            if (ptrFirstResult == nil)
                return S_OK;

            for (int i = 0; i < screens.count; i++) {
                ptrFirstResult[i] = [[screens objectAtIndex:i] av_displayId];
            }

            return S_OK;
        }
    }

    virtual HRESULT GetScreen (
       CGDirectDisplayID displayId,
       void** localizedName,
       AvnScreen* ret
    ) override {
        START_COM_CALL;
        
        @autoreleasepool
        {
            NSScreen* screen;
            for (NSScreen *s in NSScreen.screens) {
                if (s.av_displayId == displayId)
                {
                    screen = s;
                    break;
                }
            }
            
            if (screen == nil) {
                return E_INVALIDARG;
            }

            ret->Bounds.Height = [screen frame].size.height;
            ret->Bounds.Width = [screen frame].size.width;
            ret->Bounds.X = [screen frame].origin.x;
            ret->Bounds.Y = ConvertPointY(ToAvnPoint([screen frame].origin)).Y - ret->Bounds.Height;
            
            ret->WorkingArea.Height = [screen visibleFrame].size.height;
            ret->WorkingArea.Width = [screen visibleFrame].size.width;
            ret->WorkingArea.X = [screen visibleFrame].origin.x;
            ret->WorkingArea.Y = ConvertPointY(ToAvnPoint([screen visibleFrame].origin)).Y - ret->WorkingArea.Height;
            
            ret->Scaling = 1;
            
            ret->IsPrimary = CGDisplayIsMain(displayId);

            // Compute natural orientation:
            auto naturalScreenSize = CGDisplayScreenSize(displayId);
            auto isNaturalLandscape = naturalScreenSize.width > naturalScreenSize.height;
            // Normalize rotation:
            auto rotation = (int)CGDisplayRotation(displayId) % 360;
            if (rotation < 0) rotation = 360 - rotation;
            // Get current orientation relative to the natural
            if (rotation >= 0 && rotation < 90) {
                ret->Orientation = isNaturalLandscape ? AvnScreenOrientation::Landscape : AvnScreenOrientation::Portrait;
            } else if (rotation >= 90 && rotation < 180) {
                ret->Orientation = isNaturalLandscape ? AvnScreenOrientation::Portrait : AvnScreenOrientation::Landscape;
            } else if (rotation >= 180 && rotation < 270) {
                ret->Orientation = isNaturalLandscape ? AvnScreenOrientation::LandscapeFlipped : AvnScreenOrientation::PortraitFlipped;
            } else {
                ret->Orientation = isNaturalLandscape ? AvnScreenOrientation::PortraitFlipped : AvnScreenOrientation::LandscapeFlipped;
            }

            if (@available(macOS 10.15, *)) {
                *localizedName = CreateAvnString([screen localizedName]);
            }

            return S_OK;
        }
    }

private:
    static void CGDisplayReconfigurationCallBack(CGDirectDisplayID display, CGDisplayChangeSummaryFlags flags, void *screens)
    {
        auto object = (Screens *)screens;
        auto events = object->_events;
        if (events != nil) {
            events->OnChanged();
        }
    }
};

extern IAvnScreens* CreateScreens(IAvnScreenEvents* events)
{
    return new Screens(events);
}
