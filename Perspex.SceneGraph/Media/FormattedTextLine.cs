// -----------------------------------------------------------------------
// <copyright file="FormattedTextLine.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    public class FormattedTextLine
    {
        public FormattedTextLine(int length, double height)
        {
            this.Length = length;
            this.Height = height;
        }

        public int Length { get; private set; }

        public double Height { get; private set; }
    }
} 
