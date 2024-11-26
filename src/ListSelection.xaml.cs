/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.16.2024

	License: MIT
*/

using System.Collections;
using System.Windows;

namespace TRLEManager
{
	public partial class ListSelection : Window
	{
		public object Selection { get; private set; }
		public int SelectionIndex { get; private set; }

		public ListSelection(IEnumerable populateList)
		{
			InitializeComponent();

			ListBox_Options.ItemsSource = populateList;
		}

		private void Button_OK_Click(object sender, RoutedEventArgs e)
		{
			Selection = ListBox_Options.SelectedItem;
			SelectionIndex = ListBox_Options.SelectedIndex;
			DialogResult = true;
			Close();
        }

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
        }
    }
}
