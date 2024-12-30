/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.26.2024

	License: MIT
*/

using System.Windows;

namespace TRLEManager
{
	public partial class ProgressBarWindow : Window
	{
		public ProgressBarWindow(bool indetermined = false)
		{
			InitializeComponent();

			ProgressBar_Main.IsIndeterminate = indetermined;
		}
	}
}
