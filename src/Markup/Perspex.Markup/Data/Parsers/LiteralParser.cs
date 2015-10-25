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
                StringBuilder result = new StringBuilder();

                while (!r.End)
                {
                    if (char.IsDigit(r.Peek))
                    {
                        result.Append(r.Take());
                    }
                    else
                    {
                        break;
                    }
                }

                return int.Parse(result.ToString(), CultureInfo.InvariantCulture);
            }

            return null;
        }
    }
}
