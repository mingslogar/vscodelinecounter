using System;

namespace VSCodeLineCounter
{
	class IOHelpers
	{
		public static string FormatFileSize(long bytes)
		{
			if (bytes < 1024)
				return bytes.ToString() + " byte" + (bytes != 1 ? "s" : "");
			else if (bytes < 1048576)
				return (Math.Round((decimal)bytes / 1024, 2)).ToString() + " KB";
			else if (bytes < 1073741824)
				return (Math.Round((decimal)bytes / 1048576, 2)).ToString() + " MB";
			else if (bytes < 1099511627776)
				return (Math.Round((decimal)bytes / 1073741824, 2)).ToString() + " GB";
			else
				return (Math.Round((decimal)bytes / 1099511627776, 2)).ToString() + " TB";
		}

		public static long FormatFileSize(string data)
		{
			string[] split = data.Split(' ');
			double size = double.Parse(split[0]);
			string modifier = split[1];

			switch (modifier)
			{
				case "byte":
				case "bytes":
				default:
					return (long)size;

				case "KB":
					return (long)(size * 1024);

				case "MB":
					return (long)(size * 1048576);

				case "GB":
					return (long)(size * 1073741824);

				case "TB":
					return (long)(size * 1099511627776);
			}
		}
	}
}
