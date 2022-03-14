using System;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Media
{
    internal class TextNoneTrimming : TextTrimming
    {
        public override TextCollapsingProperties CreateCollapsingProperties(TextCollapsingCreateInfo createInfo)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return nameof(None);
        }
    }
}
