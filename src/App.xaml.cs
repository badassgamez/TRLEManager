using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
/*
	Copyright © badassgamez Ltd.

	Author:     badassgamez
	Created:	11.13.2024

	License:    MIT
*/
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TRLEManager.Properties;

namespace TRLEManager
{
	public partial class App : Application
	{
		private const string _appFolder = "TRLEManager";

		private static TRLERunner _runningTRLE;

		public static MainWindow Window { get; private set; }

		private void TRLEManagerApp_Startup(object sender, StartupEventArgs e)
		{
			TRLECollection.LoadFromSettings();

			Window = new MainWindow();
			Window.Show();
		}

		private void TRLEManagerApp_Exit(object sender, ExitEventArgs e)
		{
			TRLECollection.SaveToSettings();
		}

		public static string GetDownloadPath()
		{
			string downloadPath = Settings.Default.DownloadPath;
			if (string.IsNullOrEmpty(downloadPath))
			{
				string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				downloadPath = Path.Combine(appDataFolder, $"{_appFolder}\\Downloads");

				if (!File.Exists(downloadPath))
					Directory.CreateDirectory(downloadPath);

				Settings.Default.DownloadPath = downloadPath;
			}

			return downloadPath;
		}
		
		public static string GetBrowseTRLEURL()
		{
			return Settings.Default.BrowseTRLEURL;
		}

		public static string GetInstallPathBase()
		{
			string installPath = Settings.Default.TRLEInstallPath;
			if (!string.IsNullOrEmpty(installPath))
				return installPath;
			
			// default
			string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			installPath = Path.Combine(appDataFolder, $"{_appFolder}\\TRLEs");

			if (!File.Exists(installPath))
				Directory.CreateDirectory(installPath);

			Settings.Default.DownloadPath = installPath;
		
			return installPath;
		}

		public static Rect GetMainWindowRect()
		{
			Rect result = default;

			string storedRect = Settings.Default.MainWindowRect;

			if (!string.IsNullOrEmpty(storedRect))
			{
				string[] split = storedRect.Split(',');
				
				result.X = int.Parse(split[0]);
				result.Y = int.Parse(split[1]);
				result.Width = int.Parse(split[2]);
				result.Height = int.Parse(split[3]);
			}

			return result;
		}

		public static GamepadInfo GetGamepadInfo()
		{
			string selectedGamepad = Settings.Default.SelectedGamepad;
			if (!string.IsNullOrEmpty(selectedGamepad))
			{
				var info = GamepadInfo.Deserialize(selectedGamepad);
				if (info.IsConnected())
					return info;
			}
			
			return new Gamepad.Devices().GetInfo(0);
		}

