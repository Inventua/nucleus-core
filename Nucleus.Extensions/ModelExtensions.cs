//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Nucleus.Abstractions.Models;
//using System.Data;
//using Nucleus.Data.Common;
//using System.Reflection;

//namespace Nucleus.Extensions
//{
//	/// <summary>
//	/// Populates a model from a database IDataReader.
//	/// </summary>
//	/// <remarks>
//	/// Property names must match the column name in the database (IDataReader) and be writable.
//	/// </remarks>
//	public static class ModelExtensions
//	{
//		/// <summary>
//		/// Reflection data cache.
//		/// </summary>
//		/// <remarks>
//		/// Reflection data is cached to improve performance.
//		/// </remarks>
//		private static Dictionary<System.Type, IEnumerable<PropertyInfo>> ReflectionCache { get; } = new();

//		public static T Create<T>(IDataReader reader)  where T : new()
//		{
//			T result = new();

//			if (!ReflectionCache.TryGetValue(typeof(T), out IEnumerable<PropertyInfo> properties))
//			{
//				properties = BuildCacheEntry(typeof(T));

//				ReflectionCache.TryAdd(typeof(T), properties);
//			}

//			foreach (PropertyInfo prop in properties)
//			{
//				if (DataHelper.FieldExists(reader, prop.Name))
//				{
//					if (prop.PropertyType == typeof(string))
//					{
//						prop.SetValue(result, DataHelper.GetString(reader, prop.Name));
//					}
//					else if (prop.PropertyType == typeof(Guid))
//					{
//						prop.SetValue(result, DataHelper.GetGUID(reader, prop.Name));
//					}
//					else if (prop.PropertyType == typeof(Guid?))
//					{
//						Guid value = DataHelper.GetGUID(reader, prop.Name);
//						prop.SetValue(result, value==Guid.Empty ? null : value);
//					}
//					else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?))
//					{
//						prop.SetValue(result, DataHelper.GetInteger(reader, prop.Name));
//					}
//					else if (prop.PropertyType == typeof(Boolean) || prop.PropertyType == typeof(Boolean?))
//					{
//						prop.SetValue(result, DataHelper.GetBoolean(reader, prop.Name));
//					}
//					else if (prop.PropertyType == typeof(DateTime))
//					{
//						prop.SetValue(result, DataHelper.GetDateTime(reader, prop.Name));
//					}
//					else if (prop.PropertyType == typeof(DateTimeOffset))
//					{
//						prop.SetValue(result, new DateTimeOffset(DateTime.SpecifyKind (DataHelper.GetDateTime(reader, prop.Name), DateTimeKind.Utc)));
//					}
//					else if (prop.PropertyType == typeof(double) || prop.PropertyType == typeof(double?))
//					{
//						prop.SetValue(result, DataHelper.GetDouble(reader, prop.Name));
//					}
//					else if (prop.PropertyType == typeof(long) || prop.PropertyType == typeof(long?))
//					{
//						prop.SetValue(result, DataHelper.GetLong(reader, prop.Name));
//					}
//					else if (prop.PropertyType.IsEnum)
//					{
//						prop.SetValue(result, DataHelper.GetInteger(reader, prop.Name));
//					}
//					else if (prop.PropertyType == typeof(System.Net.IPAddress))
//					{
//						if (System.Net.IPAddress.TryParse(DataHelper.GetString(reader, prop.Name), out System.Net.IPAddress tryValue))
//						{
//							prop.SetValue(result, tryValue);
//						}
//					}
//				}
//			}

//			return result;
//		}

//		//public static void Copy<T>(T source, T target) where T : class
//		//{
//		//	if (!ReflectionCache.TryGetValue(typeof(T), out IEnumerable<PropertyInfo> properties))
//		//	{
//		//		properties = BuildCacheEntry(typeof(T));

//		//		ReflectionCache.TryAdd(typeof(T), properties);
//		//	}

//		//	foreach (PropertyInfo prop in properties)
//		//	{
//		//		prop.SetValue(target, prop.GetValue(source));
//		//	}
//		//}

//		/// <summary>
//		/// Retrieve type properties from reflection and return as an array of PropertyInfo objects.
//		/// </summary>
//		/// <param name="type"></param>
//		/// <returns></returns>
//		static internal IEnumerable<PropertyInfo> BuildCacheEntry(Type type)
//		{
//			List<PropertyInfo> properties = new();

//			foreach (PropertyInfo prop in type.GetProperties())
//			{
//				if (prop.CanWrite)
//				{
//					properties.Add(prop);
//				}
//			}

//			return properties.ToArray();
//		}

//	}
//}
