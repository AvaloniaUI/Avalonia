//
//  TopLevelImpl.h
//  Avalonia.Native.OSX
//
//  Created by Benedikt Stebner on 16.05.24.
//  Copyright © 2024 Avalonia. All rights reserved.
//

#ifndef TopLevelImpl_h
#define TopLevelImpl_h

#include "rendertarget.h"
#include "INSWindowHolder.h"
#include "AvnTextInputMethod.h"
#include "AutoFitContentView.h"
#include <list>

class TopLevelImpl : public virtual ComObject,
                     public virtual MicroComImpl<IAvnTopLevel>,
                     public INSViewHolder{
    
public:
    FORWARD_IUNKNOWN()
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(IAvnTopLevel, IID_IAvnTopLevel)
    END_INTERFACE_MAP()
    
    virtual ~TopLevelImpl();
    
    TopLevelImpl(const ComPtr<IAvnTopLevelEvents>& events);
                         
    virtual AvnView *GetNSView() override;
                         
    virtual HRESULT SetCursor(const ComPtr<IAvnCursor>& cursor) override;
                         
    virtual HRESULT GetScaling(double*ret) override;
                         
    virtual HRESULT GetClientSize(AvnSize *ret) override;
                           
    virtual HRESULT GetInputMethod(ComPtr<IAvnTextInputMethod>& ppv) override;
                           
    virtual HRESULT ObtainNSViewHandle(void** retOut) override;
                                                  
    virtual HRESULT ObtainNSViewHandleRetained(void** retOut) override;
                           
    virtual HRESULT CreateSoftwareRenderTarget(ComPtr<IAvnSoftwareRenderTarget>& ret) override;
                                                  
    virtual HRESULT CreateMetalRenderTarget(const ComPtr<IAvnMetalDevice>& device, ComPtr<IAvnMetalRenderTarget>& ret) override;
                           
    virtual HRESULT CreateGlRenderTarget(const ComPtr<IAvnGlContext>& context, ComPtr<IAvnGlSurfaceRenderTarget>& ret) override;

    virtual HRESULT CreateNativeControlHost(ComPtr<IAvnNativeControlHost>& retOut) override;
                         
    virtual HRESULT Invalidate() override;
                         
    virtual HRESULT PointToClient(AvnPoint point, AvnPoint *ret) override;

    virtual HRESULT PointToScreen(AvnPoint point, AvnPoint *ret) override;
     
    virtual HRESULT SetTransparencyMode(AvnWindowTransparencyMode mode) override;

    virtual HRESULT GetCurrentDisplayId (CGDirectDisplayID* ret) override;

    virtual HRESULT BeginDragAndDropOperation(
        AvnDragDropEffects effects,
        AvnPoint point,
        const ComPtr<IAvnClipboardDataSource>& source,
        const ComPtr<IAvnDndResultCallback>& callback,
        void* sourceHandle) override;

protected:
    NSCursor *cursor;
    virtual void UpdateAppearance();
                           
public:
    NSObject<IRenderTarget> *currentRenderTarget;
    ComPtr<AvnTextInputMethod> InputMethod;
    ComPtr<IAvnTopLevelEvents> TopLevelEvents;
    AvnView *View;
                         
    void UpdateCursor();
    virtual void SetClientSize(NSSize size);
};

#endif /* TopLevelImpl_h */
