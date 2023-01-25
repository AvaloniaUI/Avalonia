//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_IWINDOWSTATECHANGED_H
#define AVALONIA_NATIVE_OSX_IWINDOWSTATECHANGED_H

struct IWindowStateChanged
{
    virtual void WindowStateChanged () = 0;
    virtual void StartStateTransition () = 0;
    virtual void EndStateTransition () = 0;
    virtual SystemDecorations Decorations () = 0;
    virtual AvnWindowState WindowState () = 0;
};

#endif //AVALONIA_NATIVE_OSX_IWINDOWSTATECHANGED_H
