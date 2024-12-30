/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.14.2024

	License: MIT
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using TRLEManager.Properties;


namespace TRLEManager
{
	public partial class ManagerSettings : Window
	{
		private GamepadInfo _selectedGamepad;
		private Gamepad _gamepad;

		private VirtualGamepadButton[] _gamepadMap; // maps gamepad buttons to standard layout buttons
		private Dictionary<VirtualGamepadButton, string> _virtualMap; // maps standard layout buttons to game function
		private Dictionary<string, Key> _keyboardMap; // maps game function to keyboard key

		public ManagerSettings()
		{
			InitializeComponent();

			_selectedGamepad = App.GetGamepadInfo();
			_gamepadMap = App.GetGamepadMapping();
			_virtualMap = App.GetVirtualGamepadMapping();
			_keyboardMap = App.GetKeyboardMapping();

			Radio_NoShift.IsChecked = true;

			TextBox_TRLEInstallPath.Text = App.GetInstallPathBase().FullName;
			TextBox_DownloadsPath.Text = App.GetDownloadDirectory().FullName;
			TextBox_BrowseTRLEURL.Text = App.GetBrowseTRLEURL();
			TextBox_GamepadName.Text = _selectedGamepad?.ToString();

			PopulateKeyboardMap();

			ShowHideAuxButtons();
		}

		private void PopulateKeyboardMap()
		{
			TextBox_Keybd_Up.Text = _keyboardMap["Up"].ToString();
			TextBox_Keybd_Down.Text = _keyboardMap["Down"].ToString();
			TextBox_Keybd_Left.Text = _keyboardMap["Left"].ToString();
			TextBox_Keybd_Right.Text = _keyboardMap["Right"].ToString();
			TextBox_Keybd_Action.Text = _keyboardMap["Action"].ToString();
			TextBox_Keybd_Jump.Text = _keyboardMap["Jump"].ToString();
			TextBox_Keybd_Roll.Text = _keyboardMap["Roll"].ToString();
			TextBox_Keybd_Draw.Text = _keyboardMap["Draw"].ToString();
			TextBox_Keybd_Look.Text = _keyboardMap["Look"].ToString();
			TextBox_Keybd_Walk.Text = _keyboardMap["Walk"].ToString();
			TextBox_Keybd_Duck.Text = _keyboardMap["Duck"].ToString();
			TextBox_Keybd_Dash.Text = _keyboardMap["Dash"].ToString();
			TextBox_Keybd_Inventory.Text = _keyboardMap["Inventory"].ToString();
			TextBox_Keybd_Flare.Text = _keyboardMap["Flare"].ToString();
			TextBox_Keybd_StepLeft.Text = _keyboardMap["StepLeft"].ToString();
			TextBox_Keybd_StepRight.Text = _keyboardMap["StepRight"].ToString();
			TextBox_Keybd_1.Text = _keyboardMap["1"].ToString();
			TextBox_Keybd_2.Text = _keyboardMap["2"].ToString();
			TextBox_Keybd_3.Text = _keyboardMap["3"].ToString();
			TextBox_Keybd_4.Text = _keyboardMap["4"].ToString();
			TextBox_Keybd_5.Text = _keyboardMap["5"].ToString();
			TextBox_Keybd_6.Text = _keyboardMap["6"].ToString();
			TextBox_Keybd_7.Text = _keyboardMap["7"].ToString();
			TextBox_Keybd_8.Text = _keyboardMap["8"].ToString();
			TextBox_Keybd_9.Text = _keyboardMap["9"].ToString();
			TextBox_Keybd_0.Text = _keyboardMap["0"].ToString();
			TextBox_Keybd_QuickLoad.Text = _keyboardMap["Quick Load"].ToString();
			TextBox_Keybd_QuickSave.Text = _keyboardMap["Quick Save"].ToString();
			TextBox_Keybd_Pause.Text = _keyboardMap["Pause"].ToString();
		}

		private enum GamepadShift
		{
			None,
			StartShifted,
			MenuShifted
		}

