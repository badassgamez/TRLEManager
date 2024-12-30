/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.13.2024

	License: MIT
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Icon = System.Drawing.Icon;

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

		public string DriveSpaceUsed
		{
			get
			{
				return GetDriveSpaceUsed();
			}

			set
			{
				_drivespace = value;
			}
		}

		public DateTime LastPlayed { get; private set; }

        public bool UseGamepad { get; set; }

		public bool RemoveWindowBorder { get; set; }

        private ImageSource _icon;
		public ImageSource Icon
		{
			get
			{
				return GetIcon();
			}
			set
			{
				_icon = value;
			}
		}
		public ImageSource GetIcon(bool fresh = false)
		{
			if (_icon != null && !fresh)
				return _icon;

			var path = EXEPath;

			if (string.IsNullOrEmpty(path) || !File.Exists(path))
				return null;

			_icon = GetFileIcon(path);
			return _icon;
		}

		public string GetDriveSpaceUsed(bool fresh = false)
		{
			if (!string.IsNullOrEmpty(_drivespace) && !fresh)
				return _drivespace;

			return CalcDriveSpace();
		}

		public long GetFolderDriveSpace(bool fresh = false)
		{
			if (_folderdrivespace != null && !fresh)
				return _folderdrivespace.Value;

			return CalcDirSpace();
		}

		public long ZipDriveSpace(bool fresh = false)
		{
			if (_zipdrivespace != null && !fresh)
				return _zipdrivespace.Value;

			return CalcZipSpace();
		}

		private long? _folderdrivespace = null;
		private long? _zipdrivespace = null;
		private string _drivespace = null;

		public static string BulkSerialize(IEnumerable<TRLE> infos)
		{
			StringBuilder builder = new StringBuilder();

			foreach (TRLE trleinfo in infos)
			{
				// Note - converting to Base64 ensures a | character doesn't interfere with deserialization
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
			serialized.Append($"DriveSpaceUsed={_drivespace}\n");
			serialized.Append($"UseGamepad={UseGamepad}\n");
			serialized.Append($"LastPlayed={LastPlayed}\n");
			serialized.Append($"RemoveWindowBorder={RemoveWindowBorder}");

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
				if (pos == -1)
				{
					var err = new Error("Malformed serialized TRLE entry, '=' not found.");
					err.Data.Add("line", line);
					throw err;
				}

				string key;
				string val;

				try
				{
					key = line.Substring(0, pos);
					val = line.Substring(pos + 1);
				}
				catch (ArgumentOutOfRangeException e)
				{
					var err = new Error("Failed to parse TRLE entry from settings.", e);
					err.Data.Add("line", line);
					err.Data.Add("pos", pos);
					throw err;
				}

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
					case "DriveSpaceUsed": info._drivespace = string.IsNullOrEmpty(val) ? null : val; break;
					case "UseGamepad": bool.TryParse(val, out bool ugp); info.UseGamepad = ugp; break;
					case "LastPlayed": DateTime.TryParse(val, out DateTime dt); info.LastPlayed = dt; break;
					case "RemoveWindowBorder": bool.TryParse(val, out bool rwb); info.RemoveWindowBorder = rwb; break;
					default: break;
				}
				
				return info;
			});
		}

		public static long DoCalcDirSpace(DirectoryInfo dir)
		{
			long size = 0;

			if (dir == null)
				throw new Error("Directory info is null.");

            // calc file sizes in this dir
            {
                FileInfo[] files = null;
				try
				{
					files = dir.GetFiles();
				}
				catch (DirectoryNotFoundException e)
				{
					var err = new Error($"Failed to calculate directory size.", e);
					e.Data.Add("dir", dir);
					throw err;
				}

				foreach (FileInfo file in files)
					size += file.Length;
			}

			//calc file sizes in sub dirs
			{
				DirectoryInfo[] dirs = null;
				try
				{
					dirs = dir.GetDirectories();
				}
				catch (Exception e) when (
					e is DirectoryNotFoundException
					|| e is SecurityException
					|| e is UnauthorizedAccessException)
				{
					var err = new Error("Failed to calculate sub directory sizes.", e);
					err.Data.Add("dir", dir?.FullName);
					throw err;
				}

				foreach (DirectoryInfo di in dirs)
					size += DoCalcDirSpace(di);
			}

			return size;
		}

		public long CalcZipSpace()
		{
			string zipPath = CompressedPath;

			if (string.IsNullOrEmpty(zipPath))
				return 0;

			FileInfo fi;
			try
			{
				fi = new FileInfo(zipPath);
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is SecurityException
				|| e is ArgumentException
				|| e is UnauthorizedAccessException
				|| e is PathTooLongException
				|| e is NotSupportedException)
			{
				var err = new Error("Failed to calc zip size.", e);
				err.Data.Add("zipPath", zipPath);
				throw err;
			}

			_zipdrivespace = fi.Length;
			return _zipdrivespace.Value;
		}

		public long CalcDirSpace()
		{
			string exePath = EXEPath;

			if (string.IsNullOrEmpty(exePath))
				return 0;

			DirectoryInfo trleDirInfo;
            try
			{
				 trleDirInfo = Directory.GetParent(EXEPath);
			}
			catch (Exception e) when (
				e is IOException
				|| e is UnauthorizedAccessException
				|| e is ArgumentException
				|| e is ArgumentNullException
				|| e is PathTooLongException
				|| e is DirectoryNotFoundException
				|| e is NotSupportedException
				|| e is SecurityException)
			{
				var err = new Error("Failed to determine parent directory of executable.", e);
				e.Data.Add("exePath", exePath);
				throw err;
			}

            _folderdrivespace = DoCalcDirSpace(trleDirInfo);
            return _folderdrivespace.Value;
        }

		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		static public string SizeSuffix(long value, int decimalPlaces = 0)
		{
			if (value < 0)
			{
				var err = new Error($"Invalid value.");
				err.Data.Add("value", value);
				throw err;
			}

			var mag = (int)Math.Max(0, Math.Log(value, 1024));
			var adjustedSize = Math.Round(value / Math.Pow(1024, mag), decimalPlaces);
			return string.Format("{0} {1}", adjustedSize, SizeSuffixes[mag]);
		}

		public string CalcDriveSpace()
		{
			long dirSize = GetFolderDriveSpace();
			long zipSize = ZipDriveSpace();

			long totalSize = dirSize + zipSize;

			_drivespace = SizeSuffix(totalSize);
			return _drivespace;
		}

		public Process Play()
		{
			if (!File.Exists(EXEPath))
			{
				var err = new Error($"Executable doesn't exist.");
				err.Data.Add("EXEPath", EXEPath);
				throw err;
			}

			string exeDir;
			try
			{
				exeDir = Path.GetDirectoryName(EXEPath);
            } catch (Exception e) when (e is ArgumentException || e is PathTooLongException)
			{
				var err = new Error("Unable to start TRLE game due to an error.", e);
				err.Data.Add("EXEPath", EXEPath);
				throw err;
			}

			Process p = App.LaunchProcess(new ProcessStartInfo(EXEPath)
			{
				FileName = EXEPath,
				WorkingDirectory = exeDir,
				UseShellExecute = false,
			});

			LastPlayed = DateTime.Now;

			return p;
		}

		public void Setup()
		{
			string workingDir;
			try
			{
				workingDir = Path.GetDirectoryName(EXEPath);
			}
			catch (Exception e) when (
				e is ArgumentException
				|| e is PathTooLongException)
			{
				var err = new Error($"Failed to get working directory.\n\n{e.Message}", e);
				err.Data.Add("EXEPath", EXEPath);
				throw err;
			}

            App.LaunchProcess(new ProcessStartInfo()
			{
				FileName = EXEPath,
				Arguments = "-setup",
				WorkingDirectory = workingDir,
			});
		}

		public void BrowseFolder()
		{
			try
			{
				if (!Path.IsPathRooted(EXEPath))
				{
					var err = new Error($"Path needs to be absolute.");
					err.Data.Add("EXEPath", EXEPath);
					throw err;
				}
			}
			catch (ArgumentException e)
			{
				var err = new Error($"Error inspecting executable path.\n\n{e.Message}", e);
				err.Data.Add("EXEPath", EXEPath);
				throw err;
			}

			string directory;
			try
			{
				directory = Path.GetDirectoryName(EXEPath);
			}
			catch (Exception e) when (
				e is ArgumentException
				|| e is PathTooLongException)
			{
				var err = new Error($"Failed to get directory of executable.\n\n{e.Message}", e);
				err.Data.Add("EXEPath", EXEPath);
				throw err;
			}

			App.LaunchProcessString(directory);
		}

		public static async Task<string> Download(string downloadURL)
		{
			Uri downloadUri = App.ConvertToUri(downloadURL);

			string tempFileName;
			try
			{
				tempFileName = Path.Combine(App.GetDownloadDirectory().FullName, Path.GetFileName(Path.GetTempFileName()));
				tempFileName = Path.ChangeExtension(tempFileName, "zip");
			}
			catch (Exception e) when (
				e is ArgumentException
				|| e is ArgumentNullException
				|| e is IOException)
			{
				throw new Error($"Failed to create a temporary download file\n\n{e.Message}", e);
			}

			using (var httpClient = new HttpClient())
			{
				using (HttpResponseMessage response = await httpClient.GetAsync(downloadURL, HttpCompletionOption.ResponseHeadersRead))
				{
					try
					{
						response.EnsureSuccessStatusCode();
					}
					catch (HttpRequestException e)
					{
						var err = new Error($"Failed to download TRLE.\n\n{e.Message}", e);
						e.Data.Add("downloadURL", downloadURL);
						e.Data.Add("tempFileName", tempFileName);
						throw err;
					}

					long totalSize = response.Content.Headers.ContentLength ?? -1L;

					using (FileStream fs = App.OpenFile(tempFileName, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						using (Stream contentStream = await response.Content.ReadAsStreamAsync())
						{
							byte[] buffer = new byte[8192];
							long totalBytesRead = 0;
							int bytesRead;

							ProgressBarWindow progress = new ProgressBarWindow(totalSize <= 0)
							{
								Owner = App.Window,
							};

							progress.Show();

							async Task<int> DoRead()
							{
								try
								{
									return await contentStream.ReadAsync(buffer, 0, buffer.Length);
								}
								catch (Exception e) when (
									e is ArgumentNullException
									|| e is ArgumentOutOfRangeException
									|| e is ArgumentException
									|| e is NotSupportedException
									|| e is ObjectDisposedException
									|| e is InvalidOperationException)
								{
									var err = new Error($"The download failed.\n\n{e.Message}", e);
									err.Data.Add("downloadURL", downloadURL);
									err.Data.Add("tempFileName", tempFileName);
									err.Data.Add("totalSize", totalSize);
									err.Data.Add("totalBytesRead", totalBytesRead);
									throw err;
								}
							}

							async Task DoWrite()
							{
								try
								{
									await fs.WriteAsync(buffer, 0, bytesRead);
								}
								catch (Exception e) when (
									e is ArgumentNullException
									|| e is ArgumentOutOfRangeException
									|| e is NotSupportedException
									|| e is ObjectDisposedException
									|| e is InvalidOperationException)
								{
									var err = new Error($"There was an error writing to the download file.\n\n{e.Message}", e);
									err.Data.Add("downloadURL", downloadURL);
									err.Data.Add("tempFileName", tempFileName);
									err.Data.Add("totalSize", totalSize);
									err.Data.Add("totalBytesRead", totalBytesRead);
									throw err;
								}
							}

							try
							{
								while ((bytesRead = await DoRead()) > 0)
								{
									await DoWrite();

									totalBytesRead += bytesRead;

									if (totalSize > 0)
										progress.ProgressBar_Main.Value = (int)((totalBytesRead * 100) / totalSize); // Update progress bar
									
								}
							}
							finally
							{
								progress.Close();
							}
						}
					}
				}
			}

			return tempFileName;
		}

		private string GetExtractPath()
		{
			string installPathBase = App.GetInstallPathBase().FullName;
            try
            {
                return Path.Combine(installPathBase, Regex.Replace(Name, "[*/\\<>:|?]", ""));
            }
            catch (Exception e) when (
                e is ArgumentException
                || e is ArgumentNullException
                || e is RegexMatchTimeoutException)
            {
                var err = new Error($"Failed to create extract path.\n\n{e.Message}", e);
                err.Data.Add("installPathBase", installPathBase);
                err.Data.Add("TRLE Name", Name);
                throw err;
            }
        }

		private static void ExtractZip(string zipPath, string extractPath)
		{
            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);
            }
            catch (Exception e) when (
                e is ArgumentException
                || e is ArgumentNullException
                || e is RegexMatchTimeoutException
                || e is PathTooLongException
                || e is DirectoryNotFoundException
                || e is IOException
                || e is UnauthorizedAccessException
                || e is NotSupportedException
                || e is FileNotFoundException
                || e is InvalidDataException)
            {
                var err = new Error($"Unable to extract zip.\n\n{e.Message}", e);
                err.Data.Add("zipPath", zipPath);
                err.Data.Add("extractPath", extractPath);
                throw err;
            }
        }

		public async Task<bool> Install()
		{
			// Returns:
			//  true if the trle was downloaded
			//  false if the trle was not downloaded (installed from zip)
			string zipPath = CompressedPath;
			bool downloaded = false;
			
			if (!File.Exists(zipPath))
			{
				zipPath = await Download(DownloadURL);
				downloaded = true;
			}
						
            string extractPath = GetExtractPath();

			ExtractZip(zipPath, extractPath);

			try
			{
				if (downloaded)
				{
					App.DeleteFile(zipPath);
					CompressedPath = null;
				}
            }
            catch (Error e)
            {
                e.LogError();
                App.StandardExclamationMessageBox($"The TRLE was downloaded and extracted, however the temporary download file was unable to be removed.\n\n{e.Message}");
            }

			try
			{
				EXEPath = FindEXE();
			}
			catch (Error e)
			{
				e.LogError();
				App.StandardExclamationMessageBox($"Failed to locate an executable in the extracted directory. You will need to locate it manually.\n\n{e.Message}");
			}

			CalcDriveSpace();
			return downloaded;
		}

		public void DeleteZIP()
		{
			if (string.IsNullOrEmpty(CompressedPath))
				return;

			if (!File.Exists(CompressedPath))
			{
				CompressedPath = "";
				return;
			}

			try
			{
				File.Delete(CompressedPath);
			}
			catch (Exception e) when (
				e is ArgumentException
				|| e is ArgumentNullException
				|| e is DirectoryNotFoundException
				|| e is IOException
				|| e is NotSupportedException
				|| e is PathTooLongException
				|| e is UnauthorizedAccessException)
			{
				var err = new Error($"Failed to delete zip file.\n\n{e.Message}", e);
				err.Data.Add("CompressedPath", CompressedPath);
				throw err;
			}
			finally
			{
				CompressedPath = null;
			}
		}


		public void DeleteFolder()
		{

			try
			{
				string dirName = Path.GetDirectoryName(EXEPath);
				if (string.IsNullOrEmpty(dirName) || !Directory.Exists(dirName))
					return;
				
                Directory.Delete(dirName, true);
			}
			catch (Exception e) when (
				e is ArgumentException
				|| e is PathTooLongException
				|| e is ArgumentNullException
				|| e is IOException
				|| e is UnauthorizedAccessException
				|| e is DirectoryNotFoundException)
			{
				var err = new Error($"Failed to delete TRLE folder.\n\n{e.Message}", e);
				err.Data.Add("EXEPath", EXEPath);
				err.Data.Add("Exists", File.Exists(EXEPath));
				throw err;
			}
			finally
			{
                EXEPath = null;
            }
		}

		public string FindEXE()
		{
			string extractPath = GetExtractPath();
			
			string[] Traverse(string path)
			{
                string[] dirs;

                try
				{
                    string[] exeFiles = Directory.GetFiles(extractPath, "*.exe");
					if (exeFiles.Length != 0)
						return exeFiles;

					dirs = Directory.GetDirectories(path);
				}
				catch (Exception e) when (
					e is ArgumentException
					|| e is ArgumentNullException
					|| e is PathTooLongException
					|| e is DirectoryNotFoundException
					|| e is IOException
					|| e is UnauthorizedAccessException)
				{
					var err = new Error($"Couldn't retrive files or directories.\n\n{e.Message}", e);
					err.Data.Add("path", path);
					err.Data.Add("extractPath", extractPath);
					throw err;
				}

                foreach (string subdir in dirs)
                {
                    string[] result = Traverse(subdir);
                    if (result != null)
                        return result;
                }
				{
					var err = new Error($"Unable to find exe file.");
					err.Data.Add("extractPath", extractPath);
					throw err;
				}
			}

			string[] exeCandidates = Traverse(extractPath); 
			
			if (exeCandidates.Length == 1) return exeCandidates[0];
			
			List<string> fileNames = exeCandidates.Select((fullPath) => Path.GetFileName(fullPath)).ToList();
			ListSelection selector = new ListSelection(fileNames);

			if (selector.ShowDialog() != true)
				return null;

			return exeCandidates[selector.SelectionIndex];
		}

		public void ViewWebsite()
		{
			try
			{
				App.ShellWebsite(InfoWebpageURL);
			}
			catch (Error err)
			{
				err.LogError();
				App.StandardErrorMessageBox(err.Message);
			}
		}

		public void ViewWalkthrough()
		{
			try
			{
				App.ShellWebsite(WalkthroughURL);
			}
            catch (Error err)
            {
                err.LogError();
                App.StandardErrorMessageBox(err.Message);
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
			if (iconHandle == IntPtr.Zero)
			{
				var err = new Error($"Failed to get file info attempting to get icon.", new Win32Exception(Marshal.GetLastWin32Error()));
				err.Data.Add("filePath", filePath);
				err.Data.Add("File.Exists", File.Exists(filePath));
				err.Data.Add("SHGetFileInfo Flags", flags);
				throw err;
			}

			using (Icon icon = System.Drawing.Icon.FromHandle(shinfo.hIcon))
			{
				BitmapSource iconSource = Imaging.CreateBitmapSourceFromHIcon(
					icon.Handle,
					Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions());
				return iconSource;
			}
		}
	}
}
