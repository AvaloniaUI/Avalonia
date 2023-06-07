//
// Created by Dan Walmsley on 05/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#include "AvnView.h"
#include "AutoFitContentView.h"
#include "WindowInterfaces.h"
#include "WindowProtocol.h"

@implementation AutoFitContentView
{
    NSVisualEffectView* _titleBarMaterial;
    NSBox* _titleBarUnderline;
    NSView* _content;
    NSVisualEffectView* _blurBehind;
    double _titleBarHeightHint;
    bool _settingSize;
}

-(AutoFitContentView* _Nonnull) initWithContent:(NSView *)content
{
    _titleBarHeightHint = -1;
    _content = content;
    _settingSize = false;

    [self setAutoresizesSubviews:true];
    [self setWantsLayer:true];

    _titleBarMaterial = [NSVisualEffectView new];
    [_titleBarMaterial setBlendingMode:NSVisualEffectBlendingModeWithinWindow];
    [_titleBarMaterial setMaterial:NSVisualEffectMaterialTitlebar];
    [_titleBarMaterial setWantsLayer:true];
    _titleBarMaterial.hidden = true;

    _titleBarUnderline = [NSBox new];
    _titleBarUnderline.boxType = NSBoxSeparator;
    _titleBarUnderline.fillColor = [NSColor underPageBackgroundColor];
    _titleBarUnderline.hidden = true;

    [self addSubview:_titleBarMaterial];
    [self addSubview:_titleBarUnderline];

    _blurBehind = [NSVisualEffectView new];
    [_blurBehind setBlendingMode:NSVisualEffectBlendingModeBehindWindow];
    [_blurBehind setMaterial:NSVisualEffectMaterialLight];
    [_blurBehind setWantsLayer:true];
    _blurBehind.hidden = true;

    [_blurBehind setAutoresizingMask:NSViewWidthSizable | NSViewHeightSizable];
    [_content setAutoresizingMask:NSViewWidthSizable | NSViewHeightSizable];

    [self addSubview:_blurBehind];
    [self addSubview:_content];

    [self setWantsLayer:true];
    return self;
}

-(void) ShowBlur:(bool)show
{
    _blurBehind.hidden = !show;
}

-(void) ShowTitleBar: (bool) show
{
    _titleBarMaterial.hidden = !show;
    _titleBarUnderline.hidden = !show;
}

-(void) SetTitleBarHeightHint: (double) height
{
    _titleBarHeightHint = height;

    [self setFrameSize:self.frame.size];
}

-(void)setFrameSize:(NSSize)newSize
{
    if(_settingSize)
    {
        return;
    }

    _settingSize = true;
    [super setFrameSize:newSize];

    auto window = (id <AvnWindowProtocol>) [self window];

    // TODO get actual titlebar size

    double height = _titleBarHeightHint == -1 ? [window getExtendedTitleBarHeight] : _titleBarHeightHint;

    NSRect tbar;
    tbar.origin.x = 0;
    tbar.origin.y = newSize.height - height;
    tbar.size.width = newSize.width;
    tbar.size.height = height;

    [_titleBarMaterial setFrame:tbar];
    tbar.size.height = height < 1 ? 0 : 1;
    [_titleBarUnderline setFrame:tbar];

    _settingSize = false;
}
@end