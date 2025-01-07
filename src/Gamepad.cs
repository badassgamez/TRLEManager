/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.17.2024

	License: MIT
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;
using System.Collections;
using System.CodeDom;
using static TRLEManager.Gamepad;

namespace TRLEManager
{
    public class GamepadChangedEventArgs : EventArgs
    {
		public ushort[] buttonChanges;
		public ushort buttonChangeCount;
		public ushort prevDpad;
    }

    public class Gamepad : IDisposable
	{
		private IntPtr _hDevice;
		private IntPtr _hIO;
		private IntPtr _preparsedData;
		private HIDP_CAPS _caps;
		private HIDP_BUTTON_CAPS[] _buttonCaps;
		private HIDP_VALUE_CAPS[] _valueCaps;

		private SynchronizationContext _syncContext;
		private Thread _monitoringThread;

		private volatile BitArray _buttonStates;
		private volatile uint _hatSwitchValue;
		private volatile bool _threadRunningFlag;

		private bool _disposed;

		public event EventHandler<GamepadChangedEventArgs> OnGamepadChanged;

		public uint HatSwitchValue { get { return _hatSwitchValue; } }

		public BitArray ButtonValues
		{
			get
			{
				return _buttonStates;
			}
		}

		public void StartMonitor()
		{
			if (_monitoringThread != null) return;
			_monitoringThread = new Thread(MonitorThread);

			_threadRunningFlag = true;

			_syncContext = SynchronizationContext.Current;

			try
			{
				var bcap = GetButtonCapabilities().First(cap => cap.UsagePage == USAGEPAGE_BUTTONS);
				_buttonStates = new BitArray(bcap.Union.Range.UsageMax - bcap.Union.Range.UsageMin + 1);
			}
			catch (Exception e) when (
				e is ArgumentNullException
				|| e is InvalidOperationException
				|| e is ArgumentOutOfRangeException)
			{
				throw new Error("Unable to start monitoring a gamepad.", e);
			}

			_monitoringThread.Start();
		}

		public void StopMonitor()
		{
			_threadRunningFlag = false;

			CancelIoEx(_hIO, IntPtr.Zero);
		}

