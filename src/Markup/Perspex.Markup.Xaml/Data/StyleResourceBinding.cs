// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Perspex.Controls;
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
            return new Subject(target, Name, treeAnchor);
        }

        private class Subject : ISubject<object>
        {
            private IPerspexObject _target;
            private string _name;
            private IPerspexObject _treeAnchor;

            public Subject(IPerspexObject target, string name, IPerspexObject treeAnchor)
            {
                _target = target;
                _name = name;
                _treeAnchor = treeAnchor;
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
                var host = (_target as IControl) ?? (_treeAnchor as IControl);

                if (host != null)
                {
                    var resource = host.FindStyleResource(_name);

                    if (resource != PerspexProperty.UnsetValue)
                    {
                        observer.OnNext(resource);
                    }

                    observer.OnCompleted();
                }
                else
                {
                    // TODO: Log error.
                }

                return Disposable.Empty;
            }
        }
    }
}
