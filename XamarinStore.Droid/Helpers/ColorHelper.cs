using System;

namespace XamarinStore
{
	public static class ColorHelper
	{
		public static Android.Graphics.Color ToAndroidColor (this Color color)
		{
			return Android.Graphics.Color.Rgb ((int)(255 * color.R), (int)(255 * color.G), (int)(255 * color.B));
		}

	}
}