		private void MonitorThread()
		{
            try
			{
                IntPtr inputReport;
                try
                {
                    inputReport = Marshal.AllocHGlobal(GetGamepadCapabilities().InputReportByteLength);
                }
                catch (OutOfMemoryException e)
                {
                    throw new Error("Out of memorty to begin monitoring gamepad.", e);
                }

				try
				{
					var bcap = GetButtonCapabilities().First(c => c.UsagePage == USAGEPAGE_BUTTONS);
					var hcap = GetValueCapabilities().First(c => c.UsagePage == USAGEPAGE_GENERIC_DESKTOP && c.Union.NotRange.Usage == USAGE_HATSWITCH);

					ushort[] usages = new ushort[bcap.Union.Range.UsageMax - bcap.Union.Range.UsageMin + 1];
					GamepadChangedEventArgs eventArgs = new GamepadChangedEventArgs();
					eventArgs.buttonChanges = new ushort[usages.Length];

					BitArray buttonBuffer = new BitArray(_buttonStates);

					while (_threadRunningFlag)
					{
						if (!ReadFile(_hIO, inputReport, _caps.InputReportByteLength, out uint bytesRead, IntPtr.Zero))
						{
							int lasterr = Marshal.GetLastWin32Error();
							if (lasterr != ERROR_OPERATION_ABORTED)
								throw new Error($"Failed to read from the device.", new Win32Exception(Marshal.GetLastWin32Error()));

							break;
						}

						uint ntresult = HidP_GetUsageValue(0, USAGEPAGE_GENERIC_DESKTOP, 0, USAGE_HATSWITCH, out uint povValue, _preparsedData, inputReport, _caps.InputReportByteLength);
						if (ntresult != HIDP_STATUS_SUCCESS && ntresult != HIDP_STATUS_USAGE_NOT_FOUND)
							throw new Error($"Failed read value from the POV Hat Switch. '{HidResultToErrorString(ntresult)}'");

						// adjust povValue
						if (povValue < hcap.LogicalMin || povValue > hcap.LogicalMax)
							povValue = 0; // idle
						else
						{
							povValue = povValue - (uint)hcap.LogicalMin + 1; // normalize;
							int adjustedPov = 0;
							adjustedPov = 0x1 * Convert.ToUInt16(povValue == 8 || povValue == 1 || povValue == 2);
							adjustedPov += 0x2 * Convert.ToUInt16(povValue == 2 || povValue == 3 || povValue == 4);
							adjustedPov += 0x4 * Convert.ToUInt16(povValue == 4 || povValue == 5 || povValue == 6);
							adjustedPov += 0x8 * Convert.ToUInt16(povValue == 6 || povValue == 7 || povValue == 8);

							povValue = (uint)adjustedPov;
						}

						uint usageLen = (uint)usages.Length;

						ntresult = HidP_GetUsages(0, USAGEPAGE_BUTTONS, 0, usages, ref usageLen, _preparsedData, inputReport, _caps.InputReportByteLength);
						if (ntresult != HIDP_STATUS_SUCCESS)
							throw new Error($"An error occured attempting to check number of buttons down '{HidResultToErrorString(ntresult)}'");

						bool raiseEvent = false;
						eventArgs.buttonChangeCount = 0;

						var buttonCount = usages.Length;

						if (usageLen == 0)
						{
							for (ushort i = 0; i < buttonCount; i++)
							{
								if (_buttonStates[i] != false)
								{
									eventArgs.buttonChanges[eventArgs.buttonChangeCount++] = i;
									raiseEvent = true;
								}
							}

							if (raiseEvent)
								_buttonStates.SetAll(false);
						}
						else
						{
							buttonBuffer.SetAll(false);

							for (ushort i = 0; i < usageLen; i++)
							{   // Set used button flags to true
								int buttonIndex = usages[i] - bcap.Union.Range.UsageMin;
								buttonBuffer[buttonIndex] = true;
							}

							for (ushort i = 0; i < buttonCount; i++)
							{   // test for difference in buttons (buffer contains the new state, states contains stale state)
								if (buttonBuffer[i] != _buttonStates[i])
								{
									eventArgs.buttonChanges[eventArgs.buttonChangeCount++] = i;
									raiseEvent = true;
								}
							}

							if (raiseEvent)
								(buttonBuffer, _buttonStates) = (_buttonStates, buttonBuffer);
						}

						eventArgs.prevDpad = (ushort)_hatSwitchValue;
						raiseEvent |= _hatSwitchValue != povValue;
						_hatSwitchValue = povValue;

						if (raiseEvent)
						{
							_syncContext.Send((_) =>
							{
								try
								{
									OnGamepadChanged?.Invoke(this, eventArgs);
								}
								catch (Exception e)
								{
									Error.LogException(e);
								}
							}, null);
						}
						//Thread.Sleep(1);
					} // while
				}
				finally
				{
                    Marshal.FreeHGlobal(inputReport);
                }
			} // try
			catch (Exception e)
			{
				Error.LogException(e);
				if (!(e is Error)) throw;
			}
			finally
			{
				CloseHandle(_hIO);
                _threadRunningFlag = false;
            }
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			_threadRunningFlag = false;

			if (_hIO != IntPtr.Zero)
			{
				CloseHandle(_hIO);
				_hIO = IntPtr.Zero;
			}

			if (_preparsedData != IntPtr.Zero)
			{
				HidD_FreePreparsedData(_preparsedData);
				_preparsedData = IntPtr.Zero;
			}
		}
		
		public class Devices
		{
			private IntPtr[] _deviceHandles = null;

