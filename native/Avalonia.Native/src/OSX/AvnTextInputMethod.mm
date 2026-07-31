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

void AvnTextInputMethod::SetSurroundingText(char* text, int start, int end) {
    // stringWithUTF8String: throws on a null pointer and returns nil for invalid UTF-8.
    NSString* surroundingText = text != nullptr ? [NSString stringWithUTF8String:text] : nil;

    [_inputMethodDelegate setText:surroundingText != nil ? surroundingText : @""];
    [_inputMethodDelegate setSelection: start:end];
}

void AvnTextInputMethod::SetCursorRect(AvnRect rect) {
    [_inputMethodDelegate setCursorRect: rect];
}

void AvnTextInputMethod::SetSelectionInSurroundingText(int start, int end) {
    [_inputMethodDelegate setSelection: start:end];
}
