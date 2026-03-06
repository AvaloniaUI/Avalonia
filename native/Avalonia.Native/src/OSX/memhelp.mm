#include "common.h"
class MemHelper : public ComSingleObject<IAvnNativeObjectsMemoryManagement, &IID_IAvnNativeObjectsMemoryManagement>
{
    FORWARD_IUNKNOWN()
    void RetainNSObject(void *object) override
    {
        ::RetainNSObject(object);
    }
    
    void ReleaseNSObject(void *object) override
    {
        ::ReleaseNSObject(object);
    }
    
    void RetainCFObject(void *object) override
    {
        CFRetain(object);
    }
    
    void ReleaseCFObject(void *object) override
    {
        CFRelease(object);
    }
    
    uint64_t GetRetainCountForNSObject(void *obj) override {
        return ::GetRetainCountForNSObject(obj);
    }
    
    int64_t GetRetainCountForCFObject(void *obj) override { 
        return CFGetRetainCount(obj);
    }
    
    
};


extern IAvnNativeObjectsMemoryManagement* CreateMemoryManagementHelper()
{
    return new MemHelper();
}