			public static Devices GetDevices()
			{
				return new Devices()
				{
					_deviceHandles = GetRawInputDevices().Aggregate(new List<IntPtr>(), (list, dev) =>
					{
						if (dev.dwType != RIM_TYPEHID)
							return list;

						var devInfo = GetHidDeviceInfo(dev.hDevice);
						if (devInfo.hid.usUsagePage == USAGEPAGE_GENERIC_DESKTOP && (devInfo.hid.usUsage == USAGEPAGE_SPORT_CONTROLS || devInfo.hid.usUsage == USAGEPAGE_GAME_CONTROLS))
							list.Add(dev.hDevice);

						return list;
					}).ToArray()
				};
            }

            private Devices() { }

			public int DeviceCount
			{
				get
				{
					return _deviceHandles.Length;
				}
			}

			public IntPtr GetHandle(int index)
			{
                if (index >= _deviceHandles.Length)
                {
                    var err = new Error("Index out of range.");
                    err.Data.Add("Index to Access", index);
                    err.Data.Add("_deviceHandles Length", _deviceHandles.Length);
                    throw err;
                }

                return _deviceHandles[index];
			}

			public List<GamepadInfo> GetGamepads()
			{
				return _deviceHandles.Aggregate(new List<GamepadInfo>(), (result, handle) =>
				{
					IntPtr hFile = OpenDevice(handle);
					
					try
					{
						var gpi = GetInfoFromIO(hFile);

						result.Add(gpi);
						return result;
					}
					finally
					{
						CloseHandle(hFile);
					}
				});
			}


			private static GamepadInfo GetInfoFromIO(IntPtr hIO)
			{
				return new GamepadInfo()
				{
					VendorName = GetDeviceManufacturerString(hIO),
					ProductName = GetDeviceProductName(hIO),
					// SerialNumber = GetDeviceSerialNumberString(hIO)
				};
			}
			
			public GamepadInfo GetInfo(int index)
			{
				IntPtr hIO = OpenDevice(GetHandle(index));

				try
				{
					return GetInfoFromIO(hIO);
				}
				finally
				{
					 CloseHandle(hIO);
				}
			}

			private static IntPtr OpenDevice(IntPtr hDevice)
			{
				string devName = GetDeviceName(hDevice);

				IntPtr hFile = CreateFileW(devName, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
				if (hFile.ToInt64() != -1)
					return hFile;

				var err = new Error($"Unable to open a device.", new Win32Exception(Marshal.GetLastWin32Error()));
				err.Data.Add("devName", devName);
				throw err;
			}
		}

		public Gamepad(int deviceIndex = 0)
		{
			_hDevice = Devices.GetDevices().GetHandle(deviceIndex);
		}

		public uint GetButtonCount(bool fresh = false)
		{
			var bcap = GetButtonCapabilities().First(caps => caps.UsagePage == USAGEPAGE_BUTTONS);
			return (uint)bcap.Union.Range.UsageMax - bcap.Union.Range.UsageMin + 1;
		}

		private IntPtr GetIOHandle(bool fresh = false)
		{
			if (_hIO != IntPtr.Zero)
			{
				if (!fresh) return _hIO;
				else CloseHandle(_hIO);
			}

			_hIO = CreateFileW(GetDeviceName(_hDevice), FileAccess.Read, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);
			if (_hIO.ToInt64() == -1)
				throw new Error("Unable to open the device", new Win32Exception(Marshal.GetLastWin32Error()));

			return _hIO;
		}

		private IntPtr GetPreparsedData(bool fresh = false)
		{
			if (_preparsedData != IntPtr.Zero && !fresh) return _preparsedData;

			if (!HidD_GetPreparsedData(GetIOHandle(fresh), out _preparsedData))
				throw new Error("Unable to get device's preparsed data", new Win32Exception(Marshal.GetLastWin32Error()));

			return _preparsedData;
		}

