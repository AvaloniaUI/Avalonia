// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ControlDetailsViewModel : ViewModelBase, IDisposable
    {
        private IVisual _control;

        public ControlDetailsViewModel(IVisual control)
        {
            if (control is AvaloniaObject avaloniaObject)
            {
                var props = AvaloniaPropertyRegistry.Instance.GetRegistered(avaloniaObject)
                    .Select(x => new PropertyDetails(avaloniaObject, x))
                    .OrderBy(x => x.IsAttached)
                    .ThenBy(x => x.Name)
                    .ToList();

                if (control is Control c)
                {
                    var classesProp = new PropertyDetails(c, nameof(c.Classes),
                        () => string.Join(" ", c.Classes),
                        v => c.Classes.Replace(Classes.Parse(v as string)),
                        Observable.FromEventPattern(c.Classes, nameof(c.Classes.CollectionChanged))
                    );

                    props.Insert(0, classesProp);

                    var l = c as ILayoutable;
                    DateTime? last = null;
                    var layoutProps = new[]
                    {
                        new PropertyDetails(c, "Layout Props",
                        () => $"measured: {l.IsMeasureValid} -> {l.PreviousMeasure} arranged: {l.IsArrangeValid} -> {l.PreviousArrange} ({last?.TimeOfDay})",
                        null,
                        Observable.FromEventPattern(c, nameof(c.LayoutUpdated)).Select(_=>(object)(last=DateTime.Now))
                        ),
                    };
                    props.InsertRange(0, layoutProps);
                }

                Properties = props;
            }

            _control = control;
        }

        public IEnumerable<PropertyDetails> Properties
        {
            get;
            private set;
        }

        public void Dispose()
        {
            foreach (var d in Properties)
            {
                d.Dispose();
            }
        }
    }
}
