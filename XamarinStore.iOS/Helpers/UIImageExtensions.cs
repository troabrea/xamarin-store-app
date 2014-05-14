using System;
using MonoTouch.UIKit;
using System.Threading.Tasks;
using MonoTouch.CoreGraphics;
using System.Drawing;

namespace XamarinStore
{
	public static class UIImageExtensions
	{
		public static async Task LoadUrl(this UIImageView imageView, string url)
		{	
			if (string.IsNullOrEmpty (url))
				return;
			var progress = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.WhiteLarge)
			{
				Center = new PointF(imageView.Bounds.GetMidX(), imageView.Bounds.GetMidY()),
			};
			imageView.AddSubview (progress);

		
			var t = FileCache.Download (url);
			if (t.IsCompleted) {
				imageView.Image = UIImage.FromFile(t.Result);
				progress.RemoveFromSuperview ();
				return;
			}
			progress.StartAnimating ();
			var image = UIImage.FromFile(await t);

			UIView.Animate (.3, 
				() => imageView.Image = image,
				() => {
					progress.StopAnimating ();
					progress.RemoveFromSuperview ();
				});
		}
		public static UIImage CropImage( UIImage image, RectangleF rect )
		{
			if (rect.X == 0 && rect.Y == 0 && rect.Size == image.Size) {
				return image;
			}
			UIGraphics.BeginImageContextWithOptions(rect.Size,false,1);
			image.Draw( new PointF(-rect.X,-rect.Y) );
			var croppedImage = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();
			return croppedImage;
		}
	}
}