		public static void RunTRLE(TRLE trle)
		{
			if (_runningTRLE != null)
				return;
			try
			{
				_runningTRLE = new TRLERunner(trle);
				_runningTRLE.Start();

				_runningTRLE.OnClosed += _runningTRLE_OnClosed;
			}
			catch (Exception e)
			{
				_runningTRLE = null;

				if (e == null)
				{
					MessageBox.Show($"There was an unknown error trying to run the game and startup was aborted.", "Error Running TRLE", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				MessageBox.Show($"There was an error trying to run the game and startup was aborted.\n\n'{e.Message}'");
			}
		}

        private static void _runningTRLE_OnClosed(object sender, EventArgs e)
        {
            _runningTRLE = null;
        }

        public static VirtualGamepadButton[] GetGamepadMapping()
		{
			string gamepadMapping = Settings.Default.GamepadMapping;
			if (!string.IsNullOrEmpty(gamepadMapping))
			{
				try
				{
					string[] split = gamepadMapping.Split('\n');
					return split.Aggregate(new VirtualGamepadButton[split.Length], (ary, pair) =>
					{
						int pos = pair.IndexOf('=');
						string key = pair.Substring(0, pos);
						string value = pair.Substring(pos + 1);

						ary[uint.Parse(key)] = (VirtualGamepadButton)int.Parse(value);
						return ary;
					});
				}
				catch { }
			}

			return new VirtualGamepadButton[]
			{
				VirtualGamepadButton.X,
				VirtualGamepadButton.A,
				VirtualGamepadButton.B,
				VirtualGamepadButton.Y,
				VirtualGamepadButton.L1,
				VirtualGamepadButton.R1,
				VirtualGamepadButton.L2,
				VirtualGamepadButton.R2,
				VirtualGamepadButton.Start,
				VirtualGamepadButton.Menu,
				VirtualGamepadButton.L3,
				VirtualGamepadButton.R3,
				VirtualGamepadButton.Aux1,
				VirtualGamepadButton.Aux2,
				VirtualGamepadButton.Aux3,
				VirtualGamepadButton.Aux4,
				VirtualGamepadButton.Aux5,
			};
		}

		public static Dictionary<VirtualGamepadButton, string> GetVirtualGamepadMapping()
		{
			string gameKeyMapping = Settings.Default.GameKeyMapping;

			if (!string.IsNullOrEmpty(gameKeyMapping))
			{
				try
				{
					string[] nlSplits = gameKeyMapping.Split('\n');

					return nlSplits.Aggregate(new Dictionary<VirtualGamepadButton, string>(), (dict, mapping) =>
					{
						int pos = mapping.IndexOf('=');
						string key = mapping.Substring(0, pos);
						string value = mapping.Substring(pos + 1);

						dict.Add((VirtualGamepadButton)ushort.Parse(key), value);

						return dict;
					});
				}
				catch { }
			}

			// default keys
			return new Dictionary<VirtualGamepadButton, string>()
			{
				{ VirtualGamepadButton.HatUp, "Up" },
				{ VirtualGamepadButton.HatDown, "Down" },
				{ VirtualGamepadButton.HatLeft, "Left" },
				{ VirtualGamepadButton.HatRight, "Right" },
				{ VirtualGamepadButton.X, "Jump" },
				{ VirtualGamepadButton.A, "Action" },
				{ VirtualGamepadButton.B, "Roll" },
				{ VirtualGamepadButton.Y, "Draw" },
				{ VirtualGamepadButton.L1, "Look" },
				{ VirtualGamepadButton.R1, "Walk" },
				{ VirtualGamepadButton.L2, "Duck" },
				{ VirtualGamepadButton.R2, "Dash" },
				{ VirtualGamepadButton.Start, "Inventory" },
				{ VirtualGamepadButton.Menu, "Pause" },
				{ VirtualGamepadButton.L3, "Quick Load" },
				{ VirtualGamepadButton.R3, "Quick Save" },
				{ VirtualGamepadButton.StartShiftedX, "3" },
				{ VirtualGamepadButton.StartShiftedA, "1" },
				{ VirtualGamepadButton.StartShiftedB, "2" },
				{ VirtualGamepadButton.StartShiftedY, "4" },
				{ VirtualGamepadButton.StartShiftedL1, "5" },
				{ VirtualGamepadButton.StartShiftedR1, "Flare" },
				{ VirtualGamepadButton.StartShiftedL2, "6" },
				{ VirtualGamepadButton.StartShiftedR2, "7" },
				{ VirtualGamepadButton.MenuShiftedX, "" },
				{ VirtualGamepadButton.MenuShiftedA, "" },
				{ VirtualGamepadButton.MenuShiftedB, "" },
				{ VirtualGamepadButton.MenuShiftedY, "" },
				{ VirtualGamepadButton.MenuShiftedL1, "9" },
				{ VirtualGamepadButton.MenuShiftedR1, "" },
				{ VirtualGamepadButton.MenuShiftedL2, "0" },
				{ VirtualGamepadButton.MenuShiftedR2, "8" },
				{ VirtualGamepadButton.Aux1, "" },
				{ VirtualGamepadButton.Aux2, "" },
				{ VirtualGamepadButton.Aux3, "" },
				{ VirtualGamepadButton.Aux4, "" },
				{ VirtualGamepadButton.Aux5, "" },
			};
		}

		public static Dictionary<string, Key> GetKeyboardMapping()
		{
			string keyboardMapping = Settings.Default.KeyboardMapping;

			if (!string.IsNullOrEmpty(keyboardMapping))
			{
				string[] nlSplits = keyboardMapping.Split('\n');

				return nlSplits.Aggregate(new Dictionary<string, Key>(), (dict, mapping) => { 
					int pos = mapping.IndexOf("=");
					string key = mapping.Substring(0, pos);
					string value = mapping.Substring(pos + 1);

					dict.Add(key, (Key)ushort.Parse(value));

					return dict;
				});
			}

			// default Keyboard mapping
			return new Dictionary<string, Key>
			{
				{ "Up", Key.Up },
				{ "Right", Key.Right },
				{ "Down", Key.Down },
				{ "Left", Key.Left },
				{ "Duck", Key.OemPeriod },
				{ "Dash", Key.OemQuestion },
				{ "Walk", Key.LeftShift },
				{ "Jump", Key.LeftAlt },
				{ "Action", Key.LeftCtrl },
				{ "Draw", Key.Space },
				{ "Flare", Key.OemComma },
				{ "Look", Key.NumPad0 },
				{ "Roll", Key.End },
				{ "Inventory", Key.Escape },
				{ "StepLeft", Key.Delete },
				{ "StepRight", Key.PageDown },
				{ "1", Key.D1 },
				{ "2", Key.D2 },
				{ "3", Key.D3 },
				{ "4", Key.D4 },
				{ "5", Key.D5 },
				{ "6", Key.D6 },
				{ "7", Key.D7 },
				{ "8", Key.D8 },
				{ "9", Key.D9 },
				{ "0", Key.D0 },
				{ "Quick Load", Key.F6 },
				{ "Quick Save", Key.F5 },
				{ "Pause", Key.P },
			};
		}

		public static string ToBase64(string input)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
		}

		public static string FromBase64(string input)
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(input));
		}

		public static bool IsUrl(string url)
		{
			return Uri.TryCreate(url, UriKind.Absolute, out Uri uri)
				&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
		}

		public static void ShellWebsite(string url)
		{
			if (!IsUrl(url))
				return;

			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true,
			});
		}
	}
}