		private ref readonly HIDP_CAPS GetGamepadCapabilities(bool fresh = false)
		{
			if (_caps.Usage == USAGEPAGE_GAME_CONTROLS) return ref _caps;

			uint r = HidP_GetCaps(GetPreparsedData(fresh), out _caps);
			if (r == HIDP_STATUS_SUCCESS) return ref _caps;

			throw new Error($"Unable to get device capabilities '{HidResultToErrorString(r)}'");
		}

		private IReadOnlyList<HIDP_BUTTON_CAPS> GetButtonCapabilities(bool fresh = false)
		{
			if (_buttonCaps != null && !fresh) return _buttonCaps;

			ushort numberInputButtonCaps = GetGamepadCapabilities(fresh).NumberInputButtonCaps;
			_buttonCaps = new HIDP_BUTTON_CAPS[numberInputButtonCaps];

			ushort numberInputButtonCapsReturned = numberInputButtonCaps;
			uint r = HidP_GetButtonCaps(0, _buttonCaps, ref numberInputButtonCapsReturned, _preparsedData);
			if (r != HIDP_STATUS_SUCCESS)
			{
				_buttonCaps = null;
				throw new Exception($"Unable to get device button capabilities - '{HidResultToErrorString(r)}'");
			}

			if (numberInputButtonCapsReturned != numberInputButtonCaps)
				new Error($"Only acquired {numberInputButtonCapsReturned} button capabilities out of {numberInputButtonCaps}").LogError();

			return _buttonCaps;
		}

		private IReadOnlyList<HIDP_VALUE_CAPS> GetValueCapabilities(bool fresh = false)
		{
			if (_valueCaps != null && !fresh) return _valueCaps;

			ushort numberValueCaps = GetGamepadCapabilities(fresh).NumberInputValueCaps;

			_valueCaps = new HIDP_VALUE_CAPS[numberValueCaps];
			ushort numberInputValueCapsReturned = numberValueCaps;

			uint r = HidP_GetValueCaps(0, _valueCaps, ref numberInputValueCapsReturned, _preparsedData);
			if (r != HIDP_STATUS_SUCCESS)
			{
				_valueCaps = null;
				throw new Error($"Unable to get device value capabilities - '{HidResultToErrorString(r)}'");
			}

			if (numberInputValueCapsReturned != numberValueCaps)
				new Error($"Only acquired {numberInputValueCapsReturned} value capabilities out of {numberValueCaps}").LogError();

			return _valueCaps;
		}

