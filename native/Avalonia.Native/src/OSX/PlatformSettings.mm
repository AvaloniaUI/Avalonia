#include "common.h"
#include "AvnString.h"

@interface CocoaThemeObserver : NSObject
-(id)initWithCallback:(IAvnActionCallback *)callback;
@end

@interface CocoaLocaleObserver : NSObject
-(id)initWithCallback:(IAvnActionCallback *)callback;
-(void)localeDidChange:(NSNotification *)notification;
@end

class PlatformSettings : public ComSingleObject<IAvnPlatformSettings, &IID_IAvnPlatformSettings>
{
    CocoaThemeObserver* observer;
    CocoaLocaleObserver* localeObserver;

public:
    FORWARD_IUNKNOWN()
    virtual AvnPlatformThemeVariant GetPlatformTheme() override
    {
        @autoreleasepool
        {
            if (@available(macOS 10.14, *))
            {
                if (NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAqua
                    || NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameVibrantLight) {
                    return AvnPlatformThemeVariant::Light;
                } else if (NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameDarkAqua
                    || NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameVibrantDark) {
                    return AvnPlatformThemeVariant::Dark;
                } else if (NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAccessibilityHighContrastAqua
                    || NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAccessibilityHighContrastVibrantLight) {
                    return AvnPlatformThemeVariant::HighContrastLight;
                } else if (NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAccessibilityHighContrastDarkAqua
                    || NSApplication.sharedApplication.effectiveAppearance.name == NSAppearanceNameAccessibilityHighContrastVibrantDark) {
                    return AvnPlatformThemeVariant::HighContrastDark;
                }
            }
            return AvnPlatformThemeVariant::Light;
        }
    }
    
    virtual unsigned int GetAccentColor() override
    {
        @autoreleasepool
        {
            if (@available(macOS 10.14, *))
            {
                auto color = [NSColor controlAccentColor];
                return to_argb(color);
            }
            else
            {
                return 0;
            }
        }
    }
    
    virtual void RegisterColorsChange(IAvnActionCallback *callback) override
    {
        if (@available(macOS 10.14, *))
        {
            observer = [[CocoaThemeObserver alloc] initWithCallback: callback];
            [[NSApplication sharedApplication] addObserver:observer forKeyPath:@"effectiveAppearance" options:NSKeyValueObservingOptionNew context:nil];
        }
    }

    virtual HRESULT GetPreferredLanguage(IAvnString** ret) override
    {
        @autoreleasepool
        {
            if (ret == nullptr)
                return E_POINTER;

            auto language = [[NSLocale preferredLanguages] firstObject];
            *ret = language == nil ? nullptr : CreateAvnString(language);
            return S_OK;
        }
    }

    virtual void RegisterLanguageChange(IAvnActionCallback *callback) override
    {
        localeObserver = [[CocoaLocaleObserver alloc] initWithCallback: callback];
        [[NSNotificationCenter defaultCenter] addObserver:localeObserver
                                                 selector:@selector(localeDidChange:)
                                                     name:NSCurrentLocaleDidChangeNotification
                                                   object:nil];
    }
    
private:
    unsigned int to_argb(NSColor* color)
    {
        const CGFloat* components = CGColorGetComponents(color.CGColor);
        unsigned int alpha = static_cast<unsigned int>(CGColorGetAlpha(color.CGColor) * 0xFF);
        unsigned int red = static_cast<unsigned int>(components[0] * 0xFF);
        unsigned int green = static_cast<unsigned int>(components[1] * 0xFF);
        unsigned int blue = static_cast<unsigned int>(components[2] * 0xFF);
        return (alpha << 24) + (red << 16) + (green << 8) + blue;
    }
};

@implementation CocoaThemeObserver
{
    ComPtr<IAvnActionCallback> _callback;
}
- (id) initWithCallback:(IAvnActionCallback *)callback{
    self = [super init];
    if (self) {
        _callback = callback;
    }
    return self;
}

/*- (void)didChangeValueForKey:(NSString *)key {
    if([key isEqualToString:@"effectiveAppearance"]) {
        _callback->Run();
    }
    else {
        [super didChangeValueForKey:key];
    }
}*/

- (void)observeValueForKeyPath:(NSString *)keyPath
                      ofObject:(id)object
                        change:(NSDictionary *)change
                       context:(void *)context {
    if([keyPath isEqualToString:@"effectiveAppearance"]) {
        _callback->Run();
    } else {
        [super observeValueForKeyPath:keyPath
                             ofObject:object
                               change:change
                              context:context];
    }
}
@end

@implementation CocoaLocaleObserver
{
    ComPtr<IAvnActionCallback> _callback;
}
- (id) initWithCallback:(IAvnActionCallback *)callback {
    self = [super init];
    if (self) {
        _callback = callback;
    }
    return self;
}

- (void)localeDidChange:(NSNotification *)notification {
    _callback->Run();
}
@end

extern IAvnPlatformSettings* CreatePlatformSettings()
{
    return new PlatformSettings();
}
