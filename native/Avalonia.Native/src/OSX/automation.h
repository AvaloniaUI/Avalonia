#pragma once

#import <Cocoa/Cocoa.h>
#include "AvnAccessibility.h"
NS_ASSUME_NONNULL_BEGIN

class IAvnAutomationPeer;

@interface AvnAccessibilityElement : NSAccessibilityElement <AvnAccessibility>
+ (id _Nullable) acquire:(IAvnAutomationPeer *) peer;
@end

NS_ASSUME_NONNULL_END
