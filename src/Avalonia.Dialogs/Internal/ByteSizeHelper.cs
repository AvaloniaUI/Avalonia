using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Dialogs.Internal
{
	public static class ByteSizeHelper
	{
		private static readonly string[] Prefixes =
		{
			"B",
			"KB",
			"MB",
			"GB",
			"TB"
		};

		public static string ToString(long bytes)
		{
			var index = 0;
			while (bytes >= 1000)
			{
				bytes /= 1000;
				++index;
			}
			return $"{bytes:N} {Prefixes[index]}";
		}
	}
}