		private const int RID_HEADER = 0x10000005;
		private const int RID_INPUT = 0x10000003;
		private const int RIM_TYPEHID = 2;
		private const uint RIDEV_INPUTSINK = 0x00000100;
		private const uint RIDEV_EXINPUTSINK = 0x00001000;

		
		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTDEVICE
		{
			public ushort UsagePage;
			public ushort Usage;
			public uint Flags;
			public IntPtr Target;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTHEADER
		{
			public uint dwType;
			public uint dwSize;
			public IntPtr hDevice;
			public IntPtr wParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUT
		{
			public RAWINPUTHEADER header;
			public RAWHID hid;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWHID
		{
			public uint dwSizeHid;
			public uint dwCount;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public byte[] bRawData;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct OVERLAPPED
		{
			public IntPtr Internal;             // ULONG_PTR Internal
			public IntPtr InternalHigh;         // ULONG_PTR InternalHigh

			// Anonymous union: Either Offset/OffsetHigh or Pointer
			public UnionOffsetPointer DUMMYUNIONNAME;

			public IntPtr hEvent;               // HANDLE hEvent

			[StructLayout(LayoutKind.Explicit)]
			public struct UnionOffsetPointer
			{
				[FieldOffset(0)]
				public OffsetStruct Offset;     // DWORD Offset and OffsetHigh

				[FieldOffset(0)]
				public IntPtr Pointer;          // PVOID Pointer
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct OffsetStruct
			{
				public uint Offset;             // DWORD Offset
				public uint OffsetHigh;         // DWORD OffsetHigh
			}
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetRawInputDeviceList([Out] RAWINPUTDEVICELIST[] pRawInputDeviceList, [In, Out] ref uint puiNumDevices, uint cbSize);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern int GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref int pcbSize);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr CreateFileW(
			[MarshalAs(UnmanagedType.LPWStr)] string filename,
			[MarshalAs(UnmanagedType.U4)] FileAccess access,
			[MarshalAs(UnmanagedType.U4)] FileShare share,
			IntPtr securityAttributes,
			[MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
			[MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
			IntPtr templateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool ReadFile(IntPtr hFile, IntPtr lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CancelIoEx(IntPtr hFile, IntPtr lpOverlapped);


		[DllImport("kernel32.dll", SetLastError = true)]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[SuppressUnmanagedCodeSecurity]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool CloseHandle(IntPtr hObject);


		private const uint RIDI_DEVICENAME = 0x20000007;
		private const uint RIDI_DEVICEINFO = 0x2000000b;

		[StructLayout(LayoutKind.Sequential)]
		private struct RAWINPUTDEVICELIST
		{
			public IntPtr hDevice; // Handle to the raw input device
			public uint dwType;    // Type of device
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RID_DEVICE_INFO
		{
			public uint cbSize;
			public uint dwType;

			// Union for different device types. Only HID type is defined here.
			public RID_DEVICE_INFO_HID hid;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RID_DEVICE_INFO_HID
		{
			public uint dwVendorId;
			public uint dwProductId;
			public uint dwVersionNumber;
			public ushort usUsagePage;
			public ushort usUsage;
		}

		private const uint ERROR_OPERATION_ABORTED = 0x3E3;
		
		private const uint HIDP_STATUS_SUCCESS = 0x00110000;
		private const uint HIDP_STATUS_INVALID_PREPARSED_DATA = 0xC0110001;
		private const uint HIDP_STATUS_USAGE_NOT_FOUND = 0xC0110004;
		private const uint HIDP_STATUS_BUFFER_TOO_SMALL = 0xC0110007;

		private const ushort USAGEPAGE_GENERIC_DESKTOP = 0x1;
		private const ushort USAGEPAGE_SPORT_CONTROLS = 0x4;
		private const ushort USAGEPAGE_GAME_CONTROLS = 0x5;
		private const ushort USAGEPAGE_BUTTONS = 0x9;

		private const ushort USAGE_X = 0x30;
		private const ushort USAGE_Y = 0x31;
		private const ushort USAGE_Z = 0x32;
		private const ushort USAGE_Rx = 0x33;
		private const ushort USAGE_Ry = 0x34;
		private const ushort USAGE_Rz = 0x35;
		private const ushort USAGE_SLIDER = 0x36;
		private const ushort USAGE_DIAL = 0x37;
		private const ushort USAGE_WHEEL = 0x38;
		private const ushort USAGE_HATSWITCH = 0x39;

		// Import functions from hid.dll
		[DllImport("hid.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool HidD_GetPreparsedData(IntPtr hDevice, out IntPtr preparsedData);

		[DllImport("hid.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool HidD_FreePreparsedData(IntPtr preparsedData);

		[DllImport("hid.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.I4)]
		private static extern uint HidP_GetCaps(IntPtr preparsedData, out HIDP_CAPS caps);

		[DllImport("hid.dll", SetLastError = true)]
		private static extern int HidP_GetData(ushort ReportType, [Out] HIDP_DATA[] DataList, ref uint DataLength, IntPtr PreparsedData, IntPtr Report, uint ReportLength);

		[DllImport("hid.dll", SetLastError = true)]
		private static extern uint HidP_GetButtonCaps(ushort ReportType, [Out] HIDP_BUTTON_CAPS[] ButtonCaps, ref ushort ButtonCapsLength, IntPtr PreparsedData);

		[DllImport("hid.dll", SetLastError = true)]
		private static extern uint HidP_GetValueCaps(ushort ReportType, [Out] HIDP_VALUE_CAPS[] ValueCaps, ref ushort ValueCapsLength, IntPtr PreparsedData);

		[DllImport("hid.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool HidD_GetInputReport(IntPtr HidDeviceObject, IntPtr lpReportBuffer, uint ReportBufferLength);

		[DllImport("hid.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool HidD_GetManufacturerString(IntPtr HidDeviceObject, IntPtr Buffer, int BufferLength);

		[DllImport("hid.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool HidD_GetProductString(IntPtr HidDeviceObject, IntPtr Buffer, int BufferLength);

		//[DllImport("hid.dll", SetLastError = true)]
		//public static extern bool HidD_GetSerialNumberString(IntPtr HidDeviceObject, IntPtr Buffer, int BufferLength);

		[DllImport("hid.dll", SetLastError = true)]
		private static extern uint HidP_GetUsageValue(ushort ReportType, ushort UsagePage, ushort LinkCollection, ushort Usage, out uint UsageValue, IntPtr PreparsedData, IntPtr Report, uint ReportLength);

		[DllImport("hid.dll", SetLastError = true)]
		private static extern uint HidP_GetUsages(ushort ReportType, ushort UsagePage, ushort LinkCollection, [Out] ushort[] UsageList, ref uint UsageLength, IntPtr PreparsedData, IntPtr Report, uint ReportLength);

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_CAPS
		{
			public ushort Usage;
			public ushort UsagePage;
			public ushort InputReportByteLength;
			public ushort OutputReportByteLength;
			public ushort FeatureReportByteLength;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
			public ushort[] Reserved;

			public ushort NumberLinkCollectionNodes;
			public ushort NumberInputButtonCaps;
			public ushort NumberInputValueCaps;
			public ushort NumberInputDataIndices;
			public ushort NumberOutputButtonCaps;
			public ushort NumberOutputValueCaps;
			public ushort NumberOutputDataIndices;
			public ushort NumberFeatureButtonCaps;
			public ushort NumberFeatureValueCaps;
			public ushort NumberFeatureDataIndices;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_BUTTON_CAPS
		{
			public ushort UsagePage; // USAGE corresponds to ushort
			public byte ReportID;    // UCHAR corresponds to byte
			[MarshalAs(UnmanagedType.I1)]
			public bool IsAlias;     // BOOLEAN corresponds to bool with I1 for 1-byte size
			public ushort BitField;
			public ushort LinkCollection;
			public ushort LinkUsage;      // USAGE corresponds to ushort
			public ushort LinkUsagePage;  // USAGE corresponds to ushort
			[MarshalAs(UnmanagedType.I1)]
			public bool IsRange;
			[MarshalAs(UnmanagedType.I1)]
			public bool IsStringRange;
			[MarshalAs(UnmanagedType.I1)]
			public bool IsDesignatorRange;
			[MarshalAs(UnmanagedType.I1)]
			public bool IsAbsolute;
			public ushort ReportCount;
			public ushort Reserved2;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
			public uint[] Reserved;

			// Union field
			public HIDP_BUTTON_CAPS_UNION Union;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct HIDP_BUTTON_CAPS_UNION
		{
			[FieldOffset(0)]
			public HIDP_BUTTON_CAPS_RANGE Range;

			[FieldOffset(0)]
			public HIDP_BUTTON_CAPS_NOTRANGE NotRange;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_BUTTON_CAPS_RANGE
		{
			public ushort UsageMin;       // USAGE corresponds to ushort
			public ushort UsageMax;       // USAGE corresponds to ushort
			public ushort StringMin;
			public ushort StringMax;
			public ushort DesignatorMin;
			public ushort DesignatorMax;
			public ushort DataIndexMin;
			public ushort DataIndexMax;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_BUTTON_CAPS_NOTRANGE
		{
			public ushort Usage;          // USAGE corresponds to ushort
			public ushort Reserved1;      // USAGE corresponds to ushort
			public ushort StringIndex;
			public ushort Reserved2;
			public ushort DesignatorIndex;
			public ushort Reserved3;
			public ushort DataIndex;
			public ushort Reserved4;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_VALUE_CAPS
		{
			public ushort UsagePage;  // USAGE corresponds to ushort
			public byte ReportID;     // UCHAR corresponds to byte
			[MarshalAs(UnmanagedType.I1)]
			public bool IsAlias;      // BOOLEAN corresponds to 1-byte bool
			public ushort BitField;
			public ushort LinkCollection;
			public ushort LinkUsage;      // USAGE corresponds to ushort
			public ushort LinkUsagePage;  // USAGE corresponds to ushort
			[MarshalAs(UnmanagedType.I1)]
			public bool IsRange;
			[MarshalAs(UnmanagedType.I1)]
			public bool IsStringRange;
			[MarshalAs(UnmanagedType.I1)]
			public bool IsDesignatorRange;
			[MarshalAs(UnmanagedType.I1)]
			public bool IsAbsolute;
			[MarshalAs(UnmanagedType.I1)]
			public bool HasNull;
			public byte Reserved;
			public ushort BitSize;
			public ushort ReportCount;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public ushort[] Reserved2;

			public uint UnitsExp;
			public uint Units;
			public int LogicalMin;    // LONG corresponds to int
			public int LogicalMax;    // LONG corresponds to int
			public int PhysicalMin;   // LONG corresponds to int
			public int PhysicalMax;   // LONG corresponds to int

			public HIDP_VALUE_CAPS_UNION Union;  // Union field
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct HIDP_VALUE_CAPS_UNION
		{
			[FieldOffset(0)]
			public HIDP_VALUE_CAPS_RANGE Range;

			[FieldOffset(0)]
			public HIDP_VALUE_CAPS_NOTRANGE NotRange;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_VALUE_CAPS_RANGE
		{
			public ushort UsageMin;
			public ushort UsageMax;
			public ushort StringMin;
			public ushort StringMax;
			public ushort DesignatorMin;
			public ushort DesignatorMax;
			public ushort DataIndexMin;
			public ushort DataIndexMax;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_VALUE_CAPS_NOTRANGE
		{
			public ushort Usage;
			public ushort Reserved1;
			public ushort StringIndex;
			public ushort Reserved2;
			public ushort DesignatorIndex;
			public ushort Reserved3;
			public ushort DataIndex;
			public ushort Reserved4;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct HIDP_DATA
		{
			public ushort DataIndex;  // Corresponds to USHORT
			public ushort Reserved;   // Corresponds to USHORT

			public HIDP_DATA_UNION Data;  // Union field
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct HIDP_DATA_UNION
		{
			[FieldOffset(0)]
			public uint RawValue;  // Corresponds to ULONG

			[FieldOffset(0), MarshalAs(UnmanagedType.I1)]
			public bool On;  // Corresponds to BOOLEAN (1 byte)
		}

		private static RAWINPUTDEVICELIST[] GetRawInputDevices()
		{
			uint deviceCount = 0;

			if (GetRawInputDeviceList(null, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST))) != 0)
				throw new Error("Failed to get raw input device list size.", new Win32Exception(Marshal.GetLastWin32Error()));

			RAWINPUTDEVICELIST[] rawInputDeviceList = new RAWINPUTDEVICELIST[deviceCount];

			if (GetRawInputDeviceList(rawInputDeviceList, ref deviceCount, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST))) == -1)
                throw new Error("Failed to get raw input device list.", new Win32Exception(Marshal.GetLastWin32Error()));

            return rawInputDeviceList;
		}

		private static RID_DEVICE_INFO GetHidDeviceInfo(IntPtr hDevice)
		{
			int size = 0;

			if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICEINFO, IntPtr.Zero, ref size) == -1 || size == 0)
				throw new Error("Failed to get device info size.", new Win32Exception(Marshal.GetLastWin32Error()));

			var deviceInfo = new RID_DEVICE_INFO
			{
				cbSize = (uint)size
			};

			IntPtr buffer = Marshal.AllocHGlobal(size);

			try
			{
				// RID_DEVICE_INFO must be initialized with it's structure size before passing to GetRawInputDeviceInfo

				try
				{
					Marshal.StructureToPtr(deviceInfo, buffer, false);
				}
				catch (ArgumentException e)
				{
					throw new Error("Failed to copy structure to buffer.", e);
				}

				if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICEINFO, buffer, ref size) == -1)
					throw new Error("Failed to get raw input device info.", new Win32Exception(Marshal.GetLastWin32Error()));

				try
				{
					deviceInfo = Marshal.PtrToStructure<RID_DEVICE_INFO>(buffer);
				}
				catch (Exception e) when (
					e is ArgumentException
					|| e is MissingMethodException)
				{
					throw new Error("Failed to copy from buffer to structure.", e);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}

			if (deviceInfo.dwType != RIM_TYPEHID)
			{
				var err = new Error("The device is not a HID device.");
				err.Data.Add("deviceInfo.dwType", deviceInfo.dwType);
				throw err;
			}

			return deviceInfo;
        }

		private static T WrapMarshalBuffer<T>(int size, Func<IntPtr, T> action)
		{
			IntPtr buffer;
			try
			{
				buffer = Marshal.AllocHGlobal(size);
			}
			catch (OutOfMemoryException e)
			{
				throw new Error("Out of memory. Unable to get raw input info.", e);
			}

			try
			{
				return action(buffer);
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		private static string GetDeviceName(IntPtr hDevice)
		{
			int chars = 0;

			if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref chars) != 0)
				throw new Error("Unable to get raw input info size.", new Win32Exception(Marshal.GetLastWin32Error()));

			IntPtr buffer = Marshal.AllocHGlobal(2 * (chars + 1)); // Unicode characters, null terminated

			string name = "";
			try
			{
				if (GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, buffer, ref chars) != chars)
					throw new Error("Unable to get raw input info.", new Win32Exception(Marshal.GetLastWin32Error()));

				name = Marshal.PtrToStringUni(buffer);
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}

			return name;
			
		}

		private static string GetDeviceManufacturerString(IntPtr hFile)
		{
			string result;

			int bufferSize = 1024;
			IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

			try
			{
				if (!HidD_GetManufacturerString(hFile, buffer, bufferSize))
					throw new Error("Unable to get device manufacturer string.", new Win32Exception(Marshal.GetLastWin32Error()));

				result = Marshal.PtrToStringUni(buffer);
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}

			return result;
			
		}

		private static string GetDeviceProductName(IntPtr hFile)
		{
			string result;

			int bufferSize = 1024;
			IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

			try
			{
				if (!HidD_GetProductString(hFile, buffer, bufferSize))
					throw new Error("Unable to get device product string.", new Win32Exception(Marshal.GetLastWin32Error()));

				result = Marshal.PtrToStringUni(buffer);
			}
			finally 
			{ 
				Marshal.FreeHGlobal(buffer); 
			}

			return result;
        }

		//private static string GetDeviceSerialNumberString(IntPtr hFile)
		//{
		//	int bufferSize = 1024;
		//	return WrapMarshalBuffer(bufferSize, buffer =>
		//	{
		//		if (!HidD_GetSerialNumberString(hFile, buffer, bufferSize))
		//			throw new Error("Unable to get device serial number.", new Win32Exception(Marshal.GetLastWin32Error()));

		//		return Marshal.PtrToStringUni(buffer);
		//	});
		//}

		private static string HidResultToErrorString(uint r)
		{
			switch (r)
			{
				case HIDP_STATUS_SUCCESS: return "Success";
				case HIDP_STATUS_INVALID_PREPARSED_DATA: return "Invalid Preparsed Data";
				case HIDP_STATUS_BUFFER_TOO_SMALL: return "Buffer Too Small";
				default: return $"0x{r.ToString("x")} - Unknown Error";
			}
		}
	}
}