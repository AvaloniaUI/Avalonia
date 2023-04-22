//
//  AvnTextInputMethod.h
//  Avalonia.Native.OSX
//
//  Created by Benedikt Stebner on 22.11.22.
//  Copyright © 2022 Avalonia. All rights reserved.
//

#ifndef AvnTextInputMethod_h
#define AvnTextInputMethod_h

#import <Foundation/Foundation.h>

#include "com.h"
#include "comimpl.h"
#include "avalonia-native.h"
#import "AvnTextInputMethodDelegate.h"

class AvnTextInputMethod: public virtual ComObject, public virtual IAvnTextInputMethod{
private:
    id<AvnTextInputMethodDelegate> _inputMethodDelegate;
public:
    FORWARD_IUNKNOWN()
    
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(IAvnTextInputMethod, IID_IAvnTextInputMethod)
    END_INTERFACE_MAP()
    
    virtual ~AvnTextInputMethod();
    
    AvnTextInputMethod(id<AvnTextInputMethodDelegate> inputMethodDelegate);
    
    bool IsActive ();
    
    HRESULT SetClient (IAvnTextInputMethodClient* client) override;
    
    virtual void Reset () override;
    
    virtual void SetCursorRect (AvnRect rect) override;
    
    virtual void SetSurroundingText (char* text, int anchorOffset, int cursorOffset) override;
    
public:
    ComPtr<IAvnTextInputMethodClient> Client;
};
#endif /* AvnTextInputMethod_h */
