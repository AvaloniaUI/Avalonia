//
// Created by Dan Walmsley on 04/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <AppKit/AppKit.h>
#include "ResizeScope.h"
#include "AvnView.h"

ResizeScope::ResizeScope(AvnView *view, AvnPlatformResizeReason reason) {
    _view = view;
    _restore = [view getResizeReason];
    [view setResizeReason:reason];
}

ResizeScope::~ResizeScope() {
    [_view setResizeReason:_restore];
}
