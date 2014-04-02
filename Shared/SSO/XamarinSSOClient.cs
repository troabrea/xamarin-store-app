using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Xamarin.SSO.Client {
	public class XamarinSSOClient {
		const int TimeoutSeconds = 30;
		const string user_agent = "XamarinSSO .NET v1.0";
		static Encoding encoding = Encoding.UTF8;
		string auth_api_url;
		string accounts_api_url;
		string apikey;

		public XamarinSSOClient (string apikey) : this ("https://auth.xamarin.com", apikey)
		{
		}

		public XamarinSSOClient (string base_url, string apikey)
		{
			auth_api_url = base_url + "/api/v1/auth";
			accounts_api_url = base_url + "/api/v1/accounts";
			this.apikey = apikey;
		}

		protected virtual HttpClient SetupRequest (string url)
		{
			var credentials = new NetworkCredential(apikey, "");
			var handler = new HttpClientHandler { Credentials = credentials };
			handler.PreAuthenticate = true;


			var client = new HttpClient (handler);
			client.BaseAddress = new Uri( url);
			client.DefaultRequestHeaders.Add("User-Agent",user_agent);
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue ("Basic", Convert.ToBase64String (Encoding.UTF8.GetBytes (apikey + ":")));
			client.Timeout = new TimeSpan(0,10,0);
			return client;
		}

		static string GetResponseAsString (WebResponse response)
		{
			using (StreamReader sr = new StreamReader (response.GetResponseStream (), encoding)) {
				return sr.ReadToEnd ();
			}
		}

		protected virtual async Task<string> DoRequest (string endpoint, string method = "GET", string body = null)
		{
			string result = null;
			var req = SetupRequest (endpoint);

			HttpContent content = body == null ? null : new StringContent (body, Encoding.UTF8,"application/x-www-form-urlencoded");

			try {
				if(method == "GET"){
					result = await req.GetStringAsync(endpoint);
				}
				else if(method == "POST")
					result = await (await req.PostAsync(endpoint,content)).Content.ReadAsStringAsync();
				else if(method == "PUT")
					result = await (await req.PutAsync(endpoint,content)).Content.ReadAsStringAsync();
			} catch (WebException) {
				throw;
			}
			return result;
		}

		public async Task<AccountResponse> CreateToken (string email, string password)
		{
			if (String.IsNullOrWhiteSpace (email))
				throw new ArgumentNullException ("email");
			if (String.IsNullOrWhiteSpace (password))
				throw new ArgumentNullException ("password");

			var str = String.Format ("email={0}&password={1}", UrlEncode (email), UrlEncode (password));
			string json = await DoRequest (auth_api_url, "POST", str);
			return JsonConvert.DeserializeObject<AccountResponse> (json);
		}

		static string UrlEncode (string src)
		{
			if (src == null)
				return null;
			return Uri.EscapeDataString (src);
		}
	}
}

