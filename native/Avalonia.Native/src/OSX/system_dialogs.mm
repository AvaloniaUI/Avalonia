// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"
#include "window.h"

class SystemDialogs : public ComSingleObject<IAvnSystemDialogs, &IID_IAvnSystemDialogs>
{
public:
    FORWARD_IUNKNOWN()
    virtual void SelectFolderDialog (IAvnWindow* parentWindowHandle,
                                     IAvnSystemDialogEvents* events,
                                     const char* title,
                                     const char* initialDirectory) override
    {
        @autoreleasepool
        {
            auto panel = [NSOpenPanel openPanel];
            
            panel.canChooseDirectories = true;
            panel.canCreateDirectories = true;
            panel.canChooseFiles = false;
            
            if(title != nullptr)
            {
                panel.title = [NSString stringWithUTF8String:title];
            }
            
            if(initialDirectory != nullptr)
            {
                auto directoryString = [NSString stringWithUTF8String:initialDirectory];
                panel.directoryURL = [NSURL fileURLWithPath:directoryString];
            }
            
            auto handler = ^(NSModalResponse result) {
                if(result == NSFileHandlingPanelOKButton)
                {
                    auto urls = [panel URLs];
                    
                    if(urls.count > 0)
                    {
                        void* strings[urls.count];
                        
                        for(int i = 0; i < urls.count; i++)
                        {
                            auto url = [urls objectAtIndex:i];
                            
                            auto string = [url path];
                            
                            strings[i] = (void*)[string UTF8String];
                        }
                        
                        events->OnCompleted((int)urls.count, &strings[0]);
                        
                        [panel orderOut:panel];
                        
                        if(parentWindowHandle != nullptr)
                        {
                            auto windowHolder = dynamic_cast<INSWindowHolder*>(parentWindowHandle);
                            [windowHolder->GetNSWindow() makeKeyAndOrderFront:windowHolder->GetNSWindow()];
                        }
                        
                        return;
                    }
                }
                
                events->OnCompleted(0, nullptr);
                
            };
            
            if(parentWindowHandle != nullptr)
            {
                auto windowBase = dynamic_cast<INSWindowHolder*>(parentWindowHandle);
                
                [panel beginSheetModalForWindow:windowBase->GetNSWindow() completionHandler:handler];
            }
            else
            {
                [panel beginWithCompletionHandler: handler];
            }
        }
    }
    
    virtual void OpenFileDialog (IAvnWindow* parentWindowHandle,
                                 IAvnSystemDialogEvents* events,
                                 bool allowMultiple,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* initialFile,
                                 const char* filters) override
    {
        @autoreleasepool
        {
            auto panel = [NSOpenPanel openPanel];
            
            panel.allowsMultipleSelection = allowMultiple;
            
            if(title != nullptr)
            {
                panel.title = [NSString stringWithUTF8String:title];
            }
            
            if(initialDirectory != nullptr)
            {
                auto directoryString = [NSString stringWithUTF8String:initialDirectory];
                panel.directoryURL = [NSURL fileURLWithPath:directoryString];
            }
            
            if(initialFile != nullptr)
            {
                panel.nameFieldStringValue = [NSString stringWithUTF8String:initialFile];
            }
            
            if(filters != nullptr)
            {
                auto filtersString = [NSString stringWithUTF8String:filters];
                
                if(filtersString.length > 0)
                {
                    auto allowedTypes = [filtersString componentsSeparatedByString:@";"];
                    
                    panel.allowedFileTypes = allowedTypes;
                }
            }
            
            auto handler = ^(NSModalResponse result) {
                if(result == NSFileHandlingPanelOKButton)
                {
                    auto urls = [panel URLs];
                    
                    if(urls.count > 0)
                    {
                        void* strings[urls.count];
                        
                        for(int i = 0; i < urls.count; i++)
                        {
                            auto url = [urls objectAtIndex:i];
                            
                            auto string = [url path];
                            
                            strings[i] = (void*)[string UTF8String];
                        }
                        
                        events->OnCompleted((int)urls.count, &strings[0]);
                        
                        [panel orderOut:panel];
                        
                        if(parentWindowHandle != nullptr)
                        {
                            auto windowHolder = dynamic_cast<INSWindowHolder*>(parentWindowHandle);
                            [windowHolder->GetNSWindow() makeKeyAndOrderFront:windowHolder->GetNSWindow()];
                        }
                        
                        return;
                    }
                }
                
                events->OnCompleted(0, nullptr);
                
            };
            
            if(parentWindowHandle != nullptr)
            {
                auto windowHolder = dynamic_cast<INSWindowHolder*>(parentWindowHandle);
                
                [panel beginSheetModalForWindow:windowHolder->GetNSWindow() completionHandler:handler];
            }
            else
            {
                [panel beginWithCompletionHandler: handler];
            }
        }
    }
    
    virtual void SaveFileDialog (IAvnWindow* parentWindowHandle,
                                 IAvnSystemDialogEvents* events,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* initialFile,
                                 const char* filters) override
    {
        @autoreleasepool
        {
            auto panel = [NSSavePanel savePanel];
            
            if(title != nullptr)
            {
                panel.title = [NSString stringWithUTF8String:title];
            }
            
            if(initialDirectory != nullptr)
            {
                auto directoryString = [NSString stringWithUTF8String:initialDirectory];
                panel.directoryURL = [NSURL fileURLWithPath:directoryString];
            }
            
            if(initialFile != nullptr)
            {
                panel.nameFieldStringValue = [NSString stringWithUTF8String:initialFile];
            }
            
            if(filters != nullptr)
            {
                auto filtersString = [NSString stringWithUTF8String:filters];
                
                if(filtersString.length > 0)
                {
                    auto allowedTypes = [filtersString componentsSeparatedByString:@";"];
                    
                    panel.allowedFileTypes = allowedTypes;
                }
            }
            
            auto handler = ^(NSModalResponse result) {
                if(result == NSFileHandlingPanelOKButton)
                {
                    void* strings[1];
                    
                    auto url = [panel URL];
                    
                    auto string = [url path];   
                    strings[0] = (void*)[string UTF8String];
               
                    events->OnCompleted(1, &strings[0]);
                    
                    [panel orderOut:panel];
                    
                    if(parentWindowHandle != nullptr)
                    {
                        auto windowHolder = dynamic_cast<INSWindowHolder*>(parentWindowHandle);
                        [windowHolder->GetNSWindow() makeKeyAndOrderFront:windowHolder->GetNSWindow()];
                    }
                    
                    return;
                }
                
                events->OnCompleted(0, nullptr);
                
            };
            
            if(parentWindowHandle != nullptr)
            {
                auto windowBase = dynamic_cast<INSWindowHolder*>(parentWindowHandle);
                
                [panel beginSheetModalForWindow:windowBase->GetNSWindow() completionHandler:handler];
            }
            else
            {
                [panel beginWithCompletionHandler: handler];
            }
        }
    }

};

extern IAvnSystemDialogs* CreateSystemDialogs()
{
    return new SystemDialogs();
}
