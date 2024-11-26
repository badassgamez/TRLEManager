/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.13.2024

	License: MIT
*/

using System;
using System.IO;
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

			Label_TRCustomsID.Visibility = Visibility.Collapsed;
			TextBox_TRCustomsID.Visibility = Visibility.Collapsed;
			Button_TRCustomsLookup.Visibility = Visibility.Collapsed;

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
			}
		}

		private void Compile()
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
		}

		private void Button_OK_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Compile();
			Close();
		}

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void TRLENetLookup()
		{
			uint id;

			string textID = TextBox_TRLENetID.Text;
			if (!uint.TryParse(textID, out id))
			{
				MessageBox.Show($"Invalid TRLE ID '{textID}'", "TRLE.net ID lookup Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}

			TrleNetInfo info = TrleNet.GetTrleNetInfo(id);
			if (info == null)
			{
				MessageBox.Show($"The TRLE.net ID '{textID}' is not a valid TRLE, or an error occured");
				return;
			}

			TextBox_Name.Text = info.name;
			TextBox_Author.Text = info.author;

			TextBox_InfoWebpage.Text = info.infoWebpageUrl;
			TextBox_DownloadURL.Text = info.downloadUrl;
			TextBox_WalkthroughURL.Text = info.walkthroughUrl;
		}

		private void Button_TRLENetLookup_Click(object sender, RoutedEventArgs e)
		{
			TRLENetLookup();
		}

		private void Button_TRCustomsLookup_Click(object sender, RoutedEventArgs e)
		{

		}

		private void Button_EXEBrowse_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog()
			{
				Title = "Select TRLE Executable...",
				CheckFileExists = true,
				Filter = "Executables|*.exe"
			};

			string knownEXEPath = TextBox_InfoEXEPath.Text;
			if (!string.IsNullOrEmpty(knownEXEPath))
			{
				string parentDir = System.IO.Path.GetDirectoryName(knownEXEPath);
				if (!string.IsNullOrEmpty (parentDir))
					ofd.InitialDirectory = parentDir;
			}

			if (ofd.ShowDialog() != true)
				return;

			string selectedFile = ofd.FileName;

			TextBox_InfoEXEPath.Text = selectedFile;
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
			App.ShellWebsite(TextBox_WalkthroughURL.Text);
		}

		private void Button_Install_Click(object sender, RoutedEventArgs e)
		{
			Compile();

			var resultTask = Info.Install();
			resultTask.ContinueWith(result =>
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					if (result.Result == null)
						return;

					if (result.Result == true)
						TextBox_InfoZIPPath.Text = "";

					TextBox_InfoEXEPath.Text = Info.EXEPath;
				});
			});
		}

		private void Button_ViewInfoWebpage_Click(object sender, RoutedEventArgs e)
		{
			App.ShellWebsite(TextBox_InfoWebpage.Text);
		}

		private void TextBox_InfoWebpage_TextChanged(object sender, TextChangedEventArgs e)
		{
			Button_ViewInfoWebpage.IsEnabled = App.IsUrl(TextBox_InfoWebpage.Text);
		}

		private void TextBox_TRLENetID_TextChanged(object sender, TextChangedEventArgs e)
		{
			Button_TRLENetLookup.IsEnabled = !string.IsNullOrEmpty(TextBox_TRLENetID.Text);
		}

		private void TextBox_TRCustomsID_TextChanged(object sender, TextChangedEventArgs e)
		{
			Button_TRCustomsLookup.IsEnabled = !string.IsNullOrEmpty(TextBox_TRCustomsID.Text);
		}

		private void TextBox_TRLENetID_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				TRLENetLookup();
		}
	}
}
