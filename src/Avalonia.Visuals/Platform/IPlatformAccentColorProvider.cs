﻿using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace Avalonia.Platform
{
    public interface IPlatformAccentColorProvider
    {
        public Color GetSystemAccentColor(Color fallBackColor);
    }
}
