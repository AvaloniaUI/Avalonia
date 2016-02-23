// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Perspex.Markup.Data.Parsers
{
    internal static class LiteralParser
    {
        public static object Parse(Reader r)
        {
            if (char.IsDigit(r.Peek))
            {
                var result = new StringBuilder();
                var foundDecimal = false;
                while (!r.End)
                {
                    if (char.IsDigit(r.Peek))
                    {
                        result.Append(r.Take());
                    }
                    else if (!foundDecimal && r.Peek == '.')
                    {
                        result.Append(r.Take());
                        foundDecimal = true;
                    }
                    else
                    {
                        break;
                    }
                }

                if (!foundDecimal)
                {
                    return int.Parse(result.ToString(), CultureInfo.InvariantCulture); 
                }
                else
                {
                    return result.ToString(); // Leave as a string to support double, float, and decimal indicies
                }
            }

            return null;
        }
    }
}
