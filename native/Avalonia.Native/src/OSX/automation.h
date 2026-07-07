#pragma once

#import <Cocoa/Cocoa.h>
#include "AvnAccessibility.h"
NS_ASSUME_NONNULL_BEGIN

class IAvnAutomationPeer;

@interface AvnAccessibilityElement : NSAccessibilityElement <AvnAccessibility>
+ (id _Nullable) acquire:(IAvnAutomationPeer *) peer;
+ (NSArray * _Nullable) filterVisibleChildren:(NSArray * _Nullable) children;
@end

NS_ASSUME_NONNULL_END
