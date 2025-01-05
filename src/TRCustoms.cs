using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using Newtonsoft.Json.Linq;

namespace TRLEManager
{
	internal class TRCustomsInfo
	{
		public const string TRCustomsURL = "https://trcustoms.org";
		public const string TRCustomsJSONInfoPageTemplate = TRCustomsURL + "/api/levels/{0}";
		public const string TRCustomsInfopageURLTemplate = TRCustomsURL + "/levels/{0}";
		//public const string TRCustomsDownloadURLTemplate = TRCustomsURL + "/api/level_files/{0}/download";
		public const string TRCustomsWalkthroughURLTemplate = TRCustomsURL + "/levels/{0}/walkthroughs";

		public uint ID { get; private set; }
		public string Title { get; private set; }
		public string Author { get; private set; }
		public string WebpageURL { get; private set; }
		public string DownloadURL { get; private set; }
		public string WalkthroughURL { get; private set; }
		// public uint TRLENetID { get; private set; }

        public static TRCustomsInfo CreateFromInfoPage(string infopage)
		{
			var result = new TRCustomsInfo();

			var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(infopage);

			string acquiredTRCustomsID = jsonObject["id"]?.ToString();

			result.WebpageURL = string.Format(TRCustomsInfopageURLTemplate, acquiredTRCustomsID);
			result.Title = jsonObject["name"]?.ToString();
			uint.TryParse(acquiredTRCustomsID, out uint trCustomsID);
			result.ID = trCustomsID;
			
			result.WalkthroughURL = string.Format(TRCustomsWalkthroughURLTemplate, trCustomsID);

   //         string acquiredTRLENetID = jsonObject["trle_id"]?.ToString();
   //         uint.TryParse(acquiredTRLENetID, out uint trleNetID);
			//result.TRLENetID = trleNetID;

            var authors = jsonObject["authors"] as JArray;

			StringBuilder concat_authors = new StringBuilder();
			foreach (object author in authors) {
				concat_authors.Append(((JObject)author)["username"].ToString());
				concat_authors.Append(", ");
			}

			concat_authors.Length -= 2;
			result.Author = concat_authors.ToString();

			JObject last_file = jsonObject["last_file"] as JObject;
			string dlUrl = last_file["url"].ToString();

			result.DownloadURL = dlUrl;

			return result;
		}

		private TRCustomsInfo() { }
	}

	internal class TRCustoms
	{
		public static string GetTRCustomsInfoPage(uint trCustomsID)
		{
			try
			{
				return string.Format(TRCustomsInfo.TRCustomsJSONInfoPageTemplate, trCustomsID);
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is FormatException)
			{
				var err = new FormatException("Faild to construct TRCustoms.org URL.", e);
				err.Data.Add("Infopage URL Template", TRCustomsInfo.TRCustomsInfopageURLTemplate);
				err.Data.Add("trCustomsID", trCustomsID);
				throw err;
			}
		}

		public static Task<TRCustomsInfo> GetTRCustomsInfo(string trCustomsID)
		{
			try
			{
				return GetTRCustomsInfo(uint.Parse(trCustomsID));
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is FormatException
				|| e is OverflowException)
			{
				var err = new Error("TRCustoms id is malformed.", e);
				err.Data.Add("trCustomsID", trCustomsID);
				throw err;
			}
		}

		public static Task<TRCustomsInfo> GetTRCustomsInfo(uint trCustomsID)
		{
			var tsc = new TaskCompletionSource<TRCustomsInfo>();

			var client = new WebClient();

			client.DownloadStringCompleted += (_, eventArgs) =>
			{
				string trCustomsInfoPage = eventArgs.Result;

				try
				{
					tsc.SetResult(TRCustomsInfo.CreateFromInfoPage(trCustomsInfoPage));
				}
				catch (Exception e)
				{
					tsc.SetException(e);
				}
			};

			try
			{
				client.DownloadStringAsync(new Uri(GetTRCustomsInfoPage(trCustomsID)));
			}
			catch (FormatException e)
			{
				tsc.SetException(e);
			}

			return tsc.Task;
		}
	}
}
