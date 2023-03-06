#ifndef AVALONIA_NATIVE_OSX_WINDOWOVERLAYIMPL_H
#define AVALONIA_NATIVE_OSX_WINDOWOVERLAYIMPL_H

#include "common.h"
#include "WindowImpl.h"
#include "AvnView.h"

class WindowOverlayImpl : public virtual WindowImpl
{
private:
    NSWindow* parentWindow;
    NSView* parentView;
    FORWARD_IUNKNOWN()
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    END_INTERFACE_MAP()
public:
    WindowOverlayImpl(void* parentWindow, char* parentView, IAvnWindowEvents* events, IAvnGlContext* gl);
    virtual bool IsOverlay() override;
    virtual HRESULT PointToClient(AvnPoint point, AvnPoint *ret) override;
    virtual HRESULT PointToScreen(AvnPoint point, AvnPoint *ret) override;
    virtual HRESULT GetPosition(AvnPoint *ret) override;
};

#endif
