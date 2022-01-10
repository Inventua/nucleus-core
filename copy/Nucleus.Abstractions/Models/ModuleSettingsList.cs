using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	public class ModuleSettingsList : Dictionary<string, string>
	{
		public string Get(string key, string defaultValue)
		{
			if (this.ContainsKey(key))
			{
				return base[key];
			}
			else
			{
				return defaultValue;
			}
		}

		public T Get<T>(string key, T defaultValue)
		{
			if (this.ContainsKey(key))
			{
				try
				{
					return (T)System.ComponentModel.TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(base[key]);
					//return (T)Convert.ChangeType(base[key], typeof(T));
				}
				catch
				{
					return defaultValue;
				}
			}
			else
			{
				return defaultValue;
			}
		}

		public void Set<T>(string key, T value)
		{
			base[key] = (string)System.ComponentModel.TypeDescriptor.GetConverter(typeof(T)).ConvertToString(value);
			//base[key] = (string)Convert.ChangeType(value, typeof(string));
		}
	}
}
