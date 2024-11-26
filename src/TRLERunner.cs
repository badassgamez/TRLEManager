/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.21.2024

	License: MIT
*/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Input;

namespace TRLEManager
{
	public class TRLERunner
	{
		VirtualGamepad _gamepad;
		TRLE _trle;
		Process _p;
		ushort[] _vpadToKey;

		public event EventHandler OnClosed;

		public TRLE TRLE
		{
			get
			{
				return _trle;
			}
		}

		public TRLERunner(TRLE trle) {
			_gamepad = new VirtualGamepad(App.GetGamepadInfo().ToGamepad());
			_trle = trle;
			_p = null;

			CompilePadToKeyMap();

            _gamepad.OnVirtualChanged += _gamepad_OnVirtualChanged;
        }
			
		public void Start()
		{
			if (_p != null)
				return;

			TRLECollection.MakeFirst(_trle);

			_p = _trle.Play();
			_p.EnableRaisingEvents = true;
			_p.Exited += _p_Exited;

			_gamepad.StartMonitor();
		}

		public void CompilePadToKeyMap()
		{
			var _virtualMap = App.GetVirtualGamepadMapping();
			var _keyboardMap = App.GetKeyboardMapping();

			var virtualButtonCount = Enum.GetValues(typeof(VirtualGamepadButton)).Length;
			_vpadToKey = new ushort[virtualButtonCount];

			for (int i = 0; i < virtualButtonCount; i++)
			{
				Key lookup;
				bool found = _keyboardMap.TryGetValue(_virtualMap[(VirtualGamepadButton)i], out lookup);

				_vpadToKey[i] = found ? KeyToScanCode.Lookup[lookup] : (ushort)0;
			}
		}

		private void _gamepad_OnVirtualChanged(object sender, VirtualGamepadChangedEventArgs e)
		{
			if (!IsForeground())
				return;

			for (int i = 0; i < e.changeReportReleventLength; i++)
			{
				ref ButtonReport buttonReport = ref e.changeReport[i];
				
				SendKey(_vpadToKey[(int)buttonReport.button], buttonReport.down);
			}
		}

		private void _p_Exited(object sender, EventArgs e)
		{
			_p = null;
			_gamepad.StopMonitor();

			OnClosed?.Invoke(this, null);
		}

		[StructLayout(LayoutKind.Sequential)]
		struct INPUT
		{
			public int type;
			public InputUnion u;
		}

		[StructLayout(LayoutKind.Explicit)]
		struct InputUnion
		{
			[FieldOffset(0)]
			public MOUSEINPUT mi;
			[FieldOffset(0)]
			public KEYBDINPUT ki;
			[FieldOffset(0)]
			public HARDWAREINPUT hi;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct KEYBDINPUT
		{
			public ushort wVk;
			public ushort wScan;
			public uint dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct MOUSEINPUT
		{
			public int dx;
			public int dy;
			public uint mouseData;
			public uint dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct HARDWAREINPUT
		{
			public uint uMsg;
			public ushort wParamL;
			public ushort wParamH;
		}

		const int INPUT_KEYBOARD = 1;
		const uint KEYEVENTF_KEYUP = 0x0002;
		const uint KEYEVENTF_SCANCODE = 0x0008;
		const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

		[DllImport("user32.dll", SetLastError = true)]
		static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

		[DllImport("user32.dll", SetLastError = true)]
		static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		bool IsForeground()
		{
			IntPtr hwnd = GetForegroundWindow();
			if (0 == GetWindowThreadProcessId(hwnd, out int processId))
			{
				Debug.WriteLine($"An error occurred trying to receive the active window's process id. '{new Win32Exception().Message}'");
			}

			return _p.Id == processId;
		}

		static INPUT[] _inputBuffer = new INPUT[1];
		static readonly int _sizeOfInput = Marshal.SizeOf(typeof(INPUT));

		static void SendKey(ushort sc, bool keyDown)
		{
			uint flags = KEYEVENTF_SCANCODE + KEYEVENTF_EXTENDEDKEY * Convert.ToUInt32(sc > 0xFF) + KEYEVENTF_KEYUP * Convert.ToUInt32(!keyDown);

			//INPUT input;
			_inputBuffer[0].type = INPUT_KEYBOARD;
			_inputBuffer[0].u.ki.wVk = 0;
			_inputBuffer[0].u.ki.wScan = sc;
			_inputBuffer[0].u.ki.dwFlags = flags;
			_inputBuffer[0].u.ki.time = 0;
			_inputBuffer[0].u.ki.dwExtraInfo = IntPtr.Zero;

			if (1 != SendInput(1, _inputBuffer, _sizeOfInput))
			{
				var lastErr = Marshal.GetLastWin32Error();
				Debug.WriteLine($"Failed to send up input {sc.ToString()} ({lastErr}) '{new Win32Exception(lastErr)}'");
				return;
			}
			//}
			//else
			//{
			//	_inputBuffer[0].type = INPUT_KEYBOARD;
			//	_inputBuffer[0].u.ki.wVk = (ushort)tvk;
			//	_inputBuffer[0].u.ki.wScan = (ushort)scan;
			//	_inputBuffer[0].u.ki.dwFlags = (uint)flags;
			//	_inputBuffer[0].u.ki.time = 0;
			//	_inputBuffer[0].u.ki.dwExtraInfo = IntPtr.Zero;

			//	if (1 != SendInput(1, _inputBuffer, _sizeOfInput))
			//	{
			//		var lastErr = Marshal.GetLastWin32Error();
			//		Debug.WriteLine($"Failed to send up input {sc.ToString()} ({lastErr}) '{new Win32Exception(lastErr)}'");
			//		return;
			//	}
			//}
		}
	}
}
