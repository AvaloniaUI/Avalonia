#include "common.h"
#import <Cocoa/Cocoa.h>

@interface NotificationCenterDelegate : NSObject<NSUserNotificationCenterDelegate>
@property AvnNotificationActionCallback ActionCallback;
@property AvnNotificationCloseCallback CloseCallback;
@end

@implementation NotificationCenterDelegate

- (BOOL)userNotificationCenter:(NSUserNotificationCenter *)center shouldPresentNotification:(NSUserNotification *)notification
{
    // Force notifications to be always displayed even when running in foreground
    return YES;
}

-(void)userNotificationCenter:(NSUserNotificationCenter *)center didActivateNotification:(NSUserNotification *)notification
{
    if (notification.activationType != NSUserNotificationActivationTypeContentsClicked &&
        notification.activationType != NSUserNotificationActivationTypeActionButtonClicked) {
        return;
    }
    
    [self ActionCallback]([notification.identifier intValue]);
}
@end

class NotificationManager : public ComSingleObject<IAvnNotificationManager, &IID_IAvnNotificationManager>
{
    NotificationCenterDelegate* _notificationCenterDelegate;
    
public:
   FORWARD_IUNKNOWN()
    
    NotificationManager() {
      _notificationCenterDelegate = [NotificationCenterDelegate new];
    }
    
    virtual bool ShowNotification(AvnNotification* notification) override {

        NSUserNotificationCenter* notificationCenter = [NSUserNotificationCenter defaultUserNotificationCenter];
        notificationCenter.delegate = _notificationCenterDelegate;
        
        NSUserNotification* cocoaNotification = [NSUserNotification new];
        
        int notificationId = notification->identifier;
        cocoaNotification.identifier = [NSString stringWithFormat:@"%i", notificationId];
        cocoaNotification.title = [NSString stringWithUTF8String:notification->TitleUtf8];
        cocoaNotification.informativeText = [NSString stringWithUTF8String:notification->TextUtf8];

        [notificationCenter deliverNotification:cocoaNotification];
        
        if (notification->durationMs != 0) {
            unsigned long long durationNs = notification->durationMs * NSEC_PER_MSEC;
  
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, durationNs), dispatch_get_main_queue(), ^{
                [notificationCenter removeDeliveredNotification:cocoaNotification];
                
                //TODO: HACK: I can't find a global close event... Might be missing something.
                _notificationCenterDelegate.CloseCallback(notificationId);
            });
        }
        
        return YES;
    }
    
    virtual void SetCloseCallback(AvnNotificationCloseCallback callback) override {
        _notificationCenterDelegate.CloseCallback = callback;
    }
    
    virtual void SetActionCallback(AvnNotificationActionCallback callback) override {
        _notificationCenterDelegate.ActionCallback = callback;
    }
    
};

extern IAvnNotificationManager* CreateNotificationManager()
{
    @autoreleasepool
    {
        return new NotificationManager();
    }
}
