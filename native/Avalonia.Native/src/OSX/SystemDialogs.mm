#include "common.h"
#include "AvnString.h"
#include "INSWindowHolder.h"
#import <UniformTypeIdentifiers/UniformTypeIdentifiers.h>

const int kFileTypePopupTag = 10975;

// Target for NSPopupButton control in file dialog's accessory view.
// ExtensionDropdownHandler is copied from Chromium MIT code of select_file_dialog_bridge
@interface ExtensionDropdownHandler : NSObject {
 @private
  // The file dialog to which this target object corresponds. Weak reference
  // since the dialog_ will stay alive longer than this object.
  NSSavePanel* _dialog;

  // Two ivars serving the same purpose. While `_fileTypeLists` is for pre-macOS
  // 11, and contains NSStrings with UTType identifiers, `_fileUTTypeLists` is
  // for macOS 11 and later, and contains UTTypes.
  NSArray<NSArray<NSString*>*>* __strong _fileTypeLists;
  NSArray<NSArray<UTType*>*>* __strong _fileUTTypeLists
      API_AVAILABLE(macos(11.0));
}

- (instancetype)initWithDialog:(NSSavePanel*)dialog
                 fileTypeLists:(NSArray<NSArray<NSString*>*>*)fileTypeLists;

- (instancetype)initWithDialog:(NSSavePanel*)dialog
               fileUTTypeLists:(NSArray<NSArray<UTType*>*>*)fileUTTypeLists
    API_AVAILABLE(macos(11.0));

- (void)popupAction:(id)sender;
@end


@implementation ExtensionDropdownHandler

- (instancetype)initWithDialog:(NSSavePanel*)dialog
                 fileTypeLists:(NSArray<NSArray<NSString*>*>*)fileTypeLists {
  if ((self = [super init])) {
    _dialog = dialog;
    _fileTypeLists = fileTypeLists;
  }
  return self;
}

- (instancetype)initWithDialog:(NSSavePanel*)dialog
               fileUTTypeLists:(NSArray<NSArray<UTType*>*>*)fileUTTypeLists
    API_AVAILABLE(macos(11.0)) {
  if ((self = [super init])) {
    _dialog = dialog;
    _fileUTTypeLists = fileUTTypeLists;
  }
  return self;
}

- (void)popupAction:(id)sender {
  NSUInteger index = [sender indexOfSelectedItem];
  if (@available(macOS 11, *)) {
      _dialog.allowedContentTypes = [_fileUTTypeLists objectAtIndex:index];
  } else {
      _dialog.allowedFileTypes = [_fileTypeLists objectAtIndex:index];
  }
}

@end

