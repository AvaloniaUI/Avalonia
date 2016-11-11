// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace BindingTest.ViewModels
{
    public class DataAnnotationsErrorViewModel
    {
        [Phone]
        [MaxLength(10)]
        public string PhoneNumber { get; set; }

        [Range(0, 9)]
        public int LessThan10 { get; set; }
    }
}
