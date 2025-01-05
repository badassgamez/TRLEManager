/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.21.2024

	License: MIT
*/

using System;
using System.Linq;
using System.Text;

namespace TRLEManager
{
	public class GamepadInfo
	{
		public string VendorName { get; set; }
		public string ProductName { get; set; }
		// public string SerialNumber { get; set; }

		
		public override string ToString()
		{
			return $"{VendorName} - {ProductName}";
		}

		public string Serialize()
		{
			StringBuilder serialized = new StringBuilder();

			serialized.Append($"VendorName={VendorName}\n");
			serialized.Append($"ProductName={ProductName}\n");
			//serialized.Append($"SerialNumber={SerialNumber}\n");

			return App.ToBase64(serialized.ToString());
		}

		public bool IsConnected()
		{
			return null != Gamepad.Devices.GetDevices().GetGamepads().FirstOrDefault(info => 
				info.VendorName == VendorName
				&& info.ProductName == ProductName
				//&& info.SerialNumber == SerialNumber
				);
		}

		public static GamepadInfo Deserialize(string serial)
		{
			string[] split = serial.Split('\n');

			return split.Aggregate(new GamepadInfo(), (info, line) =>
			{
				if (string.IsNullOrEmpty(line))
					return info;

				int pos = line.IndexOf('=');
				if (pos == -1)
				{
					var err = new Error("A gamepad info store entry is malformed.");
                    err.Data.Add("line", line);
                    err.Data.Add("serial", serial);
					return info;
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
					var err = new Error("Failed to parse gamepad info.", e);
					err.Data.Add("line", line);
					err.Data.Add("pos", pos);
					throw err;
				}

				switch (key)
				{
					case "VendorName": info.VendorName = val; break;
					case "ProductName": info.ProductName = val; break;
					//case "SerialNumber": info.SerialNumber = val; break;
					default: break;
				}

				return info;
			});
		}

		public Gamepad ToGamepad()
		{
			var devs = Gamepad.Devices.GetDevices();
			if (devs.DeviceCount == 0)
			{
				var err = new Error("Failed to locate a previous gamepad.");
				err.Data.Add("device count", devs.DeviceCount);
				err.Data.Add("VendorName", VendorName);
				err.Data.Add("ProductName", ProductName);
				//err.Data.Add("SerialNumber", SerialNumber);
				throw err;
			}

			for (int i = 0; i < devs.DeviceCount; i++)
			{
				var dev = devs.GetInfo(i);

				if (dev.VendorName == VendorName 
					&& dev.ProductName == ProductName 
					//&& dev.SerialNumber == SerialNumber
					)
					return new Gamepad(i);
			}

			return new Gamepad();
		}
	}
}
