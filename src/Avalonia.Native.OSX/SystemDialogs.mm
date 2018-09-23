#include "common.h"
#include "window.h"

class SystemDialogs : public ComSingleObject<IAvnSystemDialogs, &IID_IAvnSystemDialogs>
{
    virtual void SelectFolderDialog (IAvnWindow* parentWindowHandle,
                                     IAvnSystemDialogEvents* events,
                                     const char* title,
                                     const char* initialPath)
    {
        
    }
    
    virtual void OpenFileDialog (IAvnWindow* parentWindowHandle,
                                 IAvnSystemDialogEvents* events,
                                 bool allowMultiple,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* intialFile,
                                 const char* filters)
    {
        auto panel = [NSOpenPanel openPanel];
        
        panel.allowsMultipleSelection = allowMultiple;
        
        if(parentWindowHandle != nullptr)
        {
            auto windowBase = dynamic_cast<WindowBaseImpl*>(parentWindowHandle);
            
            [panel beginSheet:windowBase->Window completionHandler:^(NSModalResponse result) {
                if(result == NSFileHandlingPanelOKButton)
                {
                    
                }
                
                events->OnCompleted(0, nullptr);
            }];
        }
        else
        {
            [panel beginWithCompletionHandler:^(NSModalResponse result) {
                if(result == NSFileHandlingPanelOKButton)
                {
                    
                }
                
                events->OnCompleted(0, nullptr);
            }];
        }
    }
    
    virtual void SaveFileDialog (IAvnWindow* parentWindowHandle,
                                 IAvnSystemDialogEvents* events,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* intialFile,
                                 const char* filters)
    {
        
    }

};

extern IAvnSystemDialogs* CreateSystemDialogs()
{
    return new SystemDialogs();
}
