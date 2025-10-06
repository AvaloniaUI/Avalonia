#import <AppKit/AppKit.h>

class CppAutoreleasePool
{
    void* _pool;
public:
    CppAutoreleasePool();
    ~CppAutoreleasePool();
};

#define START_ARP_CALL CppAutoreleasePool __autoreleasePool
extern void ReleaseNSObject(void* obj);
extern void RetainNSObject(void* obj);
extern uint64_t GetRetainCountForNSObject(void* obj);
