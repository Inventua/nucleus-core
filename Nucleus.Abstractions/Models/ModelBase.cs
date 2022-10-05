using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Base class for core model classes.
	/// </summary>
	public class ModelBase
	{
		/// <summary>
		/// Id of the user who added the record.
		/// </summary>
		[XmlIgnore]
		public Guid? AddedBy { get; set; }

		/// <summary>
		/// Date/Time when the record was added.
		/// </summary>
		[XmlIgnore] 
		public DateTime? DateAdded { get; set; }

		/// <summary>
		/// Id of the user who last changed the record.
		/// </summary>
		[XmlIgnore] 
		public Guid? ChangedBy { get; set; }

		/// <summary>
		/// Date/Time when the record was last changed.
		/// </summary>
		[XmlIgnore] 
		public DateTime? DateChanged { get; set; }

		/// <summary>
		/// Copy the object to a new object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Copy<T>() where T : class
		{
			string thisObject = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings
			{
				PreserveReferencesHandling = PreserveReferencesHandling.Objects
			});
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(thisObject);
		}

		/// <summary>
		/// Shallow-copy the object to an existing object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public void CopyTo<T>(T target) where T : class
		{
			foreach (System.Reflection.PropertyInfo targetProp in typeof(T).GetProperties())
			{
				System.Reflection.PropertyInfo sourceProp = this.GetType().GetProperty(targetProp.Name);

				if (sourceProp != null)
				{ 
					if ( sourceProp.CanRead && targetProp.SetMethod != null)
					{
						targetProp.SetValue(target, sourceProp.GetValue(this));
					}
					else if (sourceProp.CanRead && targetProp.CanRead && sourceProp.PropertyType.IsAssignableTo(typeof(System.Collections.IList)) && targetProp.PropertyType.IsAssignableTo(typeof(System.Collections.IList)))
					{
						Boolean canCopyList = false;
						if (targetProp.PropertyType.IsGenericType && sourceProp.PropertyType.IsGenericType)
            {
							// property is a generic list, make sure that the target is the same as the source.
							if (targetProp.PropertyType.GenericTypeArguments != null && sourceProp.PropertyType.GenericTypeArguments != null)
							{
								if (targetProp.PropertyType.GenericTypeArguments.Any() && sourceProp.PropertyType.GenericTypeArguments.Any())
								{
									if (targetProp.PropertyType.GenericTypeArguments[0].IsAssignableFrom(sourceProp.PropertyType.GenericTypeArguments[0]))
									{
										canCopyList = true;
									}
								}
							} 
						}
						else
            {
							// property is not a generic list.
							canCopyList = true;
            }

						if (canCopyList)
						{ 
							// copy items from source to target when property is a collection and doesn't have a setter.
							System.Collections.IList sourceCollection = (System.Collections.IList)sourceProp.GetValue(this);
							System.Collections.IList targetCollection = (System.Collections.IList)targetProp.GetValue(target);

							foreach (Object sourceItem in sourceCollection)
							{ 
								targetCollection.Add(sourceItem);
							}
						}
					}
				}
			}
		}
	}
}
