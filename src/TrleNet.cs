/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.15.2024

	License: MIT
*/

using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Windows.Forms;
using System.Linq.Expressions;

namespace TRLEManager
{
	

    internal class TrleNetInfo 
	{
        public const string TrleNetURL = "https://www.trle.net";
        public const string TrleNetInfopageURLTemplate = TrleNetURL + "/sc/levelfeatures.php?lid={0}";
        public const string TrleNetDownloadURLTemplate = TrleNetURL + "/scadm/trle_dl.php?lid={0}";
        public const string TrleNetWalkthroughURLTemplate = TrleNetURL + "/sc/Levelwalk.php?lid={0}";

        public uint ID { get; private set; }
        public string Title { get; private set; }
		public string Author { get; private set; }
		public string WebpageURL { get; private set; }
		public string DownloadURL { get; private set; }
		public string WalkthroughURL { get; private set; }

		public static TrleNetInfo CreateFromInfoPage(string infopage)
		{
			var result = new TrleNetInfo();

            Match idValidationMatch = Regex.Match(infopage, "<a href=\"(https:\\/\\/www\\.trle\\.net\\/sc\\/levelfeatures\\.php\\?lid=(\\d*))\">Level Information<\\/a>");

			if (idValidationMatch.Captures.Count <= 0)
			{
				var e = new Error("Unable to validate the ID.");
				e.Data.Add("Info Page", infopage);
				throw e;
			}

            result.WebpageURL = idValidationMatch.Groups[1].Value;

            string acquiredTrleNetID = idValidationMatch.Groups[2].Value;

			uint trleNetID;
			try
			{
				trleNetID = uint.Parse(acquiredTrleNetID);
			}
			catch (Exception eInner) when (
				eInner is ArgumentNullException 
				|| eInner is FormatException
				|| eInner is OverflowException)
			{
				var e = new Error("The HTML is malformed. Unable to validate the TRLE.net ID.", eInner);
				e.Data.Add("Acquired TRLE.net ID", acquiredTrleNetID);
				e.Data.Add("Info Page", infopage);
				throw e;
			}

            result.ID = trleNetID;
            result.DownloadURL = string.Format(TrleNetDownloadURLTemplate, trleNetID);
            result.WalkthroughURL = string.Format(TrleNetWalkthroughURLTemplate, trleNetID);

            Match nameAndAuthorMatch = Regex.Match(infopage, "<span class=\"subHeader Stil2\">\\s*(.*?)\\s*<br \\/><br \\/>\\s*<span class=\"smallText\">by<\\/span><br \\/><br \\/>\\s*((?:<a href=\"\\/sc\\/authorfeatures\\.php\\?aid=\\d+?\" class=\"linkl\">.*?<\\/a>.*\\s*)+)<\\/span>");

			if (!nameAndAuthorMatch.Success)
			{
				var err = new Error("The HTML is malformed. Unable to read the TRLE title.");
				err.Data.Add("Info Page", infopage);
				throw err;
			}

            result.Title = nameAndAuthorMatch.Groups[1].Value;

            string authorsHTML = nameAndAuthorMatch.Groups[2].Value;

			MatchCollection authorsMatch = Regex.Matches(authorsHTML, "<a href=\"\\/sc\\/authorfeatures\\.php\\?aid=(\\d+?)\" class=\"linkl\">(.*?)<\\/a>.*\\s*");
			if (authorsMatch.Count == 0)
			{
				var err = new Error("The HTML is misformed. Unable to read authors.");
				err.Data.Add("Info Page", infopage);
				throw err;
			}

            var authors = new StringBuilder();
			string separator = ", ";

			foreach (Match match in authorsMatch)
			{
				authors.Append(match.Groups[2].Value);
				authors.Append(separator);
			}

			authors.Length -= separator.Length;

			result.Author = authors.ToString();

            return result;
		}

		private TrleNetInfo() { }
	}

	internal class TrleNet
	{
		public static string GetTrleNetInfoPage(uint trleNetID)
		{
			try
			{
				return string.Format(TrleNetInfo.TrleNetInfopageURLTemplate, trleNetID);
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is FormatException)
			{
				var err = new FormatException("Faild to construct TRLE.net URL.", e);
				err.Data.Add("Infopage URL Template", TrleNetInfo.TrleNetInfopageURLTemplate);
				err.Data.Add("trleNetID", trleNetID);
				throw err;
			}
		}

		public static Task<TrleNetInfo> GetTrleNetInfo(string trleNetID)
		{
			try
			{
				return GetTrleNetInfo(uint.Parse(trleNetID));
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is FormatException
				|| e is OverflowException)
			{
				var err = new Error("TRLE.net id is malformed.", e);
				err.Data.Add("trleNetID", trleNetID);
				throw err;
			}
		}

        public static Task<TrleNetInfo> GetTrleNetInfo(uint trleNetID)
		{
			var tsc = new TaskCompletionSource<TrleNetInfo>();

			var client = new WebClient();

			client.DownloadStringCompleted += (_, eventArgs) =>
			{
				string trlenetPage = eventArgs.Result;

                try
				{
                    tsc.SetResult(TrleNetInfo.CreateFromInfoPage(trlenetPage));
				}
				catch (Exception e)
				{
					tsc.SetException(e);
				}
			};

			try
			{
                client.DownloadStringAsync(new Uri(GetTrleNetInfoPage(trleNetID)));
            }
			catch (FormatException e)
			{
				tsc.SetException(e);
			}

			return tsc.Task;
		}
	}
}
