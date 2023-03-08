//
//  AvnTextInputMethod.mm
//  Avalonia.Native.OSX
//
//  Created by Benedikt Stebner on 23.11.22.
//  Copyright © 2022 Avalonia. All rights reserved.
//

#include "AvnTextInputMethod.h"

AvnTextInputMethod::~AvnTextInputMethod() {
    Client = nullptr;
}

AvnTextInputMethod::AvnTextInputMethod(id<AvnTextInputMethodDelegate> inputMethodDelegate) {
    _inputMethodDelegate = inputMethodDelegate;
}

bool AvnTextInputMethod::IsActive() {
    return Client != nullptr;
}

HRESULT AvnTextInputMethod::SetClient(IAvnTextInputMethodClient *client) {
    START_COM_CALL;
    
    Client = client;
    
    return S_OK;
}

void AvnTextInputMethod::Reset() {
}

void AvnTextInputMethod::SetSurroundingText(char* text, int anchorOffset, int cursorOffset) {
    [_inputMethodDelegate setText:[NSString stringWithUTF8String:text]];
    [_inputMethodDelegate setSelection: anchorOffset : cursorOffset];
}

void AvnTextInputMethod::SetCursorRect(AvnRect rect) {
    [_inputMethodDelegate setCursorRect: rect];
}
