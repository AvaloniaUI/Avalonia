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
-(void) setSwRenderedFrame: (AvnFramebuffer*) fb dispose: (IUnknown*) dispose;
@end

@interface AvnWindow : NSWindow <NSWindowDelegate>
-(AvnWindow*) initWithParent: (WindowBaseImpl*) parent;
-(void) setCanBecomeKeyAndMain;
@end

struct INSWindowHolder
{
    virtual AvnWindow* GetNSWindow () = 0;
};

#endif /* window_h */
