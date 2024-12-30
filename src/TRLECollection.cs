/*
	Copyright © badassgamez Ltd.

	Author: badassgamez
	Created:    11.17.2024

	License: MIT
*/

using System.Collections.ObjectModel;
using System.Windows.Controls;
using TRLEManager.Properties;

namespace TRLEManager
{
	internal class TRLECollection
	{
		private static ObservableCollection<TRLE> _trles { get; set; }

		public static void LoadFromSettings()
		{
			_trles = TRLE.BulkDeserialize<ObservableCollection<TRLE>>(Settings.Default.TRLECollection);
			_trles.CollectionChanged += TRLECollection_CollectionChanged;
		}

		private static void TRLECollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			SaveToSettings();
		}

		public static void SaveToSettings()
		{
			Settings.Default.TRLECollection = TRLE.BulkSerialize(_trles);
		}

		public static void SetAsItemsSource(ItemsControl ctrl)
		{
			ctrl.ItemsSource = _trles;
		}

		public static void ReplaceTRLEInfo(int index, TRLE info)
		{
			_trles.CollectionChanged -= TRLECollection_CollectionChanged;
			_trles.RemoveAt(index);
			_trles.Insert(index, info);
			_trles.CollectionChanged += TRLECollection_CollectionChanged;

			Settings.Default.TRLECollection = TRLE.BulkSerialize(_trles);
			Settings.Default.Save();
		}

		public static int GetTRLEIndexByEXEPath(string exePath)
		{
			int trlecount = _trles.Count;
			for (int i = 0; i < trlecount; i++)
			{
				TRLE info = _trles[i];
				if (string.Compare(info.EXEPath, exePath, true) == 0)
					return i;
			}

			return -1;
		}

		public static void MakeFirst(TRLE trle)
		{
			Remove(trle);
			Add(trle);
		}

		public static void Add(TRLE trle)
		{
			_trles.Insert(0, trle);
		}

		public static void Remove(TRLE trle)
		{
			_trles.Remove(trle);
		}

		public static int IndexOf(TRLE trle)
		{
			return _trles.IndexOf(trle);
		}
	}
}
