//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#ifndef AVALONIA_NATIVE_OSX_RESIZESCOPE_H
#define AVALONIA_NATIVE_OSX_RESIZESCOPE_H

#include "avalonia-native.h"

@class AvnView;

class ResizeScope
{
public:
    ResizeScope(AvnView* _Nonnull view, AvnPlatformResizeReason reason);

    ~ResizeScope();
private:
    AvnView* _Nonnull _view;
    AvnPlatformResizeReason _restore;
};

#endif //AVALONIA_NATIVE_OSX_RESIZESCOPE_H
