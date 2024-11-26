/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.15.2024

	License: MIT
*/

using System.Text;
using System.Text.RegularExpressions;
using System.Net;

namespace TRLEManager
{
	internal class TrleNetInfo 
	{
		public uint trleNetID;
		public string name;
		public string author;
		public string infoWebpageUrl;
		public string downloadUrl;
		public string walkthroughUrl;

		public bool ParseNameAndAuthors(string infopage)
		{
			Match nameAndAuthorMatch = Regex.Match(infopage, "<span class=\"subHeader Stil2\">\\s*(.*?)\\s*<br \\/><br \\/>\\s*<span class=\"smallText\">by<\\/span><br \\/><br \\/>\\s*((?:<a href=\"\\/sc\\/authorfeatures\\.php\\?aid=\\d+?\" class=\"linkl\">.*?<\\/a>.*\\s*)+)<\\/span>");

			if (!nameAndAuthorMatch.Success)
				return false;

			string authorsHTML = nameAndAuthorMatch.Groups[2].Value;

			MatchCollection authorsMatch = Regex.Matches(authorsHTML, "<a href=\"\\/sc\\/authorfeatures\\.php\\?aid=(\\d+?)\" class=\"linkl\">(.*?)<\\/a>.*\\s*");
			if (authorsMatch.Count == 0)
				return false;

			StringBuilder authors = new StringBuilder();
			string separator = ", ";

			foreach (Match match in authorsMatch)
			{
				authors.Append(match.Groups[2].Value);
				authors.Append(separator);
			}

			if (authors.Length > 2)
				authors.Length -= 2;

			name = nameAndAuthorMatch.Groups[1].Value;
			author = authors.ToString();

			return true;
		}
		public bool ParseInfoUrlAndID(string infopage)
		{
			Match idValidationMatch = Regex.Match(infopage, "<a href=\"(https:\\/\\/www\\.trle\\.net\\/sc\\/levelfeatures\\.php\\?lid=(\\d*))\">Level Information<\\/a>");

			if (idValidationMatch.Captures.Count <= 0)
				return false;
			
			string acquiredTrleNetID = idValidationMatch.Groups[2].Value;

			if (!uint.TryParse(acquiredTrleNetID, out uint testid))
				return false;

			infoWebpageUrl = idValidationMatch.Groups[1].Value;
			trleNetID = testid;
		
			return true;
		}
	}

	internal class TrleNet
	{
		private const string _trlenetUrl = "https://www.trle.net";
		private const string _trlenetInfopageUrl = _trlenetUrl + "/sc/levelfeatures.php?lid={0}";
		private const string _trlenetDownloadUrl = _trlenetUrl + "/scadm/trle_dl.php?lid={0}";
		private const string _trlenetWalkthroughUrl = _trlenetUrl + "/sc/Levelwalk.php?lid={0}";

		public static TrleNetInfo GetTrleNetInfo(uint trleNetID)
		{
			TrleNetInfo result = new TrleNetInfo();

			string infoWebpageUrl = string.Format(_trlenetInfopageUrl, trleNetID);

			WebClient client = new WebClient();
			string trlenetPage = client.DownloadString(infoWebpageUrl);

			if (!result.ParseNameAndAuthors(trlenetPage))
				return null;

			if (!result.ParseInfoUrlAndID(trlenetPage))
				return null;

			result.downloadUrl = string.Format(_trlenetDownloadUrl, trleNetID);
			result.walkthroughUrl = string.Format(_trlenetWalkthroughUrl, trleNetID);

			return result;
		}
	}
}
