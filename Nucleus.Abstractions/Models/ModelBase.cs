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
			string thisObject = Newtonsoft.Json.JsonConvert.SerializeObject(this);
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(thisObject);
		}

		/// <summary>
		/// Shallow-copy the object to an existing object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public void CopyTo<T>(T target) where T : class
		{
			foreach (System.Reflection.PropertyInfo prop in typeof(T).GetProperties())
			{
				System.Reflection.PropertyInfo thisProp = this.GetType().GetProperty(prop.Name);
				if (thisProp != null && thisProp.CanRead && prop.CanWrite)
				{
					prop.SetValue(target, thisProp.GetValue(this));
				}
			}
		}
	}
}
