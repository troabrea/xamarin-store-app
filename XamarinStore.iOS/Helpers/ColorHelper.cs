using System;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;

namespace XamarinStore
{
	public static class ColorHelper
	{

		public static UIColor ToUIColor (this Color color)
		{
			return UIColor.FromRGB ((float)color.R, (float)color.G, (float)color.B);
		}

	}
}

