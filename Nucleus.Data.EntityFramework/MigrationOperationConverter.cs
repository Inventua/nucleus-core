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
	internal class MigrationOperationConverter : JsonConverter
	{
		private string ProviderName { get; }

		public MigrationOperationConverter(string providerName)
		{
			this.ProviderName = providerName;
		}

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
			return
				objectType.IsAssignableFrom(typeof(MigrationOperation)) ||
				objectType.IsAssignableFrom(typeof(AddForeignKeyOperation)) ||
				objectType.IsAssignableFrom(typeof(AddPrimaryKeyOperation));
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
			JObject obj = JObject.Load(reader);
			string operation;

			if (objectType == typeof(MigrationOperation))
			{
				// We are deserializing DataDefinition.Operations
				// object is wrapped in a node which identifies the contained operation clr type (class which inherits MigrationOperation)
				operation = ((Newtonsoft.Json.Linq.JProperty)obj.First).Name;				
			}
			else
			{
				// We are deserializing a AddPrimaryKeyOperation, AddForeignKeyOperation or other property of another MigrationOperation 
				// object is the *actual* object that we are deserializing, use type from the objectType parameter (after checking for a 
				// DatabaseProviderSpecificOperation wrapper).

				operation = ((Newtonsoft.Json.Linq.JProperty)obj.First).Name;
				if (operation.Equals("DatabaseProviderSpecificOperation", StringComparison.OrdinalIgnoreCase))
				{
					return UnWrap(operation, obj, serializer);
				}
				else
				{
					// the serializer property must not be included in this call, or it would cause an infinite loop
					return obj.ToObject(objectType);
				}
			}

			if (operation.Equals("DatabaseProviderSpecificOperation", StringComparison.OrdinalIgnoreCase))
			{
				return UnWrap(operation, obj, serializer);
			}
			else
			{
				return AsType(operation, obj, serializer);
			}					
		}

		private object UnWrap(string operation, JObject obj, JsonSerializer serializer)
		{
			DatabaseProviderSpecificOperation wrapper = obj.First.First.ToObject<DatabaseProviderSpecificOperation>(serializer);
			if (wrapper.IsValidFor(this.ProviderName))
			{
				JObject unWrappedObj = (JObject)obj.First.First["operation"];
				operation = ((Newtonsoft.Json.Linq.JProperty)unWrappedObj.First).Name;
				return AsType(operation, unWrappedObj, serializer);
			}
			else
			{
				return null;
			}
		}

		private object AsType(string operation, JObject obj, JsonSerializer serializer)
		{
			Type operationType = null;

			System.Reflection.Assembly assembly = typeof(Microsoft.EntityFrameworkCore.Migrations.Operations.MigrationOperation).Assembly;
			string typeName = $"Microsoft.EntityFrameworkCore.Migrations.Operations.{operation}Operation";
			operationType = assembly.GetType(typeName, false, true);
			if (operationType == null)
			{
				throw new InvalidOperationException($"Type {typeName} not found.");
			}
			else
			{
				return (MigrationOperation)obj.First.First.ToObject(operationType, serializer);
			}
		}

		/// <summary>
		/// Serialize the specified type.  Not implemented.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, value);			
		}
	}
}
