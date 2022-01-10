//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Nucleus.Abstractions.Models
//{
//	/// <summary>
//	/// Represents a set of name/value pairs used to store module settings.
//	/// </summary>
//	public class ModuleSettings : Generic.SerializableDictionary<string, string>
//	{
//		/// <summary>
//		/// Get the value specified by <paramref name="key"/> as a string.
//		/// </summary>
//		/// <param name="key"></param>
//		/// <param name="defaultValue"></param>
//		/// <returns></returns>
//		public string Get(string key, string defaultValue)
//		{
//			if (this.ContainsKey(key))
//			{
//				return base[key];
//			}
//			else
//			{
//				return defaultValue;
//			}
//		}

//		/// <summary>
//		/// Get the value specified by <paramref name="key"/> as the type specified by <typeparamref name="T"/>
//		/// </summary>
//		/// <param name="key"></param>
//		/// <param name="defaultValue"></param>
//		/// <returns></returns>
//		public T Get<T>(string key, T defaultValue)
//		{
//			if (this.ContainsKey(key))
//			{
//				try
//				{
//					return (T)System.ComponentModel.TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(base[key]);
//				}
//				catch
//				{
//					return defaultValue;
//				}
//			}
//			else
//			{
//				return defaultValue;
//			}
//		}

//		/// <summary>
//		/// Set the value specified by <paramref name="key"/> of the type specified by <typeparamref name="T"/> to the value specified by <paramref name="value"/>.
//		/// </summary>
//		/// <param name="key"></param>
//		/// <param name="value"></param>
//		/// <returns></returns>
//		public void Set<T>(string key, T value)
//		{
//			base[key] = (string)System.ComponentModel.TypeDescriptor.GetConverter(typeof(T)).ConvertToString(value);
//		}
//	}
//}
