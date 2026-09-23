#include "common.h"
#include "AvnString.h"
#include <vector>

static NSString* ResolveSpellCheckLanguage(NSString* requested)
{
    if (requested == nil || requested.length == 0)
        return nil;

    NSArray<NSString*>* available = [[NSSpellChecker sharedSpellChecker] availableLanguages];
    NSString* normalized = [requested stringByReplacingOccurrencesOfString:@"_" withString:@"-"];

    for (NSString* candidate in available)
    {
        NSString* normalizedCandidate = [candidate stringByReplacingOccurrencesOfString:@"_" withString:@"-"];
        if ([normalizedCandidate caseInsensitiveCompare:normalized] == NSOrderedSame)
            return candidate;
    }

    NSString* neutral = [[normalized componentsSeparatedByString:@"-"] firstObject];
    if (neutral.length == 0)
        return nil;

    for (NSString* candidate in available)
    {
        NSString* normalizedCandidate = [candidate stringByReplacingOccurrencesOfString:@"_" withString:@"-"];
        if ([normalizedCandidate caseInsensitiveCompare:neutral] == NSOrderedSame ||
            [normalizedCandidate.lowercaseString hasPrefix:[neutral.lowercaseString stringByAppendingString:@"-"]])
        {
            return candidate;
        }
    }

    return nil;
}

class AvnSpellCheckRanges : public virtual ComSingleObject<IAvnSpellCheckRanges, &IID_IAvnSpellCheckRanges>
{
    std::vector<AvnSpellCheckRange> _ranges;

public:
    FORWARD_IUNKNOWN()

    void Add(NSRange range)
    {
        _ranges.push_back(AvnSpellCheckRange { (int)range.location, (int)range.length });
    }

    virtual unsigned int GetCount() override
    {
        return (unsigned int)_ranges.size();
    }

    virtual HRESULT Get(unsigned int index, AvnSpellCheckRange* ret) override
    {
        START_COM_CALL;

        if (ret == nullptr)
            return E_POINTER;
        if (index >= _ranges.size())
            return E_INVALIDARG;

        *ret = _ranges[index];
        return S_OK;
    }
};

class AvnSpellCheckContext : public virtual ComSingleObject<IAvnSpellCheckContext, &IID_IAvnSpellCheckContext>
{
    __strong NSSpellChecker* _checker;
    __strong NSString* _language;
    NSInteger _documentTag;

public:
    FORWARD_IUNKNOWN()

    AvnSpellCheckContext(NSString* language)
        : _checker([NSSpellChecker sharedSpellChecker]),
          _language([language copy]),
          _documentTag([NSSpellChecker uniqueSpellDocumentTag])
    {
    }

    virtual ~AvnSpellCheckContext()
    {
        [_checker closeSpellDocumentWithTag:_documentTag];
    }

    virtual HRESULT Check(IAvnString* value, IAvnSpellCheckRanges** ppv) override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (ppv == nullptr)
                return E_POINTER;

            auto ranges = new AvnSpellCheckRanges();
            NSString* text = GetNSStringWithoutRelease(value);

            if (text != nil && text.length > 0)
            {
                NSInteger offset = 0;

                while (offset < (NSInteger)text.length)
                {
                    NSInteger wordCount = 0;
                    NSRange range = [_checker
                        checkSpellingOfString:text
                        startingAt:offset
                        language:_language
                        wrap:NO
                        inSpellDocumentWithTag:_documentTag
                        wordCount:&wordCount];

                    if (range.location == NSNotFound || range.length == 0)
                        break;

                    ranges->Add(range);
                    offset = (NSInteger)NSMaxRange(range);
                }
            }

            *ppv = ranges;
            return S_OK;
        }
    }

    virtual HRESULT GetSuggestions(IAvnString* value, int start, int length, IAvnStringArray** ppv) override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (ppv == nullptr)
                return E_POINTER;

            NSString* text = GetNSStringWithoutRelease(value);

            if (text == nil || start < 0 || length <= 0 || (NSUInteger)(start + length) > text.length)
                return E_INVALIDARG;

            // Passing the whole text lets AppKit use the surrounding words.
            NSArray<NSString*>* guesses = [_checker
                guessesForWordRange:NSMakeRange((NSUInteger)start, (NSUInteger)length)
                inString:text
                language:_language
                inSpellDocumentWithTag:_documentTag];

            *ppv = CreateAvnStringArray(guesses ?: [NSArray<NSString*> array]);
            return S_OK;
        }
    }
};

class AvnSpellCheckProvider : public virtual ComSingleObject<IAvnSpellCheckProvider, &IID_IAvnSpellCheckProvider>
{
public:
    FORWARD_IUNKNOWN()

    virtual HRESULT GetSupportedCultures(IAvnStringArray** ppv) override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (ppv == nullptr)
                return E_POINTER;

            NSArray<NSString*>* available = [[NSSpellChecker sharedSpellChecker] availableLanguages];
            *ppv = CreateAvnStringArray(available);
            return S_OK;
        }
    }

    virtual HRESULT TryCreateContext(IAvnString* culture, IAvnSpellCheckContext** ppv) override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (ppv == nullptr)
                return E_POINTER;

            NSString* language = ResolveSpellCheckLanguage(GetNSStringWithoutRelease(culture));
            *ppv = language == nil ? nullptr : new AvnSpellCheckContext(language);
            return S_OK;
        }
    }
};

IAvnSpellCheckProvider* CreateSpellCheckProvider()
{
    return new AvnSpellCheckProvider();
}
