// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Perspex.Data;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Data
{
    public class StyleResourceBinding : IBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyleResourceBinding"/> class.
        /// </summary>
        /// <param name="name">The resource name.</param>
        public StyleResourceBinding(string name)
        {
            Name = name;
        }

        /// <inheritdoc/>
        public BindingMode Mode => BindingMode.OneTime;

        /// <summary>
        /// Gets the resource name.
        /// </summary>
        public string Name { get; }

        /// <inheritdoc/>
        public BindingPriority Priority => BindingPriority.LocalValue;

        /// <inheritdoc/>
        public ISubject<object> CreateSubject(
            IPerspexObject target,
            PerspexProperty targetProperty,
            IPerspexObject treeAnchor = null)
        {
            return new Subject(target, Name);
        }

        private class Subject : ISubject<object>
        {
            private IPerspexObject _target;
            private string _name;

            public Subject(IPerspexObject target, string name)
            {
                _target = target;
                _name = name;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(object value)
            {
            }

            public IDisposable Subscribe(IObserver<object> observer)
            {
                // HACK around OmniXAML issue #84.
                var po = (PerspexObject)_target;
                var parent = PerspexPropertyRegistry.Instance.FindRegistered(po, "Parent");

                return po.GetObservable(parent)
                    .Where(x => x != PerspexProperty.UnsetValue && x != null)
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        var resource = ((IStyleHost)_target).FindStyleResource(_name);

                        if (resource != PerspexProperty.UnsetValue)
                        {
                            observer.OnNext(resource);
                        }

                        observer.OnCompleted();
                    });
            }
        }
    }
}
