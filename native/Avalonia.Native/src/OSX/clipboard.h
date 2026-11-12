#pragma once

#include "common.h"

@interface WriteableClipboardItem : NSObject <NSPasteboardWriting>
- (nonnull instancetype) initWithItem:(const ComPtr<IAvnClipboardDataItem>&)item source:(const ComPtr<IAvnClipboardDataSource>&)source;
@end
