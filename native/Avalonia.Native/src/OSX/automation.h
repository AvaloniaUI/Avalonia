#pragma once

#import <Cocoa/Cocoa.h>
#include "AvnAccessibility.h"
NS_ASSUME_NONNULL_BEGIN

@interface AvnAccessibilityElement : NSAccessibilityElement <AvnAccessibility>
+ (id _Nullable) acquire:(const ComPtr<IAvnAutomationPeer>&) peer;
@end

NS_ASSUME_NONNULL_END
