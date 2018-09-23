#include "common.h"

class SystemDialogs : public ComSingleObject<IAvnSystemDialogs, &IID_IAvnSystemDialogs>
{
    virtual void SelectFolderDialog (IAvnSystemDialogEvents* events,
                                     const char* title,
                                     const char* initialPath)
    {
        
    }
    
    virtual void OpenFileDialog (IAvnSystemDialogEvents* events,
                                 bool allowMultiple,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* intialFile,
                                 const char* filters)
    {
        events->OnCompleted(0, nullptr);
    }
    
    virtual void SaveFileDialog (IAvnSystemDialogEvents* events,
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
