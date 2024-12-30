/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.13.2024

	License: MIT
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TRLEManager.Properties;

namespace TRLEManager
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			// Load window position and size from settings
			Rect winrect = App.GetMainWindowRect();
			if (winrect != default)
			{
				Left = winrect.X;
				Top = winrect.Y;
				Width = winrect.Width;
				Height = winrect.Height;
			}

			ListBox_InstalledTRLE.ItemContainerGenerator.ItemsChanged += ListBox_InstalledTRLE_ItemContainerGenerator_ItemsChanged;
			// Clear the design-time demo listbox item and set the item source to our TRLE collection
			ListBox_InstalledTRLE.Items.Clear();
			TRLECollection.SetAsItemsSource(ListBox_InstalledTRLE);
		}

		private void ListBox_InstalledTRLE_ItemContainerGenerator_ItemsChanged(object sender, ItemsChangedEventArgs e)
		{
			var generator = ListBox_InstalledTRLE.ItemContainerGenerator;

			foreach (var item in generator.Items)
			{
				var container = generator.ContainerFromItem(item);
				
			}
		}

		private void Button_AddTRLE_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = new TRLE();

			TRLEInfoEntryWindow infoEntryWindow = new TRLEInfoEntryWindow(info)
			{
				Owner = this
			};

			if (infoEntryWindow.ShowDialog() != true) return;

			// TODO: Locate the TRLE to see if it already exists
			int index = TRLECollection.GetTRLEIndexByEXEPath(info.EXEPath);
			if (index < 0)
				TRLECollection.Add(infoEntryWindow.Info);
			else
				TRLECollection.ReplaceTRLEInfo(index, infoEntryWindow.Info);
		}

		private void Button_BrowseTRLE_Click(object sender, RoutedEventArgs e)
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = App.GetBrowseTRLEURL(),
				UseShellExecute = true
			});
		}

		private void Button_ManagerSettings_Click(object sender, RoutedEventArgs e)
		{
			var settingsWindow = new ManagerSettings()
			{
				Owner = this,
			};

			settingsWindow.Show();
		}

		private T GetDataContext<T>(object sender) where T : class
		{
			return (sender as FrameworkElement).DataContext as T;
		}

		private void ListBoxMenu_Play_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);

			App.RunTRLE(info);
		}

		private void ListBoxMenu_Setup_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
			info.Setup();
		}

		private void ListBoxMenu_Download_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
		}

		private void ListBoxMenu_BrowseFolder_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
			info.BrowseFolder();
		}

		private void ListBoxMenu_LocateZip_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
		}

		private void ListBoxMenu_DeleteZip_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
		}

		private void UpdateTRLEDialog(TRLE info)
		{
			TRLEInfoEntryWindow infoEntry = new TRLEInfoEntryWindow(info)
			{
				Owner = this
			};

			if (infoEntry.ShowDialog() != true)
				return;

			int infoIndex = TRLECollection.IndexOf(infoEntry.Info);
			if (infoIndex == -1)
				return;

			TRLECollection.ReplaceTRLEInfo(infoIndex, info);
		}

		private void ListBoxMenu_Profile_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
			UpdateTRLEDialog(info);
		}

		private void ListBoxMenu_VisitWebsite_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
			info.ViewWebsite();
		}

		private void ListBoxMenu_ViewWalkthrough_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
			info.ViewWalkthrough();
		}

		private void ContextMenu_TRLEListFunctions(object sender, ContextMenuEventArgs e)
		{
			Grid itemGrid = sender as Grid;
			TRLE info = itemGrid.DataContext as TRLE;
			ContextMenu menu = itemGrid.ContextMenu;

			bool hasEXE = File.Exists(info.EXEPath);
			bool hasZIP = File.Exists(info.CompressedPath);

			bool hasInfoWebpage = Uri.TryCreate(info.InfoWebpageURL, UriKind.Absolute, out Uri infoWebpageURL)
				&& (infoWebpageURL.Scheme == Uri.UriSchemeHttp || infoWebpageURL.Scheme == Uri.UriSchemeHttps);

			bool hasDownload = Uri.TryCreate(info.WalkthroughURL, UriKind.Absolute, out Uri downloadUri)
				&& (downloadUri.Scheme == Uri.UriSchemeHttp || downloadUri.Scheme == Uri.UriSchemeHttps);

			bool hasWalkthrough = Uri.TryCreate(info.WalkthroughURL, UriKind.Absolute, out Uri walkthroughUri)
				&& (walkthroughUri.Scheme == Uri.UriSchemeHttp || walkthroughUri.Scheme == Uri.UriSchemeHttps);

			foreach (Control item in menu.Items)
			{
				switch (item.Tag)
				{
					case "Play":
					case "Setup":
					case "Separator1":
					case "BrowseFolder":
					case "RemoveFiles":
						item.Visibility = hasEXE ? Visibility.Visible : Visibility.Collapsed;
						break;

					case "BrowseZip":
					case "DeleteZip":
						item.Visibility = hasZIP ? Visibility.Visible : Visibility.Collapsed;
						break;

					case "ViewWalkthrough":
						item.Visibility = hasWalkthrough ? Visibility.Visible : Visibility.Collapsed;
						break;

					case "Download":
						item.Visibility = hasDownload ? Visibility.Visible : Visibility.Collapsed;
						break;

					case "VisitWebsite":
						item.Visibility = hasInfoWebpage ? Visibility.Visible : Visibility.Collapsed;
						break;

					case "Separator2":
						item.Visibility = hasInfoWebpage || hasDownload ? Visibility.Visible : Visibility.Collapsed;
						break;
				}
			}

			// If download URL OR zip file -> enable install

		}

		private void ListBoxMenu_DeleteEntry_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);

			DeleteEntryDialog ded = new DeleteEntryDialog(info)
			{
				Owner = this,
			};

			ded.ShowDialog();
		}

		private void Button_PlayTRLE_Click(object sender, RoutedEventArgs e)
		{
			TRLE trle = GetDataContext<TRLE>(sender);

			try
			{
				App.RunTRLE(trle);
			}
			catch (Error err)
			{
				err.LogError();
                App.StandardErrorMessageBox($"There was an error trying to run the game and startup was aborted.\n\n'{err.Message}'");
            }
		}

		private async void Button_DownloadTRLE_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);

			try
			{
				await info.Install();
			}
			catch (Error err)
			{
				err.LogError();
				App.StandardErrorMessageBox($"Failed to download TRLE.\n\n{err.Message}");
			}

            int infoIndex = TRLECollection.IndexOf(info);
			if (infoIndex == -1)
				TRLECollection.Add(info);

            TRLECollection.ReplaceTRLEInfo(infoIndex, info);
        }

		private void Button_EditTRLE_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
			UpdateTRLEDialog(info);
		}

		private void Button_WalkthroughTRLE_Click(object sender, RoutedEventArgs e)
		{
			TRLE info = GetDataContext<TRLE>(sender);
			info.ViewWalkthrough();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Settings.Default.MainWindowRect = $"{Left},{Top},{Width},{Height}";
		}

		private void ListBox_InstalledTRLEs_StackPanel_Loaded(object sender, RoutedEventArgs e)
		{
			StackPanel sp = (StackPanel)sender;
			TRLE trle = (TRLE)sp.DataContext;

			Button playButton = (Button)sp.Children[0];
			Button downloadButton = (Button)sp.Children[1];
			Button walkthroughButton = (Button)sp.Children[2];
			
			if (File.Exists(trle.EXEPath))
			{
				playButton.Visibility = Visibility.Visible;
				downloadButton.Visibility = Visibility.Collapsed;
			}
			else
			{
				playButton.Visibility = Visibility.Collapsed;
				downloadButton.Visibility = App.IsUrl(trle.DownloadURL) ? Visibility.Visible : Visibility.Collapsed;
			}

			walkthroughButton.Visibility = App.IsUrl(trle.WalkthroughURL) ? Visibility.Visible : Visibility.Collapsed;
		}
	}
}
