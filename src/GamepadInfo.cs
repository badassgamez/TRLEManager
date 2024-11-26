/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.21.2024

	License: MIT
*/

using System.Linq;
using System.Text;

namespace TRLEManager
{
	public class GamepadInfo
	{
		public string VendorName { get; set; }
		public string ProductName { get; set; }
		public string SerialNumber { get; set; }

		
		public override string ToString()
		{
			return $"{VendorName} - {ProductName}";
		}

		public string Serialize()
		{
			StringBuilder serialized = new StringBuilder();

			serialized.Append($"VendorName={VendorName}\n");
			serialized.Append($"ProductName={ProductName}\n");
			serialized.Append($"SerialNumber={SerialNumber}\n");

			return serialized.ToString();
		}

		public bool IsConnected()
		{
			return null != new Gamepad.Devices().GetGamepads().FirstOrDefault(info => 
				info.VendorName == VendorName
				&& info.ProductName == ProductName
				&& info.SerialNumber == SerialNumber);
		}

		public static GamepadInfo Deserialize(string serial)
		{
			string[] split = serial.Split('\n');

			return split.Aggregate(new GamepadInfo(), (info, line) =>
			{
				if (string.IsNullOrEmpty(line))
					return info;

				int pos = line.IndexOf('=');
				string key = line.Substring(0, pos);
				string val = line.Substring(pos + 1);

				switch (key)
				{
					case "VendorName": info.VendorName = val; break;
					case "ProductName": info.ProductName = val; break;
					case "SerialNumber": info.SerialNumber = val; break;
					default: break;
				}

				return info;
			});
		}

		public Gamepad ToGamepad()
		{
			var devs = new Gamepad.Devices();
			for (int i = 0; i < devs.DeviceCount; i++)
			{
				var dev = devs.GetInfo(i);

				if (dev.VendorName == VendorName && dev.ProductName == ProductName && dev.SerialNumber == SerialNumber)
					return new Gamepad(i);
			}

			return null;
		}
	}
}
