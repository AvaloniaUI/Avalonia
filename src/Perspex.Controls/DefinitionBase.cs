// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Controls
{
   /// <summary>
   /// Base class for <see cref="ColumnDefinition"/> and <see cref="RowDefinition"/>.
   /// </summary>
   public class DefinitionBase : PerspexObject
   {
      /// <summary>
      /// Defines the <see cref="SharedSizeGroup"/> property.
      /// </summary>
      public static readonly StyledProperty<string> SharedSizeGroupProperty =
         PerspexProperty.Register<DefinitionBase, string>(nameof(SharedSizeGroup), inherits: true);

      /// <summary>
      /// Gets or sets the name of the shared size group of the column or row.
      /// </summary>
      public string SharedSizeGroup
      {
         get { return GetValue(SharedSizeGroupProperty); }
         set { SetValue(SharedSizeGroupProperty, value); }
      }

      internal Double Offset { get; set; }

      internal Double MinMeasuredSize { get; set; }
   }
}