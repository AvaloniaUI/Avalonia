// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using ReactiveUI;
using System;

namespace BindingTest.ViewModels
{
    public class ExceptionPropertyErrorViewModel : ReactiveObject
    {
        private int _lessThan10;

        public int LessThan10
        {
            get { return _lessThan10; }
            set
            {
                if (value < 10)
                {
                    this.RaiseAndSetIfChanged(ref _lessThan10, value);
                }
                else
                {
                    throw new InvalidOperationException("Value must be less than 10.");
                }
            }
        }
    }
}
