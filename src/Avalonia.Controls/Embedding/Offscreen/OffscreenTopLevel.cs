﻿using System;
using System.ComponentModel;
using Avalonia.Styling;

namespace Avalonia.Controls.Embedding.Offscreen
{
    class OffscreenTopLevel : TopLevel, IStyleable
    {
        public OffscreenTopLevelImplBase Impl { get; }

        public OffscreenTopLevel(OffscreenTopLevelImplBase impl) : base(impl)
        {
            Impl = impl;
            Prepare();
        }

        public void Prepare()
        {
            EnsureInitialized();
            ApplyTemplate();
            LayoutManager.ExecuteInitialLayoutPass();
        }

        private void EnsureInitialized()
        {
            if (!this.IsInitialized)
            {
                var init = (ISupportInitialize)this;
                init.BeginInit();
                init.EndInit();
            }
        }

        Type IStyleable.StyleKey => typeof(EmbeddableControlRoot);
        public void Dispose()
        {
            PlatformImpl?.Dispose();
        }
    }
}
