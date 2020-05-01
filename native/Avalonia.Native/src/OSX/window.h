#ifndef window_h
#define window_h

class WindowBaseImpl;

@interface AvnView : NSView<NSTextInputClient>
-(AvnView* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
-(NSEvent* _Nonnull) lastMouseDownEvent;
-(AvnPoint) translateLocalPoint:(AvnPoint)pt;
-(void) setSwRenderedFrame: (AvnFramebuffer* _Nonnull) fb dispose: (IUnknown* _Nonnull) dispose;
-(void) onClosed;
-(AvnPixelSize) getPixelSize;
@end

@interface AvnWindow : NSWindow <NSWindowDelegate>
+(void) closeAll;
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
-(void) setCanBecomeKeyAndMain;
-(void) pollModalSession: (NSModalSession _Nonnull) session;
-(void) restoreParentWindow;
-(bool) shouldTryToHandleEvents;
-(bool) isModal;
-(void) setModal: (bool) isModal;
-(void) showAppMenuOnly;
-(void) showWindowMenuWithAppMenu;
-(void) applyMenu:(NSMenu* _Nullable)menu;
-(double) getScaling;
@end

struct INSWindowHolder
{
    virtual AvnWindow* _Nonnull GetNSWindow () = 0;
};

struct IWindowStateChanged
{
    virtual void WindowStateChanged () = 0;
};

#endif /* window_h */
