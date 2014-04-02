using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using CryptSharp;

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

		public static async Task<byte[]> GetImageBytes (string email, int size, Rating rating = Rating.PG)
		{
			var url = GetURL (email, size, rating);
			var client = new HttpClient ();
			return await client.GetByteArrayAsync (url);
		}

		static string MD5Hash (string input)
		{
			var hasher = new MD5Crypter();
			var builder = new StringBuilder ();
			byte[] data = Encoding.UTF8.GetBytes (hasher.Crypt (input));

			foreach (byte datum in data)
				builder.Append (datum.ToString ("x2"));

			return builder.ToString ();
		}
	}
}

