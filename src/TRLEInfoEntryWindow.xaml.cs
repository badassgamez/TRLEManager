/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.13.2024

	License: MIT
*/

using System;
using System.IO;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TRLEManager
{
	public partial class TRLEInfoEntryWindow : Window
	{
		public TRLE Info { get; private set; }

		public TRLEInfoEntryWindow(TRLE startinginfo)
		{
			InitializeComponent();

			//Label_TRCustomsID.Visibility = Visibility.Collapsed;
			//TextBox_TRCustomsID.Visibility = Visibility.Collapsed;
			//Button_TRCustomsLookup.Visibility = Visibility.Collapsed;

			Info = startinginfo;

			if (startinginfo != null)
			{
				TextBox_Name.Text = startinginfo.Name;
				TextBox_Author.Text = startinginfo.Author;
				TextBox_InfoWebpage.Text = startinginfo.InfoWebpageURL;
				TextBox_DownloadURL.Text = startinginfo.DownloadURL;
				TextBox_WalkthroughURL.Text = startinginfo.WalkthroughURL;
				TextBox_TRLENetID.Text = startinginfo.TRLENetID;
				TextBox_TRCustomsID.Text = startinginfo.TRCustomsID;
				TextBox_InfoEXEPath.Text = startinginfo.EXEPath;
				TextBox_InfoZIPPath.Text = startinginfo.CompressedPath;
				//CheckBox_EnableGamepad.IsChecked = startinginfo.UseGamepad;
				//CheckBox_RemoveWindowBorder.IsChecked = startinginfo.RemoveWindowBorder;
			}
		}

		private void UpdateInfo()
		{
			Info.Name = TextBox_Name.Text;
			Info.Author = TextBox_Author.Text;
			Info.InfoWebpageURL = TextBox_InfoWebpage.Text;
			Info.DownloadURL = TextBox_DownloadURL.Text;
			Info.WalkthroughURL = TextBox_WalkthroughURL.Text;
			Info.TRLENetID = TextBox_TRLENetID.Text;
			Info.TRCustomsID = TextBox_TRCustomsID.Text;
			Info.EXEPath = TextBox_InfoEXEPath.Text;
			Info.CompressedPath = TextBox_InfoZIPPath.Text;
			//Info.UseGamepad = CheckBox_EnableGamepad.IsChecked.Value;
			//Info.RemoveWindowBorder = CheckBox_RemoveWindowBorder.IsChecked.Value;
		}

		private void Button_OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			UpdateInfo();
			Close();
		}

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private async void TRLENetLookup()
		{
			string textID = TextBox_TRLENetID.Text;

            try {
                TrleNetInfo info = await TrleNet.GetTrleNetInfo(textID);
				
				TextBox_Name.Text = info.Title;
				TextBox_Author.Text = info.Author;

				TextBox_InfoWebpage.Text = info.WebpageURL;
				TextBox_DownloadURL.Text = info.DownloadURL;
				TextBox_WalkthroughURL.Text = info.WalkthroughURL;
			}
			catch (Error e)
			{
                e.LogError();
                App.StandardErrorMessageBox($"The TRLE.net ID '{textID}' is not a valid TRLE, or an error occured\n\n{e.Message}");
            }
        }

		private async void TRCustomsLookup()
		{
			string textID = TextBox_TRCustomsID.Text;

			try
			{
				TRCustomsInfo info = await TRCustoms.GetTRCustomsInfo(textID);

				TextBox_Name.Text = info.Title;
				TextBox_Author.Text = info.Author;

				TextBox_InfoWebpage.Text = info.WebpageURL;
				TextBox_DownloadURL.Text = info.DownloadURL;
				TextBox_WalkthroughURL.Text = info.WalkthroughURL;
				// TextBox_TRLENetID.Text = info.TRLENetID.ToString();
			}
			catch (Error e)
			{
				e.LogError();
				App.StandardErrorMessageBox($"The TRCustoms.org ID '{textID}' is not a valid TRLE, or an error occured\n\n{e.Message}");
			}
		}

		private void Button_TRLENetLookup_Click(object sender, RoutedEventArgs e)
		{
			TRLENetLookup();
		}

		private void Button_TRCustomsLookup_Click(object sender, RoutedEventArgs e)
		{
			TRCustomsLookup();
		}

		private void Button_EXEBrowse_Click(object sender, RoutedEventArgs e)
		{
			var ofd = new OpenFileDialog()
			{
				Title = "Select TRLE Executable...",
				CheckFileExists = true,
				Filter = "Executables|*.exe"
			};

			string knownEXEPath = TextBox_InfoEXEPath.Text;
			if (!string.IsNullOrEmpty(knownEXEPath))
			{
				try
				{
					string parentDir = Path.GetDirectoryName(knownEXEPath);
					if (!string.IsNullOrEmpty(parentDir))
						ofd.InitialDirectory = parentDir;
				}
				catch (ArgumentException) { }
				catch (PathTooLongException) { }
			}

			if (ofd.ShowDialog() != true)
				return;

			TextBox_InfoEXEPath.Text = ofd.FileName;
		}

		private void TestForInstallButtonEnabled()
		{
			bool isURL = Uri.TryCreate(TextBox_DownloadURL.Text, UriKind.Absolute, out Uri downloadUri)
				&& (downloadUri.Scheme == Uri.UriSchemeHttps || downloadUri.Scheme == Uri.UriSchemeHttp);

			bool isZip = File.Exists(TextBox_InfoZIPPath.Text);

			Button_Install.IsEnabled = isURL || isZip;
		}

		private void TextBox_DownloadURL_TextChanged(object sender, TextChangedEventArgs e)
		{
			TestForInstallButtonEnabled();
		}

		private void TextBox_InfoZIPPath_TextChanged(object sender, TextChangedEventArgs e)
		{
			TestForInstallButtonEnabled();
		}

		private void TextBox_WalkthroughURL_TextChanged(object sender, TextChangedEventArgs e)
		{
			Button_ViewWalkthrough.IsEnabled = App.IsUrl(TextBox_WalkthroughURL.Text);
		}

		private void Button_ViewWalkthrough_Click(object sender, RoutedEventArgs e)
		{
			string siteURL = TextBox_WalkthroughURL.Text;

            try
			{
				App.ShellWebsite(siteURL);
            }
            catch (Error err)
			{
				err.LogError();
                App.StandardErrorMessageBox($"Failed to show the walkthrough website. '{siteURL}'\n\n{err.Message}");
            }
		}

		private async void Button_Install_Click(object sender, RoutedEventArgs e)
		{
			UpdateInfo();

			try
			{
				switch (await Info.Install())
				{
					case true: TextBox_InfoZIPPath.Text = ""; break;
					case false: TextBox_InfoZIPPath.Text = Info.CompressedPath; break;
				}

                TextBox_InfoEXEPath.Text = Info.EXEPath;
            }
			catch (Error err)
			{
				err.LogError();
				App.StandardErrorMessageBox($"Failed to install.\n\n{err.Message}");
			}
		}

		private void Button_ViewInfoWebpage_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				App.ShellWebsite(TextBox_InfoWebpage.Text);
			}
			catch (Error err)
			{
				err.LogError();
				App.StandardErrorMessageBox($"Failed to launch webpage.\n\n{err.Message}");
			}
		}

		private void TextBox_InfoWebpage_TextChanged(object sender, TextChangedEventArgs e)
		{
			Button_ViewInfoWebpage.IsEnabled = App.IsUrl(TextBox_InfoWebpage.Text);
		}

		private void TextBox_TRLENetID_TextChanged(object sender, TextChangedEventArgs e)
		{
			string testID = TextBox_TRLENetID.Text;
            Button_TRLENetLookup.IsEnabled = !string.IsNullOrEmpty(testID) && uint.TryParse(testID, out uint _);
		}

		private void TextBox_TRCustomsID_TextChanged(object sender, TextChangedEventArgs e)
		{
			string testID = TextBox_TRCustomsID.Text;
            Button_TRCustomsLookup.IsEnabled = !string.IsNullOrEmpty(testID) && uint.TryParse(testID, out uint _);
		}

		private void TextBox_TRLENetID_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) TRLENetLookup();
		}

		private void TextBox_TRCustomsID_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) 
				TRCustomsLookup();
		}
	}
}
