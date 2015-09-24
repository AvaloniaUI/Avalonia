// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Input.Raw;
using Perspex.Layout;

namespace Perspex.Input
{
    public interface IInputManager
    {
        IObservable<RawInputEventArgs> RawEventReceived { get; }

        IObservable<RawInputEventArgs> PostProcess { get; }

        void Process(RawInputEventArgs e);
    }
}
