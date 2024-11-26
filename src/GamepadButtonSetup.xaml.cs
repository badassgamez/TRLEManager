/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.21.2024

	License: MIT
*/

using System;
using System.Linq;
using System.Windows;

namespace TRLEManager
{
	public partial class GamepadButtonSetup : Window
	{
		Gamepad _gamepad;
		public int finalButton = 0;
		public string finalFunction = null;

		private bool _guardGamepadSelect;

		public GamepadButtonSetup(Gamepad gamepad, int initialButton, string initialFunction)
		{
			InitializeComponent();
			_gamepad = gamepad;

			finalButton = initialButton;
			finalFunction = initialFunction;

			TextBox_GamepadButton.Text = initialButton >= 0 ? initialButton.ToString() : "";
			TextBox_GameFunction.Text = initialFunction;
		}

		private void Button_SelectGamepadButton_Click(object sender, RoutedEventArgs e)
		{
			if (_guardGamepadSelect) return;
			_guardGamepadSelect = true;

			TextBox_GamepadButton.Text = "...";
			_gamepad.OnGamepadChanged += GamepadChanged;
        }

		private void GamepadChanged(object sender, EventArgs e)
		{
			var buttonValues = _gamepad.ButtonValues;
			var buttonCount = buttonValues.Count;

            for (int i=0; i < buttonCount; i++)
            {
                if (buttonValues[i])
				{
					_gamepad.OnGamepadChanged -= GamepadChanged;
					TextBox_GamepadButton.Text = i.ToString();

					_guardGamepadSelect = false;
					return;
				}
            }

			uint hatSwitchValue = _gamepad.HatSwitchValue;
			switch (hatSwitchValue)
			{
				default:
					return;
				case 0:
					TextBox_GamepadButton.Text = "17";
					break;
				case 2:
					TextBox_GamepadButton.Text = "18";
					break;
				case 4:
					TextBox_GamepadButton.Text = "19";
					break;
				case 6:
					TextBox_GamepadButton.Text = "20";
					break;
			}

			_gamepad.OnGamepadChanged -= GamepadChanged;
			_guardGamepadSelect = false;
		}

		private void Button_SelectGameFunction_Click(object sender, RoutedEventArgs e)
		{
			var keyboardMapping = App.GetKeyboardMapping();
			var listSelection = new ListSelection(keyboardMapping.Keys.ToArray());

			if (listSelection.ShowDialog() != true)
				return;

			TextBox_GameFunction.Text = listSelection.Selection as string;
        }

		private void Button_OK_Click(object sender, RoutedEventArgs e)
		{
			finalButton = int.Parse(TextBox_GamepadButton.Text);
			finalFunction = TextBox_GameFunction.Text;

			DialogResult = true;
			Close();
        }

		private void Button_Cancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			_gamepad.OnGamepadChanged -= GamepadChanged;
		}
	}
}
