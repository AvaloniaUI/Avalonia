//
//  SandboxBookmark.h
//  Avalonia.Native.OSX
//
//  Created by Mikolaytis Sergey on 03.09.2021.
//  Copyright © 2021 Avalonia. All rights reserved.
//
#ifndef SandboxBookmark_h
#define SandboxBookmark_h

extern IAvnSandboxBookmark* CreateSandboxBookmark(NSURL* url);
extern IAvnSandboxBookmark* CreateSandboxBookmark(NSData* data);
#endif /* SandboxBookmark */
