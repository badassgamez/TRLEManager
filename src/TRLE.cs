/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.13.2024

	License: MIT
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TRLEManager
{
	public class TRLE
	{

		public string Name { get; set; }
		public string Author { get; set; }
		public string InfoWebpageURL { get; set; }
		public string DownloadURL { get; set; }
		public string WalkthroughURL { get; set; }
		public string TRLENetID { get; set; }
		public string TRCustomsID { get; set; }
		public string EXEPath { get; set; }
		public string CompressedPath { get; set; }

		public DateTime LastPlayed { get; private set; }

		private ImageSource _icon;
		public ImageSource Icon
		{
			get
			{
				if (_icon != null)
					return _icon;

				var path = EXEPath;

				if (string.IsNullOrEmpty(path) || !File.Exists(path))
					return null;

				_icon = GetFileIcon(path);
				return _icon;
			}
			set
			{
				_icon = value;
			}
		}

		public bool UseGamepad { get; set; }

		public string DriveSpaceUsed
		{
			get
			{
				if (!string.IsNullOrEmpty(_drivespace))
					return _drivespace;

				return CalcDriveSpace();
			}
			set
			{
				_drivespace = value;
			}
		}

		public long FolderDriveSpace
		{
			get
			{
				if (_folderdrivespace != null)
					return _folderdrivespace.Value;

				return CalcDirSpace();
			}
		}

		public long ZipDriveSpace
		{
			get
			{
				if (_zipdrivespace != null)
					return _zipdrivespace.Value;

				return CalcZipSpace();
			}
		}

		private long? _folderdrivespace = null;
		private long? _zipdrivespace = null;
		private string _drivespace = null;

		public static string BulkSerialize(IEnumerable<TRLE> infos)
		{
			StringBuilder builder = new StringBuilder();

			foreach (TRLE trleinfo in infos)
			{
				builder.Append(App.ToBase64(trleinfo.Serialize()));
				builder.Append('|');
			}

			if (builder.Length > 0)
				builder.Length--;

			return builder.ToString();
		}

		public static T BulkDeserialize<T>(string input) where T : ICollection<TRLE>, new()
		{
			T result = new T();
			if (string.IsNullOrEmpty(input)) return result;

			string[] rawinfos = input.Split('|');

			foreach (string info in rawinfos)
				result.Add(Deserialize(App.FromBase64(info)));

			return result;
		}

		public string Serialize()
		{
			StringBuilder serialized = new StringBuilder();

			serialized.Append($"Name={Name}\n");
			serialized.Append($"Author={Author}\n");
			serialized.Append($"InfoWebpageURL={InfoWebpageURL}\n");
			serialized.Append($"DownloadURL={DownloadURL}\n");
			serialized.Append($"WalkthroughURL={WalkthroughURL}\n");
			serialized.Append($"TRLENetID={TRLENetID}\n");
			serialized.Append($"TRCustomsID={TRCustomsID}\n");
			serialized.Append($"EXEPath={EXEPath}\n");
			serialized.Append($"CompressedPath={CompressedPath}\n");
			serialized.Append($"DriveSpaceUsed={DriveSpaceUsed}\n");
			serialized.Append($"UseGamepad={UseGamepad}\n");
			serialized.Append($"LastPlayed={LastPlayed}\n");

			return serialized.ToString();
		}

		public static TRLE Deserialize(string serial)
		{
			string[] split = serial.Split('\n');

			return split.Aggregate(new TRLE(), (info, line) =>
			{
				if (string.IsNullOrEmpty(line))
					return info;

				int pos = line.IndexOf('=');
				string key = line.Substring(0, pos);
				string val = line.Substring(pos + 1);

				switch (key)
				{
					case "Name": info.Name = val; break;
					case "Author": info.Author = val; break;
					case "InfoWebpageURL": info.InfoWebpageURL = val; break;
					case "DownloadURL": info.DownloadURL = val; break;
					case "WalkthroughURL": info.WalkthroughURL = val; break;
					case "TRLENetID": info.TRLENetID = val; break;
					case "TRCustomsID": info.TRCustomsID = val; break;
					case "EXEPath": info.EXEPath = val; break;
					case "CompressedPath": info.CompressedPath = val; break;
					case "DriveSpaceUsed": info.DriveSpaceUsed = val; break;
					case "UseGamepad": bool.TryParse(val, out bool ugp); info.UseGamepad = ugp; break;
					case "LastPlayed": DateTime.TryParse(val, out DateTime dt); info.LastPlayed = dt; break;
					default: break;
				}
				
				return info;
			});
		}

		public static long DoCalcDirSpace(DirectoryInfo dir)
		{
			long size = 0;
			
			{
				FileInfo[] files = dir.GetFiles();

				foreach (FileInfo file in files)
				{
					size += file.Length;
				}
			}

			{
				DirectoryInfo[] dirs = dir.GetDirectories();

				foreach (DirectoryInfo di in dirs)
				{
					size += DoCalcDirSpace(di);
				}
			}

			return size;
		}

		public long CalcZipSpace()
		{
			try
			{
				FileInfo fi = new FileInfo(CompressedPath);
				_zipdrivespace = fi.Length;
				return _zipdrivespace.Value;
			}
			catch
			{
				return 0;
			}
		}

		public long CalcDirSpace()
		{
			try
			{
				DirectoryInfo trleDirInfo = Directory.GetParent(EXEPath);
				_folderdrivespace = DoCalcDirSpace(trleDirInfo);
				return _folderdrivespace.Value;
			}
			catch
			{
				return 0;
			}
		}

		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		static public string SizeSuffix(long value, int decimalPlaces = 0)
		{
			if (value < 0)
			{
				throw new ArgumentException("Bytes should not be negative", "value");
			}
			var mag = (int)Math.Max(0, Math.Log(value, 1024));
			var adjustedSize = Math.Round(value / Math.Pow(1024, mag), decimalPlaces);
			return String.Format("{0} {1}", adjustedSize, SizeSuffixes[mag]);
		}

		public string CalcDriveSpace()
		{
			long dirSize = this.FolderDriveSpace;
			long zipSize = this.ZipDriveSpace;

			long totalSize = dirSize + zipSize;

			_drivespace = SizeSuffix(totalSize);
			return _drivespace;
		}

		public Process Play()
		{
			if (!File.Exists(EXEPath)) return null;

			ProcessStartInfo psi = new ProcessStartInfo(EXEPath)
			{
				FileName = EXEPath,
				WorkingDirectory = Path.GetDirectoryName(EXEPath),
				UseShellExecute = false,
			};

			Process p = Process.Start(psi);

			LastPlayed = DateTime.Now;

			return p;
		}

		public bool Setup()
		{
			if (!File.Exists(EXEPath)) return false;

			ProcessStartInfo psi = new ProcessStartInfo()
			{
				FileName = EXEPath,
				Arguments = "-setup",
				WorkingDirectory = Path.GetDirectoryName(EXEPath),
			};

			Process.Start(psi);
			return true;
		}

		public void BrowseFolder()
		{
			if (!Path.IsPathRooted(EXEPath))
				return;
			
			string directory = Path.GetDirectoryName(EXEPath);

			if (!Directory.Exists(directory))
				return;

			Process.Start(directory);
		}

		public static Task<string> Download(string downloadURL)
		{
			if (!Uri.TryCreate(downloadURL, UriKind.Absolute, out Uri downloadUri)
				|| (downloadUri.Scheme != Uri.UriSchemeHttp && downloadUri.Scheme != Uri.UriSchemeHttps))
			{
				MessageBox.Show($"The download location '{downloadURL}' is not valid.", "Bad Download URL", MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}

			string tempFileName = Path.Combine(App.GetDownloadPath(), Path.GetFileName(Path.GetTempFileName()));
			tempFileName = Path.ChangeExtension(tempFileName, "zip");

			ProgressBarWindow progress = new ProgressBarWindow()
			{
				Owner = App.Window,
			};
			
			progress.Show();

			var tcs = new TaskCompletionSource<string>();

			WebClient webClient = new WebClient();
			webClient.DownloadProgressChanged += (sender, e) =>
			{
				Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, (Action)(() =>
				{
					progress.ProgressBar_Main.Value = e.ProgressPercentage;
				}));
			};
			
			webClient.DownloadFileCompleted += (sender, e) => 
			{
				Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, (Action)(() =>
				{
					progress.Close();
					tcs.SetResult(tempFileName);
				}));
			};

			webClient.DownloadFileAsync(new Uri(downloadURL), tempFileName);

			return tcs.Task;
		}

		public async Task<bool?> Install()
		{
			// Returns:
			//	null if not installed
			//  true if the trle was downloaded
			//  false if the trle was not downloaded (installed from zip)
			string zipPath = CompressedPath;
			bool downloaded = false;
			if (!File.Exists(zipPath))
			{
				zipPath = await Download(DownloadURL);
				if (zipPath == null)
					return null;

				downloaded = true;
			}

			string extractPath = Path.Combine(App.GetInstallPathBase(), Regex.Replace(Name, "[*/\\<>:|?]", ""));
			try
			{
				ZipFile.ExtractToDirectory(zipPath, extractPath);
			}
			catch { }

			if (downloaded)
			{
				CompressedPath = null;
				File.Delete(zipPath);
			}

			string exePath = FindEXE();
			if (exePath == null)
				return null;

			EXEPath = exePath;

			return downloaded;
		}

		public bool DeleteZIP()
		{
			try
			{
				if (!File.Exists(CompressedPath)) 
					return false;

				File.Delete(CompressedPath);
				CompressedPath = null;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool DeleteFolder()
		{
			try
			{
				string dir = Path.GetDirectoryName(EXEPath);
				if (!Directory.Exists(dir)) return false;
				Directory.Delete(dir, true);
				EXEPath = null;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public string FindEXE()
		{
			string extractPath = Path.Combine(App.GetInstallPathBase(), Regex.Replace(Name, "[*/\\<>:|?]", ""));
			if (!Directory.Exists(extractPath))
				return null;
			
			string[] exeFiles;

			string[] Traverse(string path)
			{
				try
				{
					exeFiles = Directory.GetFiles(extractPath, "*.exe");
					if (exeFiles.Length != 0)
						return exeFiles;
					
					foreach (string subdir in Directory.GetDirectories(path))
					{
						string[] result = Traverse(subdir);
						if (result != null) 
							return result;
					}
				}
				catch
				{ }
				return null;
			}

			exeFiles = Traverse(extractPath);
			if (exeFiles == null || exeFiles.Length == 0)
				return null;

			if (exeFiles.Length == 1)
				return exeFiles[0];
			
			List<string> fileNames = exeFiles.Select((fullPath) => Path.GetFileName(fullPath)).ToList();
			ListSelection selector = new ListSelection(fileNames);

			if (selector.ShowDialog() != true)
				return null;

			return exeFiles[selector.SelectionIndex];
		}

		public void ViewWebsite()
		{
			if (Uri.TryCreate(InfoWebpageURL, UriKind.Absolute, out Uri walkthroughUri)
					&& (walkthroughUri.Scheme == Uri.UriSchemeHttp || walkthroughUri.Scheme == Uri.UriSchemeHttps))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = InfoWebpageURL,
					UseShellExecute = true,
				});
			}
		}

		public void ViewWalkthrough()
		{
			if (Uri.TryCreate(WalkthroughURL, UriKind.Absolute, out Uri walkthroughUri)
					&& (walkthroughUri.Scheme == Uri.UriSchemeHttp || walkthroughUri.Scheme == Uri.UriSchemeHttps))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = WalkthroughURL,
					UseShellExecute = true,
				});
			}
		}

		private const uint SHGFI_ICON = 0x100;
		private const uint SHGFI_LARGEICON = 0x0; // Large icon
		private const uint SHGFI_SMALLICON = 0x1; // Small icon
		private const uint SHGFI_SYSICONINDEX = 0x4000;
		private const uint SHGFI_USEFILEATTRIBUTES = 0x10;

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct SHFILEINFO
		{
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string szDisplayName;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}

		public static BitmapSource GetFileIcon(string filePath, bool largeIcon = true)
		{
			var shinfo = new SHFILEINFO();

			uint flags = SHGFI_ICON | (largeIcon ? SHGFI_LARGEICON : SHGFI_SMALLICON);
			IntPtr iconHandle = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

			if (iconHandle != IntPtr.Zero)
			{
				using (Icon icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
				{
					BitmapSource iconSource = Imaging.CreateBitmapSourceFromHIcon(
						icon.Handle,
						Int32Rect.Empty,
						BitmapSizeOptions.FromEmptyOptions());
					return iconSource;
				}
			}
			return null;
		}
	}
}