class SystemDialogs : public ComSingleObject<IAvnSystemDialogs, &IID_IAvnSystemDialogs>
{
    ExtensionDropdownHandler* __strong _extension_dropdown_handler;
    
public:
    FORWARD_IUNKNOWN()
    virtual void SelectFolderDialog (IAvnWindow* parentWindowHandle,
                                     IAvnSystemDialogEvents* events,
                                     bool allowMultiple,
                                     const char* title,
                                     const char* initialDirectory) override
    {
        @autoreleasepool
        {
            auto panel = [NSOpenPanel openPanel];
            
            panel.allowsMultipleSelection = allowMultiple;
            panel.canChooseDirectories = true;
            panel.canCreateDirectories = true;
            panel.canChooseFiles = false;
            
            if(title != nullptr)
            {
                panel.message = [NSString stringWithUTF8String:title];
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
                                 IAvnFilePickerFileTypes* filters) override
    {
        @autoreleasepool
        {
            auto panel = [NSOpenPanel openPanel];
            
            panel.allowsMultipleSelection = allowMultiple;
            
            if(title != nullptr)
            {
                panel.message = [NSString stringWithUTF8String:title];
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
            
            SetAccessoryView(panel, filters, false);
            
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
                                 IAvnFilePickerFileTypes* filters) override
    {
        @autoreleasepool
        {
            auto panel = [NSSavePanel savePanel];
            
            if(title != nullptr)
            {
                panel.message = [NSString stringWithUTF8String:title];
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
            
            SetAccessoryView(panel, filters, true);
            
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
    
private:
    NSView* CreateAccessoryView() {
        // The label. Add attributes per-OS to match the labels that macOS uses.
        NSTextField* label = [NSTextField labelWithString:@"File format"];
        label.translatesAutoresizingMaskIntoConstraints = NO;
        label.textColor = NSColor.secondaryLabelColor;
        if (@available(macOS 11.0, *)) {
            label.font = [NSFont systemFontOfSize:[NSFont smallSystemFontSize]];
        }
        
        // The popup.
        NSPopUpButton* popup = [[NSPopUpButton alloc] initWithFrame:NSZeroRect
                                                          pullsDown:NO];
        popup.translatesAutoresizingMaskIntoConstraints = NO;
        popup.tag = kFileTypePopupTag;
        [popup setAutoenablesItems:NO];
        
        // A view to group the label and popup together. The top-level view used as
        // the accessory view will be stretched horizontally to match the width of
        // the dialog, and the label and popup need to be grouped together as one
        // view to do centering within it, so use a view to group the label and
        // popup.
        NSView* group = [[NSView alloc] initWithFrame:NSZeroRect];
        group.translatesAutoresizingMaskIntoConstraints = NO;
        [group addSubview:label];
        [group addSubview:popup];
        
        // This top-level view will be forced by the system to have the width of the
        // save dialog.
        NSView* view = [[NSView alloc] initWithFrame:NSZeroRect];
        view.translatesAutoresizingMaskIntoConstraints = NO;
        [view addSubview:group];
        
        NSMutableArray* constraints = [NSMutableArray array];
        
        // The required constraints for the group, instantiated top-to-bottom:
        // ┌───────────────────┐
        // │             ↕︎     │
        // │ ↔︎ label ↔︎ popup ↔︎ │
        // │             ↕︎     │
        // └───────────────────┘
        
        // Top.
        [constraints
         addObject:[popup.topAnchor constraintEqualToAnchor:group.topAnchor
                                                   constant:10]];
        
        // Leading.
        [constraints
         addObject:[label.leadingAnchor constraintEqualToAnchor:group.leadingAnchor
                                                       constant:10]];
        
        // Horizontal and vertical baseline between the label and popup.
        CGFloat labelPopupPadding;
        if (@available(macOS 11.0, *)) {
            labelPopupPadding = 8;
        } else {
            labelPopupPadding = 5;
        }
        [constraints addObject:[popup.leadingAnchor
                                constraintEqualToAnchor:label.trailingAnchor
                                constant:labelPopupPadding]];
        [constraints
         addObject:[popup.firstBaselineAnchor
                    constraintEqualToAnchor:label.firstBaselineAnchor]];
        
        // Trailing.
        [constraints addObject:[group.trailingAnchor
                                constraintEqualToAnchor:popup.trailingAnchor
                                constant:10]];
        
        // Bottom.
        [constraints
         addObject:[group.bottomAnchor constraintEqualToAnchor:popup.bottomAnchor
                                                      constant:10]];
        
        // Then the constraints centering the group in the accessory view. Vertical
        // spacing is fully specified, but as the horizontal size of the accessory
        // view will be forced to conform to the save dialog, only specify horizontal
        // centering.
        // ┌──────────────┐
        // │      ↕︎       │
        // │   ↔group↔︎    │
        // │      ↕︎       │
        // └──────────────┘
        
        // Top.
        [constraints
         addObject:[group.topAnchor constraintEqualToAnchor:view.topAnchor]];
        
        // Centering.
        [constraints addObject:[group.centerXAnchor
                                constraintEqualToAnchor:view.centerXAnchor]];
        
        // Bottom.
        [constraints
         addObject:[view.bottomAnchor constraintEqualToAnchor:group.bottomAnchor]];
        
        [NSLayoutConstraint activateConstraints:constraints];
        
        return view;
    }
    
    void SetAccessoryView(NSSavePanel* panel,
                          IAvnFilePickerFileTypes* filters,
                          bool is_save_panel)
    {
        NSView* accessory_view = CreateAccessoryView();
        
        NSPopUpButton* popup = [accessory_view viewWithTag:kFileTypePopupTag];
        
        NSMutableArray<NSArray<NSString*>*>* file_type_lists = [NSMutableArray array];
        NSMutableArray* file_uttype_lists = [NSMutableArray array];
        int default_extension_index = -1;
        
        for (int i = 0; i < filters->GetCount(); i++)
        {
            NSString* type_description = GetNSStringAndRelease(filters->GetName(i));
            [popup addItemWithTitle:type_description];

            // If any type is included, enable allowsOtherFileTypes, and skip this filter on save panel.
            if (filters->IsAnyType(i)) {
                panel.allowsOtherFileTypes = YES;
            }
            // If default extension is specified, auto select it later.
            if (filters->IsDefaultType(i)) {
                default_extension_index = i;
            }

            IAvnStringArray* array;

            // Prefer types priority of: file ext -> apple type id -> mime.
            // On macOS 10 we only support file extensions.
            if (@available(macOS 11, *)) {
                NSMutableArray* file_uttype_array = [NSMutableArray array];
                bool typeCompleted = false;

                if (filters->IsAnyType(i)) {
                    UTType* type = [UTType typeWithIdentifier:@"public.item"];
                    [file_uttype_array addObject:type];
                    typeCompleted = true;
                }
                if (!typeCompleted && filters->GetExtensions(i, &array) == 0) {
                    for (NSString* ext in GetNSArrayOfStringsAndRelease(array))
                    {
                        UTType* type = [UTType typeWithFilenameExtension:ext];
                        if (type && ![file_uttype_array containsObject:type]) {
                            [file_uttype_array addObject:type];
                            typeCompleted = true;
                        }
                    }
                }
                if (!typeCompleted && filters->GetAppleUniformTypeIdentifiers(i, &array) == 0) {
                    for (NSString* ext in GetNSArrayOfStringsAndRelease(array))
                    {
                        UTType* type = [UTType typeWithIdentifier:ext];
                        if (type && ![file_uttype_array containsObject:type]) {
                            [file_uttype_array addObject:type];
                            typeCompleted = true;
                        }
                    }
                }
                if (!typeCompleted && filters->GetMimeTypes(i, &array) == 0) {
                    for (NSString* ext in GetNSArrayOfStringsAndRelease(array))
                    {
                        UTType* type = [UTType typeWithMIMEType:ext];
                        if (type && ![file_uttype_array containsObject:type]) {
                            [file_uttype_array addObject:type];
                            typeCompleted = true;
                        }
                    }
                }
                
                [file_uttype_lists addObject:file_uttype_array];
            } else {
                NSMutableArray<NSString*>* file_type_array = [NSMutableArray array];
                if (filters->IsAnyType(i)) {
                    [file_type_array addObject:@"*.*"];
                }
                else if (filters->GetExtensions(i, &array) == 0) {
                    for (NSString* ext in GetNSArrayOfStringsAndRelease(array))
                    {
                        if (![file_type_array containsObject:ext]) {
                            [file_type_array addObject:ext];
                        }
                    }
                }
                [file_type_lists addObject:file_type_array];
            }
        }
        
        if ([file_uttype_lists count] == 0 && [file_type_lists count] == 0)
            return;

        if (@available(macOS 11, *))
            _extension_dropdown_handler = [[ExtensionDropdownHandler alloc] initWithDialog:panel
                                                                          fileUTTypeLists:file_uttype_lists];
        else
            _extension_dropdown_handler = [[ExtensionDropdownHandler alloc] initWithDialog:panel
                                                                            fileTypeLists:file_type_lists];
        
        [popup setTarget: _extension_dropdown_handler];
        [popup setAction: @selector(popupAction:)];
        
        if (default_extension_index != -1) {
            [popup selectItemAtIndex:default_extension_index];
        } else {
            // Select the first item.
            [popup selectItemAtIndex:0];
        }
        [_extension_dropdown_handler popupAction:popup];
        
        if (popup.numberOfItems > 0) {
            panel.accessoryView = accessory_view;
        }
    };
};

extern IAvnSystemDialogs* CreateSystemDialogs()
{
    return new SystemDialogs();
}
