#include "common.h"
#import <Cocoa/Cocoa.h>

class NotificationManager : public ComSingleObject<IAvnNotificationManager, &IID_IAvnNotificationManager>
{
public:
   FORWARD_IUNKNOWN()
    
    virtual bool ShowNotification(AvnNotification* notification) override {
        NSUserNotification* cocoaNotification = [NSUserNotification new];
        
        //cocoaNotification.identifier = @"unique-id";
        cocoaNotification.title = [NSString stringWithUTF8String:notification->TitleUtf8];
        cocoaNotification.informativeText = [NSString stringWithUTF8String:notification->TextUtf8];
        
        NSMutableDictionary* userData = [NSMutableDictionary dictionary];
        [userData setValue:[NSValue valueWithPointer:(const void*) notification->ActionCallback]
                  forKey:@"actionCallback"];
        [userData setValue:[NSValue valueWithPointer:(const void*) notification->CloseCallback]
                  forKey:@"closeCallback"];
        cocoaNotification.userInfo = userData;
  
        [[NSUserNotificationCenter defaultUserNotificationCenter]
             deliverNotification:cocoaNotification];
        
        if (notification->durationMs != -1) {
            unsigned long long durationNs = notification->durationMs * NSEC_PER_MSEC;
            
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, durationNs), dispatch_get_main_queue(), ^{
                [[NSUserNotificationCenter defaultUserNotificationCenter]
                 removeDeliveredNotification:cocoaNotification];
            });
        }
        
        return YES;
    }
    
};

extern IAvnNotificationManager* CreateNotificationManager()
{
    @autoreleasepool
    {
        return new NotificationManager();
    }
}
