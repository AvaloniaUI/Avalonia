#pragma once

#import <Cocoa/Cocoa.h>
NS_ASSUME_NONNULL_BEGIN

class IAvnAutomationPeer;

@interface AvnAccessibilityElement : NSAccessibilityElement
+ (AvnAccessibilityElement *) acquire:(IAvnAutomationPeer *) peer;
@end

NS_ASSUME_NONNULL_END
