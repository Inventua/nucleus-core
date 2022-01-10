using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension methods for site settings
	/// </summary>
	public static class SiteSettingsExtensions
	{
		/// <summary>
		/// Gets the specified value or returns an empty string if the key is not present.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static Boolean TryGetValue(this List<SiteSetting> settings, string key, out string result)
		{
			SiteSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (value != null)
			{
				result = value.SettingValue;
				return true;
			}
			else
			{
				result = null;
				return false;
			}
		}

		/// <summary>
		/// Gets the specified value or returns an empty string if the key is not present.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static Boolean TryGetValue(this List<SiteSetting> settings, string key, out Guid result)
		{
			SiteSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (value != null)
			{
				Guid.TryParse(value.SettingValue, out result);
				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		/// <summary>
		/// Gets the specified value or returns an empty string if the key is not present.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static Boolean TryGetValue(this List<SiteSetting> settings, string key, out int result)
		{
			SiteSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (value != null)
			{
				int.TryParse(value.SettingValue, out result);
				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		/// <summary>
		/// Gets the specified value or returns an empty string if the key is not present.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public static Boolean TryGetValue(this List<SiteSetting> settings, string key, out Boolean result)
		{
			SiteSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (value != null)
			{
				Boolean.TryParse(value.SettingValue, out result);
				return true;
			}
			else
			{
				result = default;
				return false;
			}
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this List<SiteSetting> settings, string key, string value)
		{
			SiteSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (existing != null)
			{
				existing.SettingValue = value;
			}
			else
			{
				settings.Add(new SiteSetting() { SettingName = key , SettingValue = value });
			}
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this List<SiteSetting> settings, string key, Boolean? value)
		{
			SiteSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (existing != null)
			{
				existing.SettingValue = value.ToString();
			}
			else
			{
				settings.Add(new SiteSetting() { SettingName = key, SettingValue = value.ToString() });
			}
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this List<SiteSetting> settings, string key, int? value)
		{
			SiteSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (existing != null)
			{
				existing.SettingValue = value.ToString();
			}
			else
			{
				settings.Add(new SiteSetting() { SettingName = key, SettingValue = value.ToString() });
			}
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this List<SiteSetting> settings, string key, int value)
		{
			SiteSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (existing != null)
			{
				existing.SettingValue = value.ToString();
			}
			else
			{
				settings.Add(new SiteSetting() { SettingName = key, SettingValue = value.ToString() });
			}
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this List<SiteSetting> settings, string key, Guid? value)
		{
			SiteSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (existing != null)
			{
				existing.SettingValue = value.ToString();
			}
			else
			{
				settings.Add(new SiteSetting() { SettingName = key, SettingValue = value.ToString() });
			}
		}

		/// <summary>
		/// Adds or replaces the value for the specified key.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public static void TrySetValue(this List<SiteSetting> settings, string key, double value)
		{
			SiteSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
			if (existing != null)
			{
				existing.SettingValue = value.ToString();
			}
			else
			{
				settings.Add(new SiteSetting() { SettingName = key, SettingValue = value.ToString() });
			}
		}
	}

}
