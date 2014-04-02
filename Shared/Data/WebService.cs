using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.SSO.Client;
using System.Net.Http;
using System.Diagnostics;

namespace XamarinStore
{
	public class WebService
	{
		public static readonly WebService Shared = new WebService ();

		public User CurrentUser { get; set; }

		public Order CurrentOrder { get; set; }

		XamarinSSOClient client = new XamarinSSOClient ("https://auth.xamarin.com", "0c833t3w37jq58dj249dt675a465k6b0rz090zl3jpoa9jw8vz7y6awpj5ox0qmb");

		public WebService ()
		{
			CurrentOrder = new Order ();
		}

		public async Task<bool> Login (string username, string password)
		{
			AccountResponse response;
			try {
				var request = client.CreateToken (username, password);
				response = await request;
				if (response.Success) {
					var user = response.User;
					CurrentUser = new  User {
						LastName = user.LastName,
						FirstName = user.FirstName,
						Email = username,
						Token = response.Token
					};
					return true;
				} else {
					Debug.WriteLine ("Login failed: {0}", response.Error);
				}
			} catch (Exception ex) {
				Debug.WriteLine ("Login failed for some reason...: {0}", ex.Message);
			}
			return false;
		}

		List<Product> products;

		public async Task<List<Product>> GetProducts ()
		{
			if (products == null) {
				try {
					string extraParams = "";

					//TODO: Get a Monkey!!!
					//extraParams = "?includeMonkeys=true";

					var request = CreateClient ();

					var response = await request.GetStringAsync ("products" + extraParams);


					products = await Task.Run (() => Newtonsoft.Json.JsonConvert.DeserializeObject<List<Product>> (response));
				} catch (Exception ex) {
					Debug.WriteLine (ex);
					products = new List<Product> ();
				}
			}
			return products;
		}

		bool hasPreloadedImages;

		public async Task PreloadImages (float imageWidth)
		{
			if (hasPreloadedImages)
				return;
			hasPreloadedImages = true;
			//Lets precach the countries too
			#pragma warning disable 4014
			GetCountries ();
			#pragma warning restore 4014
			await Task.Factory.StartNew (async () => {
				var imagUrls = products.SelectMany (x => x.ImageUrls.Select (y => Product.ImageForSize (y, imageWidth))).ToList ();
				foreach(var image in imagUrls)
				{
					await FileCache.Download (image);
				}

			});


		}

		List<Country> countries = new List<Country> ();

		public async Task<List<Country>> GetCountries ()
		{
			try {

				if (countries.Count > 0)
					return countries;
				var httpClient = CreateClient();
				string response =  await httpClient.GetStringAsync("Countries");
				countries =  await Task.Run(() => Newtonsoft.Json.JsonConvert.DeserializeObject<List<Country>> (response));
				return countries;
			} catch (Exception ex) {
				Debug.WriteLine (ex);
				return new List<Country> ();
			}
		}

		public async Task<string> GetCountryCode (string country)
		{
			var c = (await GetCountries ()).FirstOrDefault (x => x.Name == country) ?? new Country ();

			return c.Code;
		}
		//No need to await anything, and no need to spawn a task to return a list.
		#pragma warning disable 1998
		public  async Task<List<string>> GetStates (string country)
		{
			if (country.ToLower () == "united states")
				return new List<string> {
					"Alabama",
					"Alaska", 
					"Arizona",
					"Arkansas",
					"California",
					"Colorado",
					"Connecticut",
					"Delaware",
					"District of Columbia",
					"Florida",
					"Georgia",
					"Hawaii",
					"Idaho",
					"Illinois",
					"Indiana",
					"Iowa",
					"Kansas",
					"Kentucky",
					"Louisiana",
					"Maine",
					"Maryland",
					"Massachusetts",
					"Michigan",
					"Minnesota",
					"Mississippi",
					"Missouri",
					"Montana",
					"Nebraska",
					"Nevada",
					"New Hampshire",
					"New Jersey",
					"New Mexico",
					"New York",
					"North Carolina",
					"North Dakota",
					"Ohio",
					"Oklahoma",
					"Oregon",
					"Pennsylvania",
					"Rhode Island",
					"South Carolina",
					"South Dakota",
					"Tennessee",
					"Texas",
					"Utah",
					"Vermont",
					"Virginia",
					"Washington",
					"West Virginia",
					"Wisconsin",
					"Wyoming",
				};
			return new List<string> ();
		}
		#pragma warning restore 1998

		protected virtual HttpClient CreateClient ()
		{
			var c = new HttpClient ();
			c.BaseAddress = new Uri ("https://xamarin-store-app.xamarin.com/api/");
			c.Timeout = new TimeSpan (0, 10, 0);
			return c;
		}

		public async Task<OrderResult> PlaceOrder (User user, bool verify = false)
		{

			try {
				var content = CurrentOrder.GetJson (user);
				var request = CreateClient ();

				var responseMessage = await request.PostAsync ("order" + (verify ? "?verify=1" : ""), new StringContent (content, Encoding.UTF8, "application/json"));
				var response = await responseMessage.Content.ReadAsStringAsync();
					
				var result = await Task.Run (() => Newtonsoft.Json.JsonConvert.DeserializeObject<OrderResult> (response));
				if (!verify && result.Success)
					CurrentOrder = new Order ();
				return result;
			} catch (Exception ex) {
				Debug.WriteLine (ex);
				return new OrderResult {
					Success = false,
					Message = ex.Message,
				};
			}
		}
	}
}

