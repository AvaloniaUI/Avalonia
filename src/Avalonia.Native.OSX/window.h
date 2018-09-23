//
//  window.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 23/09/2018.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#ifndef window_h
#define window_h

class WindowBaseImpl;

@interface AvnView : NSView<NSTextInputClient>
-(AvnView*) initWithParent: (WindowBaseImpl*) parent;
-(NSEvent*) lastMouseDownEvent;
-(AvnPoint) translateLocalPoint:(AvnPoint)pt;
@end

@interface AvnWindow : NSWindow <NSWindowDelegate>
-(AvnWindow*) initWithParent: (WindowBaseImpl*) parent;
-(void) setCanBecomeKeyAndMain;
@end

class WindowBaseImpl : public ComSingleObject<IAvnWindowBase, &IID_IAvnWindowBase>
{
public:
    AvnView* View;
    AvnWindow* Window;
    ComPtr<IAvnWindowBaseEvents> BaseEvents;
    AvnPoint lastPositionSet;
    WindowBaseImpl(IAvnWindowBaseEvents* events)
    {
        BaseEvents = events;
        View = [[AvnView alloc] initWithParent:this];
        Window = [[AvnWindow alloc] initWithParent:this];
        
        lastPositionSet.X = 100;
        lastPositionSet.Y = 100;
        
        [Window setStyleMask:NSWindowStyleMaskBorderless];
        [Window setBackingType:NSBackingStoreBuffered];
        [Window setContentView: View];
    }
    
    virtual HRESULT Show()
    {
        SetPosition(lastPositionSet);
        UpdateStyle();
        [Window makeKeyAndOrderFront:Window];
        return S_OK;
    }
    
    virtual HRESULT Hide ()
    {
        if(Window != nullptr)
        {
            [Window orderOut:Window];
        }
        return S_OK;
    }
    
    virtual HRESULT Close()
    {
        [Window close];
        return S_OK;
    }
    
    virtual HRESULT GetClientSize(AvnSize* ret)
    {
        if(ret == nullptr)
            return E_POINTER;
        auto frame = [View frame];
        ret->Width = frame.size.width;
        ret->Height = frame.size.height;
        return S_OK;
    }
    
    virtual HRESULT GetScaling (double* ret)
    {
        if(ret == nullptr)
            return E_POINTER;
        
        if(Window == nullptr)
        {
            *ret = 1;
            return S_OK;
        }
        
        *ret = [Window backingScaleFactor];
        return S_OK;
    }
    
    virtual HRESULT Resize(double x, double y)
    {
        [Window setContentSize:NSSize{x, y}];
        return S_OK;
    }
    
    virtual void Invalidate (AvnRect rect)
    {
        [View setNeedsDisplayInRect:[View frame]];
    }
    
    virtual void BeginMoveDrag ()
    {
        auto lastEvent = [View lastMouseDownEvent];
        
        if(lastEvent == nullptr)
        {
            return;
        }
        
        [Window performWindowDragWithEvent:lastEvent];
    }
    
    
    virtual HRESULT GetPosition (AvnPoint* ret)
    {
        if(ret == nullptr)
        {
            return E_POINTER;
        }
        
        auto frame = [Window frame];
        
        ret->X = frame.origin.x;
        ret->Y = frame.origin.y + frame.size.height;
        
        *ret = ConvertPointY(*ret);
        
        return S_OK;
    }
    
    virtual void SetPosition (AvnPoint point)
    {
        lastPositionSet = point;
        [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(point))];
    }
    
    virtual HRESULT PointToClient (AvnPoint point, AvnPoint* ret)
    {
        if(ret == nullptr)
        {
            return E_POINTER;
        }
        
        point = ConvertPointY(point);
        auto viewPoint = [Window convertPointFromScreen:ToNSPoint(point)];
        
        *ret = [View translateLocalPoint:ToAvnPoint(viewPoint)];
        
        return S_OK;
    }
    
    virtual HRESULT PointToScreen (AvnPoint point, AvnPoint* ret)
    {
        if(ret == nullptr)
        {
            return E_POINTER;
        }
        
        auto cocoaViewPoint =  ToNSPoint([View translateLocalPoint:point]);
        auto cocoaScreenPoint = [Window convertPointToScreen:cocoaViewPoint];
        *ret = ConvertPointY(ToAvnPoint(cocoaScreenPoint));
        
        return S_OK;
    }
    
protected:
    virtual NSWindowStyleMask GetStyle()
    {
        return NSWindowStyleMaskBorderless;
    }
    
    void UpdateStyle()
    {
        [Window setStyleMask:GetStyle()];
    }
};

#endif /* window_h */
