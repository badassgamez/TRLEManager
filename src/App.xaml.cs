using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Policy;
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
			try
			{
				CoInitialize();
            }
			catch (Error err)
			{
				err.LogError();
			}

			try
			{
                TRLECollection.LoadFromSettings();
            }
			catch (Error err)
			{
                StandardErrorMessageBox($"There was an issue loading the saved TRLE list.\n\n{err.Message}");
				err.LogError();
			}

			try
			{
				Window = new MainWindow();
				Window.Show();
			}
			catch (Exception err)
			{
				StandardErrorMessageBox($"There was an error starting the main window.\n\n{err.Message}");
				Error.LogException(err);
			}
		}

		private void TRLEManagerApp_Exit(object sender, ExitEventArgs e)
		{
            CoUninitialize();

            try
			{
				TRLECollection.SaveToSettings();
			}
			catch (Error err)
			{
				StandardErrorMessageBox($"Failed to save TRLE collection.\n\n{err.Message}");
			}
			finally
			{   
                Settings.Default.Save();
            }
		}

		public static void StandardErrorMessageBox(string message, Window owner = null)
		{
			MessageBox.Show(owner ?? Window, message, "TRLE Manager Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		public static void StandardExclamationMessageBox(string message, Window owner = null)
		{
            MessageBox.Show(owner ?? Window, message, "TRLE Manager Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public static string GetBrowseTRLEURL()
        {
            return Settings.Default.BrowseTRLEURL;
        }

        private static DirectoryInfo GetDirectoryInfo(string path)
		{
			try
			{
				return new DirectoryInfo(path);
			}
			catch (Exception e) when (
				e is ArgumentNullException 
				|| e is SecurityException 
				|| e is ArgumentException 
				|| e is PathTooLongException)
			{
				var err = new Error($"There was an error retrieving directory information.\n\n{e.Message}", e);
				err.Data.Add("path", path);
				throw err;
			}
		}

		public static DirectoryInfo GetAppFolder(string folderName)
		{
			string appDataFolder = null;
			string thePath = null;
			DirectoryInfo dInfo = null;

			try
			{
                appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                thePath = Path.Combine(appDataFolder, $"{_appFolder}\\{folderName}");
				dInfo = Directory.CreateDirectory(thePath);
            }
			catch (Exception e) when (
				e is ArgumentException 
				|| e is PlatformNotSupportedException 
				|| e is ArgumentNullException 
				|| e is IOException 
				|| e is UnauthorizedAccessException
				|| e is DirectoryNotFoundException 
				|| e is NotSupportedException)
			{
				var err = new Error("Unable to create app directory.", e);
				err.Data.Add("folderName", folderName);
				err.Data.Add("appDataFolder", appDataFolder);
				err.Data.Add("thePath", thePath);
				throw err;
			}
						
			return dInfo;
        }

		public static DirectoryInfo GetDownloadDirectory()
		{
			string downloadPath = Settings.Default.DownloadPath;

			if (!string.IsNullOrEmpty(downloadPath))
                return GetDirectoryInfo(downloadPath);
            
			var dirInfo = GetAppFolder("Downloads");
			Settings.Default.DownloadPath = dirInfo.FullName;

			return dirInfo;
		}
		
		public static DirectoryInfo GetInstallPathBase()
		{
			string installPath = Settings.Default.TRLEInstallPath;

			if (!string.IsNullOrEmpty(installPath))
				return GetDirectoryInfo(installPath);

			var installPathBase = GetAppFolder("TRLEs");
			Settings.Default.TRLEInstallPath = installPathBase.FullName;
		
			return installPathBase;
		}

		public static DirectoryInfo GetErrorLogDirectory()
		{
            string errLogPath = Settings.Default.ErrorLogDirectory;

            if (!string.IsNullOrEmpty(errLogPath))
                return GetDirectoryInfo(errLogPath);

            var errLogDir = GetAppFolder("ErrLogs");
            Settings.Default.ErrorLogDirectory = errLogDir.FullName;

            return errLogDir;
        }

		public static Rect GetMainWindowRect()
		{
			Rect result = default;

			string storedRect = Settings.Default.MainWindowRect;

			if (string.IsNullOrEmpty(storedRect))
				return result;
			
			string[] split = storedRect.Split(',');
				
			result.X = int.Parse(split[0]);
			result.Y = int.Parse(split[1]);
			result.Width = int.Parse(split[2]);
			result.Height = int.Parse(split[3]);
			
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
			
			return Gamepad.Devices.GetDevices().GetInfo(0);
		}

		public static void RunTRLE(TRLE trle)
		{
			if (_runningTRLE != null)
				throw new Error($"A TRLE is already running");

			_runningTRLE = new TRLERunner(trle);
            _runningTRLE.OnClosed += _runningTRLE_OnClosed;
			_runningTRLE.Start();
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
				string[] split = gamepadMapping.Split('\n');
				return split.Aggregate(new VirtualGamepadButton[split.Length], (ary, pair) =>
				{
					int pos = pair.IndexOf('=');
					if (pos == -1) return ary;

					string key = pair.Substring(0, pos);
					string value = pair.Substring(pos + 1);

					if (!uint.TryParse(key, out uint k))
						return ary;

					if (!int.TryParse(key, out int val))
						return ary;

					ary[k] = (VirtualGamepadButton)val;

					return ary;
				});
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
						if (pos == -1) return dict;

						string key = mapping.Substring(0, pos);
						string value = mapping.Substring(pos + 1);

						if (!ushort.TryParse(key, out ushort k))
							return dict;

						dict.Add((VirtualGamepadButton)k, value);

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
					if (pos == -1) return dict;

					string key = mapping.Substring(0, pos);
					string value = mapping.Substring(pos + 1);

					if (!ushort.TryParse(value, out ushort val))
						return dict;

					dict.Add(key, (Key)val);

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
			try
			{
				return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
			}
			catch (Exception e) when (e is ArgumentException || e is ArgumentNullException || e is DecoderFallbackException || e is FormatException)
			{
				var err = new Error("Unable to convert value to base 64.", e);
				err.Data.Add("input", input);
				throw err;
			}
        }

		public static string FromBase64(string input)
		{
			try
			{
				return Encoding.UTF8.GetString(Convert.FromBase64String(input));
			}
			catch (Exception e) when (e is ArgumentException || e is ArgumentNullException || e is DecoderFallbackException || e is FormatException)
			{
                var err = new Error("Unable to convert value from base 64.", e);
                err.Data.Add("input", input);
                throw err; ;
			}
		}

		public static void ValidateUrl(string url)
		{
			Uri uri;
			try
			{
				uri = new Uri(url);
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is UriFormatException)
			{
				var err = new Error($"Failed to convert url.\n\n{e.Message}", e);
				err.Data.Add("url", url);
				throw err;
			}

			if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
				return;

			{
				var err = new Error("Invalid Url scheme.");
				err.Data.Add("url", url);
				err.Data.Add("scheme", uri.Scheme);
				throw err;
			}
		}
		
		public static bool IsUrl(string url)
		{
			return Uri.TryCreate(url, UriKind.Absolute, out Uri result) && 
				(result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
		}

		public static Uri ConvertToUri(string url)
		{
			Uri result;
			try
			{
				result = new Uri(url);

				if (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps)
					return result;

				var err = new Error($"URL does not use a supported scheme.");
				err.Data.Add("url", url);
				err.Data.Add("scheme", result.Scheme);
				throw err;
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is UriFormatException)
			{
				var err = new Error($"Failed to convert Url.\n\n{e.Message}", e);
				err.Data.Add("url", url);
				throw err;
			}
        }

		public static FileStream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
		{
			try
			{
				return new FileStream(path, mode, access, share);
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is ArgumentException
				|| e is NotSupportedException
				|| e is FileNotFoundException
				|| e is IOException
				|| e is SecurityException
				|| e is DirectoryNotFoundException
				|| e is UnauthorizedAccessException
				|| e is PathTooLongException
				|| e is ArgumentOutOfRangeException)
			{
				var err = new Error($"Failed to open a file.\n\n{e.Message}", e);
				err.Data.Add("path", path);
				err.Data.Add("mode", mode);
				err.Data.Add("access", access);
				err.Data.Add("share", share);
				throw err;
			}
		}

		public static void DeleteFile(string path)
		{
            try
            {
                File.Delete(path);
            }
            catch (Exception e) when (
                e is ArgumentNullException
                || e is ArgumentException
                || e is DirectoryNotFoundException
                || e is IOException
                || e is NotSupportedException
                || e is PathTooLongException
                || e is UnauthorizedAccessException)
            {
                var err = new Error($"Failed to delete file.\n\n{e.Message}", e);
                err.Data.Add("path", path);
                throw err;
            }
        }
		
		public static Process LaunchProcessString(string path)
		{
            try
            {
                return Process.Start(path);
            }
            catch (Exception e) when (
                e is Win32Exception
                || e is ObjectDisposedException
                || e is FileNotFoundException)
            {
                var err = new Error($"Error trying to launch process.\n\n{e.Message}");
                err.Data.Add("path", path);
                throw err;
            }
        }

		public static Process LaunchProcess(ProcessStartInfo startInfo)
		{
			try
			{
				return Process.Start(startInfo);
			}
            catch (Exception e) when (
                e is InvalidOperationException
                || e is ArgumentNullException
                || e is ObjectDisposedException
                || e is FileNotFoundException
                || e is Win32Exception
                || e is PlatformNotSupportedException)
            {
                var err = new Error($"Failed to launch process.\n\n{e.Message}", e);
                err.Data.Add("startInfo.FileName", startInfo.FileName);
                throw err;
            }
        }

		public static void ShellWebsite(string url)
		{
			ValidateUrl(url);

			LaunchProcess(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true,
			});
		}


		private const int E_INVALIDARG = unchecked((int)0x80070057);
		private const int E_OUTOFMEMORY = unchecked((int)0x8007000E);
		private const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

		private void CoInitialize()
		{
			string errMsg = "CoInitialize failed.";

			switch (CoInitialize(IntPtr.Zero))
			{
				case E_INVALIDARG:
					throw new Error($"{errMsg} Invalid Argument.");
				case E_OUTOFMEMORY:
					throw new Error($"{errMsg} Out of Memory.");
				case E_UNEXPECTED:
					throw new Error($"{errMsg} Catastrophic Failure.");

				default: break;
			}
		}

        [DllImport("ole32.dll", CharSet = CharSet.Auto)]
        private static extern int CoInitialize(IntPtr pvReserved);
        [DllImport("ole32.dll", CharSet = CharSet.Auto)]
        private static extern void CoUninitialize();
    }
}
