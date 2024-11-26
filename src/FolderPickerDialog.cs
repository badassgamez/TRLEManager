/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created: 11.15.2024

	License: MIT
*/
using System;
using System.Runtime.InteropServices;

namespace TRLEManager
{
	class FolderPicker 
	{
		public static string ShowDialog(string title)
		{
			// Create the FileOpenDialog object
			var dialog = (IFileOpenDialog) new FileOpenDialog();

			dialog.SetTitle(title);

			// Set the options on the dialog
			dialog.GetOptions(out uint options);
			options &= ~((uint)(FOS.FOS_PATHMUSTEXIST | FOS.FOS_FILEMUSTEXIST));
			options |= (uint)(FOS.FOS_PICKFOLDERS);
			dialog.SetOptions(options);
			

			// Show the dialog
			int hr = dialog.Show(IntPtr.Zero);
			if (hr == 0) // S_OK
			{
				// Retrieve the result
				dialog.GetResult(out IShellItem item);
				hr = item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out IntPtr pszPath);
				if (hr == 0)
				{
					string path = Marshal.PtrToStringUni(pszPath);
					Marshal.FreeCoTaskMem(pszPath);
					return path;
				}
			}
			return null;
		}

		// COM interop definitions
		[ComImport]
		[Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
		[ClassInterface(ClassInterfaceType.None)]
		private class FileOpenDialog
		{
		}

		[ComImport]
		[Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IFileOpenDialog
		{
			// IModalWindow
			[PreserveSig]
			int Show([In] IntPtr hwndParent);

			// IFileDialog
			void SetFileTypes(); // Not used
			void SetFileTypeIndex(); // Not used
			void GetFileTypeIndex(); // Not used
			void Advise(); // Not used
			void Unadvise(); // Not used
			void SetOptions([In] uint fos);
			void GetOptions(out uint fos);
			void SetDefaultFolder(); // Not used
			void SetFolder(); // Not used
			void GetFolder(); // Not used
			void GetCurrentSelection(); // Not used
			void SetFileName(); // Not used
			void GetFileName(); // Not used
			void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
			void SetOkButtonLabel(); // Not used
			void SetFileNameLabel(); // Not used
			void GetResult(out IShellItem ppsi);
			void AddPlace(); // Not used
			void SetDefaultExtension(); // Not used
			void Close(); // Not used
			void SetClientGuid(); // Not used
			void ClearClientData(); // Not used
			void SetFilter(); // Not used
			void GetResults(); // Not used
			void GetSelectedItems(); // Not used
		}

		[ComImport]
		[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IShellItem
		{
			void BindToHandler();
			void GetParent();
			[PreserveSig]
			int GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);
			void GetAttributes();
			void Compare();
		}

		private enum SIGDN : uint
		{
			SIGDN_FILESYSPATH = 0x80058000,
		}

		[Flags]
		private enum FOS : uint
		{
			FOS_OVERWRITEPROMPT = 0x00000002,
			FOS_STRICTFILETYPES = 0x00000004,
			FOS_NOCHANGEDIR = 0x00000008,
			FOS_PICKFOLDERS = 0x00000020,
			FOS_FORCEFILESYSTEM = 0x00000040,
			FOS_ALLNONSTORAGEITEMS = 0x00000080,
			FOS_NOVALIDATE = 0x00000100,
			FOS_ALLOWMULTISELECT = 0x00000200,
			FOS_PATHMUSTEXIST = 0x00000800,
			FOS_FILEMUSTEXIST = 0x00001000,
			FOS_CREATEPROMPT = 0x00002000,
			FOS_SHAREAWARE = 0x00004000,
			FOS_NOREADONLYRETURN = 0x00008000,
			FOS_NOTESTFILECREATE = 0x00010000,
			FOS_HIDEMRUPLACES = 0x00020000,
			FOS_HIDEPINNEDPLACES = 0x00040000,
			FOS_NODEREFERENCELINKS = 0x00100000,
			FOS_DONTADDTORECENT = 0x02000000,
			FOS_FORCESHOWHIDDEN = 0x10000000,
			FOS_DEFAULTNOMINIMODE = 0x20000000,
			FOS_FORCEPREVIEWPANEON = 0x40000000,
		}
	}
}
