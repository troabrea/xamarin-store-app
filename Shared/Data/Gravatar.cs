using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace XamarinStore
{
	public class Gravatar
	{
		public enum Rating { G, PG, R, X }

		const string _url = "http://www.gravatar.com/avatar.php?gravatar_id=";

		public static string GetURL (string email, int size, Rating rating = Rating.PG)
		{
			var hash = MD5Hash (email.ToLower ());

			if (size < 1 | size > 600) {
				throw new ArgumentOutOfRangeException("size", "The image size should be between 20 and 80");
			}

			return _url + hash + "&s=" + size.ToString () + "&r=" + rating.ToString ().ToLower ();
		}

		static string MD5Hash (string input)
		{
			var md5 = MD5Core.GetHashString (input).ToLower();
			return md5;
		}
	}
}