		private void PopulateVirtualGamepadMap(GamepadShift shift)
		{
			(Button_HatUp.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.HatUp];
			(Button_HatLeft.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.HatLeft];
			(Button_HatRight.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.HatRight];
			(Button_HatDown.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.HatDown];
			(Button_Start.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.Start];
			(Button_Menu.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.Menu];
			(Button_L3.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.L3];
			(Button_R3.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.R3];

			(Button_Aux1.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.Aux1];
			(Button_Aux2.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.Aux2];
			(Button_Aux3.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.Aux3];
			(Button_Aux4.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.Aux4];
			(Button_Aux5.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.Aux5];

			switch (shift)
			{
				case GamepadShift.None:
					(Button_L2.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.L2];
					(Button_L1.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.L1];
					(Button_R2.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.R2];
					(Button_R1.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.R1];
					(Button_Y.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.Y];
					(Button_X.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.X];
					(Button_B.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.B];
					(Button_A.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.A];
					break;

				case GamepadShift.StartShifted:
					(Button_L2.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.StartShiftedL2];
					(Button_L1.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.StartShiftedL1];
					(Button_R2.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.StartShiftedR2];
					(Button_R1.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.StartShiftedR1];
					(Button_Y.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.StartShiftedY];
					(Button_X.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.StartShiftedX];
					(Button_B.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.StartShiftedB];
					(Button_A.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.StartShiftedA];
					break;

				case GamepadShift.MenuShifted:
					(Button_L2.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.MenuShiftedL2];
					(Button_L1.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.MenuShiftedL1];
					(Button_R2.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.MenuShiftedR2];
					(Button_R1.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.MenuShiftedR1];
					(Button_Y.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.MenuShiftedY];
					(Button_X.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.MenuShiftedX];
					(Button_B.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.MenuShiftedB];
					(Button_A.Content as TextBlock).Text = _virtualMap[VirtualGamepadButton.MenuShiftedA];
					break;
			}
		}

		private void ShowHideAuxButtons()
		{
			if (_gamepad == null)
				return;

			int auxCount = (int)_gamepad.GetButtonCount() - 12;

			for (int i = 0; i < 5; i++)
			{
				Button btn = FindName($"Button_Aux{i + 1}") as Button;
				btn.Visibility = i < auxCount ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		private void HookGamepad()
		{
			if (_selectedGamepad == null)
				return;

			_gamepad = _selectedGamepad.ToGamepad();
			_gamepad.OnGamepadChanged += _gamepad_OnGamepadChanged;

			_gamepad.StartMonitor();
		}

		private void _gamepad_OnGamepadChanged(object sender, GamepadChangedEventArgs e)
		{
			Gamepad gp = (Gamepad)sender;
			var dpad = gp.HatSwitchValue;

			if ((dpad & 0x1) > 0 && (e.prevDpad & 0x1) == 0)
			{
				Button_HatUp.Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
			} else if ((dpad & 0x1) == 0 && (e.prevDpad & 0x1) > 0) { 
				Button_HatUp.ClearValue(BackgroundProperty);
			}

			if ((dpad & 0x2) > 0 && (e.prevDpad & 0x2) == 0)
			{
				Button_HatRight.Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
			} else if ((dpad & 0x2) == 0 && (e.prevDpad & 0x2) > 0) {
				Button_HatRight.ClearValue(BackgroundProperty);
			}

			if ((dpad & 0x4) > 0 && (e.prevDpad & 0x4) == 0)
			{
				Button_HatDown.Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
			} else if ((dpad & 0x4) == 0 && (e.prevDpad & 0x4) > 0) {
				Button_HatDown.ClearValue(BackgroundProperty);
			}

			if ((dpad & 0x8) > 0 && (e.prevDpad & 0x8) == 0)
			{
				Button_HatLeft.Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
			} else if ((dpad & 0x8) == 0 && (e.prevDpad & 0x8) > 0)
			{
				Button_HatLeft.ClearValue(BackgroundProperty);
			}	
			
			var buttonVals = gp.ButtonValues;
			for (int i = 0; i < e.buttonChangeCount; i++)
			{
				bool buttonDown = buttonVals[e.buttonChanges[i]];
				VirtualGamepadButton vButton = _gamepadMap[e.buttonChanges[i]];
				Button uiButton = FindName($"Button_{vButton}") as Button;

				if (buttonDown)
				{
					uiButton.Background = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));

					if (vButton == VirtualGamepadButton.Start)
						Radio_InventoryShift.IsChecked = true;
					else if (vButton == VirtualGamepadButton.Menu)
						Radio_PauseShift.IsChecked = true;
				}
				else
				{
					uiButton.ClearValue(BackgroundProperty);
					
					if (vButton == VirtualGamepadButton.Start)
					{
						if (buttonVals[(int)VirtualGamepadButton.Menu])
							Radio_PauseShift.IsChecked = true;
						else
							Radio_NoShift.IsChecked = true;
					}
					else if (vButton == VirtualGamepadButton.Menu)
					{
						if (buttonVals[(int)VirtualGamepadButton.Start])
							Radio_InventoryShift.IsChecked = true;
						else
							Radio_NoShift.IsChecked = true;
					}
				}
			}
        }

		private void SaveGamepadControls()
		{
			var result = new StringBuilder();

			for (int i = 0; i < _gamepadMap.Length; i++)
				result.Append($"{(ushort)_gamepadMap[i]}\n");

			if (result.Length > 0)
				result.Length -= 1;

			Settings.Default.GamepadMapping = result.ToString();
		}

