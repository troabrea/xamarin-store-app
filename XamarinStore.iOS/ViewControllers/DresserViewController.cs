using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.Social;
using MonoTouch.CoreImage;
using System.Drawing;

using System.Threading.Tasks;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreAnimation;

using BigTed;

namespace XamarinStore
{
	public class DresserViewController : UIViewController
	{
		private Product CurrentProduct;
		private UIImage[] Images;
		public DresserViewController ( Product product, UIImage[] images ) : base()
		{
			CurrentProduct = product;
			Images = images;
		}
		private UIImage currentImage;
		private UIImageView imageView;
		private UIImage userPhoto;
		private UIScrollView scrollView;
		private int imageIndex;

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			EdgesForExtendedLayout = UIRectEdge.None;

			View.BackgroundColor = UIColor.LightGray;
			Title = "Dressing Room";
			
			LoadUserPhoto ();

			ToolbarItems = new UIBarButtonItem[] {
				new UIBarButtonItem (UIBarButtonSystemItem.FastForward, delegate(object sender, EventArgs e) {
					if (imageIndex < Images.Count () - 1) {
						imageIndex++;
					} else {
						imageIndex = 0;
					}
					currentImage = Images [imageIndex];
					DetectFaces ();
				}),
				new UIBarButtonItem( UIBarButtonSystemItem.FlexibleSpace ),
				new UIBarButtonItem( UIBarButtonSystemItem.Camera, delegate(object sender, EventArgs e) {
					PickUserPhoto();
				}),
				new UIBarButtonItem( UIBarButtonSystemItem.FlexibleSpace ),
				new UIBarButtonItem( UIBarButtonSystemItem.Compose, delegate(object sender, EventArgs e) {
					TwitXelphie();
				})
			};

			scrollView = new UIScrollView (View.Bounds);
			scrollView.DelaysContentTouches = true;

			imageIndex = 0;
			currentImage = Images [imageIndex];

			scrollView.ContentSize = currentImage.Size;

			imageView = new UIImageView ( new RectangleF (0, 0, currentImage.Size.Width, currentImage.Size.Height));

			scrollView.MinimumZoomScale = 0.5f;
			scrollView.MaximumZoomScale = 2.0f;
			scrollView.ViewForZoomingInScrollView = delegate(UIScrollView _scrollView) {
				return imageView;
			};
			scrollView.Add (imageView);

			View.Add (scrollView);

		}

		void LoadUserPhoto ()
		{
			var photoData = NSUserDefaults.StandardUserDefaults.DataForKey ("USER_PHOTO_DATA");
			if (photoData != null) {
				userPhoto = UIImage.LoadFromData (photoData);
			}
		}

		async void SaveUserPhoto (UIImage photo, Boolean detectFace = false)
		{
			userPhoto = photo;
			if (detectFace) {
				var features = await GetFeatures (new CIImage (photo));
				if (features.Count () > 0) {
					var faceRect = AdjustFaceRect (features [0].Bounds);
					userPhoto = UIImageExtensions.CropImage (photo, faceRect);
				}
			}
			var photoData = userPhoto.AsJPEG ();
			NSUserDefaults.StandardUserDefaults.SetValueForKey (photoData, (NSString)"USER_PHOTO_DATA");
			//
			DetectFaces ();
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			NavigationController.ToolbarHidden = false;
		}

		public override void ViewWillDisappear (bool animated)
		{
			base.ViewWillDisappear (animated);
			NavigationController.ToolbarHidden = true;
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);
			if (userPhoto != null) {
				DetectFaces ();
			} else {
				PickUserPhoto ();
			}
			scrollView.SetZoomScale (0.5f, false);
		}
		private PointF FlipForBottomOrigin (PointF point, int height)
		{
			return new PointF(point.X, height - point.Y);
		}
		private async void DetectFaces()
		{
			try {
				BTProgressHUD.Show();
				CIImage image =  new CIImage(currentImage);
				var features = await GetFeatures( image );
				DrawImageAnnotatedWithFeatures (features);
				image.Dispose();
			} finally {
				BTProgressHUD.Dismiss ();
			}
		}
		private Task<CIFeature[]> GetFeatures( CIImage image )
		{
			return Task.Run (() => {
				var detector = CIDetector.CreateFaceDetector (null, FaceDetectorAccuracy.High);
				return detector.FeaturesInImage (image);
			});
		}
		private RectangleF AdjustFaceRect( RectangleF originalRect )
		{
			var rect = originalRect;
			var OFFSET = 20.0f;
			rect.X = rect.X - OFFSET;
			rect.Y = rect.Y - OFFSET;
			rect.Width = rect.Width + (OFFSET * 2);
			rect.Height = rect.Height + (OFFSET * 2);
			return rect;
		}
		void DrawImageAnnotatedWithFeatures (CIFeature[] features)
		{
			UIImage faceImage = currentImage;
			UIGraphics.BeginImageContextWithOptions (faceImage.Size, true, 0);

			faceImage.Draw (imageView.Bounds);

			using (var context = UIGraphics.GetCurrentContext ()) {
				// Flip Context
				context.TranslateCTM (0, imageView.Bounds.Size.Height);
				context.ScaleCTM (1.0f, -1.0f);
				foreach (CIFaceFeature feature in features) {
					var faceRect = AdjustFaceRect (feature.Bounds);
					var gearImage = userPhoto == null ? UIImage.FromBundle ("user-default-avatar").Scale (faceRect.Size) : userPhoto.Scale (faceRect.Size);
					gearImage = UIImage.FromImage (gearImage.CGImage, gearImage.CurrentScale, UIImageOrientation.DownMirrored);
					gearImage.Draw (faceRect);
				}
				imageView.Image = UIGraphics.GetImageFromCurrentImageContext ();
				UIGraphics.EndImageContext ();
			}
		}

		void PickUserPhoto ()
		{
			var sources = new string[] { "Photo Library"};
			if (UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera)) {
				sources = new string[] { "Photo Library", "Camera" };
			}
			var sheet = new UIActionSheet ( "Pick or take a photo of your face...", null,"Cancel",null,sources);
			sheet.Clicked += (object sender, UIButtonEventArgs e) => {
				if (e.ButtonIndex == sheet.CancelButtonIndex) {
					return;
				}
				if (e.ButtonIndex == 0) {
					Camera.SelectPicture(this,(obj) => {
						var photo = (UIImage)obj.ValueForKey(new NSString("UIImagePickerControllerEditedImage") );
						SaveUserPhoto(photo);
						photo.Dispose();
					});
				}
				if ( e.ButtonIndex == 1 ) {
					Camera.TakePicture(this,(obj) => {
						var photo = (UIImage)obj.ValueForKey(new NSString("UIImagePickerControllerEditedImage") );
						SaveUserPhoto(photo);
						photo.Dispose();
					});
				}
			};
			sheet.ShowFromToolbar (NavigationController.Toolbar);
		}

		void TwitXelphie ()
		{
			var twit = SLComposeViewController.FromService (SLServiceKind.Twitter);
			twit.SetInitialText (String.Format( "My {0} #Xelphie, Go C#!",CurrentProduct.Name));
			twit.AddImage (imageView.Image);
			PresentViewController (twit, true, null);
		}
	}
}

