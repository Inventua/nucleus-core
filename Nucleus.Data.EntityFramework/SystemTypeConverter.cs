using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Nucleus.Data.EntityFramework
{
	/// <summary>
	/// Deserialization support for Microsoft.EntityFrameworkCore.Migrations.Operations.MigrationOperation types.
	/// </summary>
	public class SystemTypeConverter : JsonConverter
	{
		/// <summary>
		/// Specifies whether this class can serialize data.
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// Specifies whether this class can deserialize/serialize the specified type.
		/// </summary>
		/// <param name="objectType"></param>
		/// <returns></returns>
		public override bool CanConvert(Type objectType)
		{
			return objectType.IsAssignableFrom(typeof(System.Type));
		}

		/// <summary>
		/// Read JSON data and return the type specified by the wrapper.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="objectType"></param>
		/// <param name="existingValue"></param>
		/// <param name="serializer"></param>
		/// <returns></returns>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader?.Value is null) return null;

			string typeName = reader.Value.ToString();

			// Manually handle common names for types which aren't their "real" type names
			if (typeName.Equals("int", StringComparison.OrdinalIgnoreCase))
			{
				typeName = typeof(System.Int32).ToString();
			}
			else if (typeName.Equals("long", StringComparison.OrdinalIgnoreCase))
			{
				typeName = typeof(System.Int64).ToString();
			}
			else if (typeName.Equals("uniqueidentifier", StringComparison.OrdinalIgnoreCase))
			{
				typeName = typeof(System.Guid).ToString();
			}
			else if (typeName.Equals("bool", StringComparison.OrdinalIgnoreCase) || typeName.Equals("bit", StringComparison.OrdinalIgnoreCase))
			{
				typeName = typeof(System.Boolean).ToString();
			}
			// Allow types in the System. namespace to be specified in the input file without the namespace.
			else if (!typeName.Contains('.'))
			{
				typeName = $"System.{typeName}";
			}

			// Try to get type from type name string
			System.Type result = Type.GetType(typeName, false, true);			

			if (result == null)
			{
				throw new InvalidOperationException($"Invalid type {typeName}.");
			}

			return result;
		}
				
		/// <summary>
		/// Serialize the specified type.  
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(value.GetType().ToString());
		}
	}
}