		private void SaveGameKeyControls()
		{
			var builder = _virtualMap.Aggregate(new StringBuilder(), (result, mapping) =>
			{
				result.Append($"{(ushort)mapping.Key}={mapping.Value}\n");
				return result;
			});

			if (builder.Length > 0)
				builder.Length -= 1;

			Settings.Default.GameKeyMapping = builder.ToString();
		}

		private void SaveKeyboardControls()
		{
			var builder = _keyboardMap.Aggregate(new StringBuilder(), (result, mapping) =>
			{
				result.Append($"{mapping.Key}={(ushort)mapping.Value}\n");
				return result;
			});

			if (builder.Length > 0) 
				builder.Length -= 1;

			Settings.Default.KeyboardMapping = builder.ToString();
		}

		private void Button_OK_Click(object sender, RoutedEventArgs e)
		{
			string windowsPathPattern = "^[a-zA-Z]:\\\\(?:[^\\/:*?\\\"<>|]+\\\\?)*$";

			string trleInstallPath = TextBox_TRLEInstallPath.Text;
			if (Regex.Match(trleInstallPath, windowsPathPattern).Success)
				Settings.Default.TRLEInstallPath = trleInstallPath;

			string downloadsPath = TextBox_DownloadsPath.Text;
			if (Regex.Match(downloadsPath, windowsPathPattern).Success)
				Settings.Default.DownloadPath = downloadsPath;

			string browseTrleUrl = TextBox_BrowseTRLEURL.Text;
			if (App.IsUrl(browseTrleUrl))
				Settings.Default.BrowseTRLEURL = browseTrleUrl;

			if (_selectedGamepad != null)
				Settings.Default.SelectedGamepad = _selectedGamepad.Serialize();

			SaveGamepadControls();
			SaveGameKeyControls();
			SaveKeyboardControls();
			Close();
        }

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Button_TRLEInstallPathBrowse_Click(object sender, RoutedEventArgs e)
		{
			string newFolder = FolderPicker.ShowDialog("Select a TRLE Installation Directory...");
			if (newFolder != null)
				TextBox_TRLEInstallPath.Text = newFolder;
		}

		private void Button_DownloadsPath_Click(object sender, RoutedEventArgs e)
		{
			string newFolder = FolderPicker.ShowDialog("Select a Download Directory...");
			if (newFolder != null)
				TextBox_DownloadsPath.Text = newFolder;
		}

		private void Button_SelectGamepad_Click(object sender, RoutedEventArgs e)
		{
			var listSelectionWin = new ListSelection(Gamepad.Devices.GetDevices().GetGamepads());
			if (listSelectionWin.ShowDialog() != true)
				return;

			_selectedGamepad = listSelectionWin.Selection as GamepadInfo;

			_gamepad?.StopMonitor();

			HookGamepad();

			ShowHideAuxButtons();

			TextBox_GamepadName.Text = _selectedGamepad == null ? "" : _selectedGamepad.ToString();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			if (_gamepad == null)
				return;

			_gamepad?.StopMonitor();
		}

		private void TextBox_Keybd_LostFocus(object sender, RoutedEventArgs e)
		{
			TextBox tb = sender as TextBox;
			tb.PreviewKeyDown -= TextBox_Keybd_PreviewKeyDown;
			
			tb.Text = _keyboardMap[tb.Tag as string].ToString();
		}

		private void TextBox_Keybd_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			TextBox tb = sender as TextBox;
			tb.Text = "...";

			tb.PreviewKeyDown += TextBox_Keybd_PreviewKeyDown;
		}

		private void TextBox_Keybd_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			TextBox tb = sender as TextBox;
			tb.PreviewKeyDown -= TextBox_Keybd_PreviewKeyDown;

			tb.Text = e.Key.ToString();
			_keyboardMap[tb.Tag as string] = e.Key;
		}

		private void Button_GamepadButton_Click(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			VirtualGamepadButton tag = (VirtualGamepadButton)button.Tag;

			var gamepadIndex = Array.IndexOf(_gamepadMap, tag);

			var gameKey = _virtualMap[tag];

			var setup = new GamepadButtonSetup(_gamepad, gamepadIndex, gameKey);
			if (setup.ShowDialog() != true)
				return;

			_virtualMap[tag] = setup.finalFunction;

			if (setup.finalButton >= 0)
				_gamepadMap[setup.finalButton] = tag;
		}

		private void Radio_NoShift_Checked(object sender, RoutedEventArgs e)
		{
			PopulateVirtualGamepadMap(GamepadShift.None);
		}

		private void Radio_InventoryShift_Checked(object sender, RoutedEventArgs e)
		{
			PopulateVirtualGamepadMap(GamepadShift.StartShifted);
		}

		private void Radio_PausShift_Checked(object sender, RoutedEventArgs e)
		{
			PopulateVirtualGamepadMap(GamepadShift.MenuShifted);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			HookGamepad();
		}
	}
}
