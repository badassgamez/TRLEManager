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
            _trle = trle;
            _p = null;

			//if (!trle.UseGamepad)
			//	return;

            try
            {
				var gamepadInfo = App.GetGamepadInfo();
				if (gamepadInfo == null)
					return;

                var baseGamepad = gamepadInfo.ToGamepad();
				if (baseGamepad == null)
					return;

                _gamepad = new VirtualGamepad(baseGamepad);

                CompilePadToKeyMap();

                _gamepad.OnVirtualChanged += _gamepad_OnVirtualChanged;
            }
            catch (Error e)
			{
				e.LogError();
			}			
        }
			
		public void Start()
		{
			if (_p != null)
				return;

			TRLECollection.MakeFirst(_trle);

			_p = _trle.Play();
			_p.EnableRaisingEvents = true;
			_p.Exited += _p_Exited;

			_gamepad?.StartMonitor();

			if (_trle.RemoveWindowBorder)
			{
                _p.WaitForInputIdle();
				DisableWindowBorder();
            }
		}

		public void DisableWindowBorder()
		{

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

		private void _gamepad_OnVirtualChanged(object sender, VirtualGamepadChangedEventArgs eventArgs)
		{
			try
			{
				if (!IsForeground())
					return;

				for (int i = 0; i < eventArgs.changeReportReleventLength; i++)
				{
					ref ButtonReport buttonReport = ref eventArgs.changeReport[i];

					SendKey(_vpadToKey[(int)buttonReport.button], buttonReport.down);
				}
			}
			catch (Error e)
			{
				e.LogError();
			}
		}

		private void _p_Exited(object sender, EventArgs e)
		{
			_p = null;
			_gamepad?.StopMonitor();

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
			if (hwnd == IntPtr.Zero) return false;

			if (0 == GetWindowThreadProcessId(hwnd, out int processId))
				return false;
			
			return _p.Id == processId;
		}

		static INPUT[] _inputBuffer = new INPUT[1];
		static readonly int _sizeOfInput = Marshal.SizeOf(typeof(INPUT));

		static void SendKey(ushort sc, bool keyDown)
		{
			uint flags = KEYEVENTF_SCANCODE + KEYEVENTF_EXTENDEDKEY * Convert.ToUInt32(sc > 0xFF) + KEYEVENTF_KEYUP * Convert.ToUInt32(!keyDown);

			_inputBuffer[0].type = INPUT_KEYBOARD;
			_inputBuffer[0].u.ki.wVk = 0;
			_inputBuffer[0].u.ki.wScan = sc;
			_inputBuffer[0].u.ki.dwFlags = flags;
			_inputBuffer[0].u.ki.time = 0;
			_inputBuffer[0].u.ki.dwExtraInfo = IntPtr.Zero;

			if (1 != SendInput(1, _inputBuffer, _sizeOfInput))
			{
				var err = new Error($"Failed to send input.", new Win32Exception(Marshal.GetLastWin32Error()));
				err.Data.Add("sc", sc);
				err.Data.Add("keyDown", keyDown);
				throw err;
			}
		}
	}
}
