#pragma once

#include "common.h"

@interface WriteableClipboardItem : NSObject <NSPasteboardWriting>
- (nonnull instancetype) initWithItem:(nonnull IAvnClipboardDataItem*)item source:(nonnull IAvnClipboardDataSource*)source;
@end
