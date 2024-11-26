/*
	Copyright © badassgamez Ltd.

	Author:     badassgamez
	Created:	11.13.2024

	License:    MIT
*/

using System.Windows;

namespace TRLEManager
{
	/// <summary>
	/// Interaction logic for DeleteEntryDialog.xaml
	/// </summary>
	public partial class DeleteEntryDialog : Window
	{
		private TRLE _info;

		public DeleteEntryDialog(TRLE info)
		{
			InitializeComponent();

			_info = info;

			// Temp hide Delete Saves until save backups implemented
			CheckBox_DeleteSaves.Visibility = Visibility.Collapsed;

			TextBox_TRLEName.Text = info.Name;
			TextBox_TRLEAuthor.Text = info.Author;
			TextBox_WebsiteUrl.Text = info.InfoWebpageURL;
			TextBox_ZipLocation.Text = info.CompressedPath;
			TextBox_ExeLocation.Text = info.EXEPath;

			UpdateSpaceReclaim();
		}

		public void UpdateSpaceReclaim()
		{
			if (_info == null)
				return;

			long space = 0;
			if (CheckBox_DeleteEXEFolder.IsChecked == true)
				space = _info.FolderDriveSpace;

			if (CheckBox_DeleteZIP.IsChecked == true)
				space += _info.ZipDriveSpace;

			string spaceStr = TRLE.SizeSuffix(space);
			Label_SpaceReclaim.Content = spaceStr;
		}

		private void Button_DeleteTRLE_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(this, $"Are you sure you want to remove {_info.Name}?", "Confirm Removal", MessageBoxButton.OKCancel, MessageBoxImage.Question)
				!= MessageBoxResult.OK) 
				return;

			DialogResult = true;

			bool deleteExeFolder = CheckBox_DeleteEXEFolder.IsChecked.GetValueOrDefault();
			bool deleteZipFile = CheckBox_DeleteZIP.IsChecked.GetValueOrDefault();
			bool removeTRLEEntry = CheckBox_RemoveEntry.IsChecked.GetValueOrDefault();
			// DeleteSaves = CheckBox_DeleteSaves.IsChecked.GetValueOrDefault();

			if (deleteExeFolder)
				_info.DeleteFolder();

			if (deleteZipFile)
				_info.DeleteZIP();

			if (removeTRLEEntry)
				TRLECollection.Remove(_info);

			Close();
		}

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void CheckBox_DeleteZIP_Checked(object sender, RoutedEventArgs e)
		{
			UpdateSpaceReclaim();
		}

		private void CheckBox_DeleteZIP_Unchecked(object sender, RoutedEventArgs e)
		{
			UpdateSpaceReclaim();
		}

		private void CheckBox_DeleteEXEFolder_Checked(object sender, RoutedEventArgs e)
		{
			UpdateSpaceReclaim();
		}

		private void CheckBox_DeleteEXEFolder_Unchecked(object sender, RoutedEventArgs e)
		{
			UpdateSpaceReclaim();
		}
	}
}
