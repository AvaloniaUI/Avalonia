// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Data;
using System.Reactive.Linq;

namespace Avalonia.Markup.Data
{
    internal class StreamNode : ExpressionNode
    {
        public override string Description => "^";

        protected override IObservable<object> StartListeningCore(WeakReference reference)
        {
            foreach (var plugin in ExpressionObserver.StreamHandlers)
            {
                if (plugin.Match(reference))
                {
                    return plugin.Start(reference);
                }
            }

            // TODO: Improve error.
            return Observable.Return(new BindingNotification(
                new MarkupBindingChainException("Stream operator applied to unsupported type", Description),
                BindingErrorType.Error));
        }
    }
}
