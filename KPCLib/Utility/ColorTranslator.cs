using System;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

#if KPCLib
namespace KeePassLib.Utility
{
	public static class ColorTranslator
    {
        public static Color FromHtml(String colorString)
        {
            Color color;

            if (colorString.StartsWith("#"))
            {
                colorString = colorString.Substring(1);
            }
            if (colorString.EndsWith(";"))
            {
                colorString = colorString.Substring(0, colorString.Length - 1);
            }

            int red, green, blue;
            switch (colorString.Length)
            {
                case 6:
                    red = int.Parse(colorString.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    green = int.Parse(colorString.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    blue = int.Parse(colorString.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    color = Color.FromArgb(red, green, blue);
                    break;
                case 3:
                    red = int.Parse(colorString.Substring(0, 1), System.Globalization.NumberStyles.HexNumber);
                    green = int.Parse(colorString.Substring(1, 1), System.Globalization.NumberStyles.HexNumber);
                    blue = int.Parse(colorString.Substring(2, 1), System.Globalization.NumberStyles.HexNumber);
                    color = Color.FromArgb(red, green, blue);
                    break;
                case 1:
                    red = green = blue = int.Parse(colorString.Substring(0, 1), System.Globalization.NumberStyles.HexNumber);
                    color = Color.FromArgb(red, green, blue);
                    break;
                default:
                    throw new ArgumentException("Invalid color: " + colorString);
            }
            return color;
        }

    }
}
#else
namespace KeePass2PCL.Utility
{
	/// <summary>
	/// Replacement for System.Drawing.ColorTranslator.
	/// </summary>
	/// <remarks>
	/// Colors are stored in the kdbx database file in HTML format (#XXXXXX).
	/// </remarks>
	public static class ColorTranslator
	{
		static Regex longForm = new Regex("^#([0-9A-Fa-f]{2})([0-9A-Fa-f]{2})([0-9A-Fa-f]{2})$");

		/// <summary>
		/// Converts an HTML color value to a Color.
		/// </summary>
		/// <returns>The Color.</returns>
		/// <param name="htmlColor">HTML color code.</param>
		/// <exception cref="ArgumentNullException">If htmlColor is null.</exception>
		/// <exception cref="ArgumentException">If htmlColor did not match the pattern "#XXXXXX".</exception>
		/// <remarks>
		/// Currently only understands "#XXXXXX". "#XXX" or named colors will
		/// throw and exception.
		/// </remarks>
		public static Color FromHtml(string htmlColor)
		{
			if (htmlColor == null)
				throw new ArgumentNullException("htmlColor");
			Match match = longForm.Match(htmlColor);
			if (match.Success) {
				var r = int.Parse(match.Groups[1].Value, NumberStyles.HexNumber);
				var g = int.Parse(match.Groups[2].Value, NumberStyles.HexNumber);
				var b = int.Parse(match.Groups[3].Value, NumberStyles.HexNumber);
				return Color.FromArgb(r, g, b);
			}
			throw new ArgumentException(string.Format("Could not parse HTML color '{0}'.", htmlColor), "htmlColor");
		}

		/// <summary>
		/// Converts a color to an HTML color code.
		/// </summary>
		/// <returns>String containing the color code.</returns>
		/// <param name="htmlColor">The Color to convert</param>
		/// <remarks>
		/// The string is in the format "#XXXXXX"
		/// </remarks>
		public static string ToHtml(Color htmlColor)
		{
			return string.Format("#{0:x2}{1:x2}{2:x2}", htmlColor.R, htmlColor.G, htmlColor.B);
		}
	}
}
#endif // KPCLib
