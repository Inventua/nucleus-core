//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml.Serialization;

//namespace Nucleus.Abstractions.Models.Generic
//{
//	/// <summary>
//	/// Serializable dictionary.
//	/// </summary>
//	/// <remarks>
//	/// Sourced from <seealso cref="https://weblogs.asp.net/pwelter34/444961"/>
//	/// </remarks>
//	/// <typeparam name="TKey"></typeparam>
//	/// <typeparam name="TValue"></typeparam>
//	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
//	{
//		#region "    IXmlSerializable    "
//		/// <summary>
//		/// This method always returns null.
//		/// </summary>
//		/// <returns></returns>
//		public System.Xml.Schema.XmlSchema GetSchema()
//		{
//			return null;
//		}

//		/// <summary>
//		/// Generates an object from its XML representation.
//		/// </summary>
//		/// <param name="reader"></param>
//		public void ReadXml(System.Xml.XmlReader reader)
//		{
//			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey), Models.Export.SiteTemplate.NAMESPACE);
//			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue), Models.Export.SiteTemplate.NAMESPACE);

//			bool wasEmpty = reader.IsEmptyElement;

//			reader.Read();

//			if (wasEmpty)
//				return;

//			while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
//			{
//				reader.ReadStartElement("item");
//				reader.ReadStartElement("key");

//				TKey key = (TKey)keySerializer.Deserialize(reader);

//				reader.ReadEndElement();
//				reader.ReadStartElement("value");
//				TValue value = (TValue)valueSerializer.Deserialize(reader);
//				reader.ReadEndElement();

//				this.Add(key, value);

//				reader.ReadEndElement();
//				reader.MoveToContent();

//			}

//			reader.ReadEndElement();
//		}

//		/// <summary>
//		/// Converts an object into its XML representation.
//		/// </summary>
//		/// <param name="writer"></param>
//		public void WriteXml(System.Xml.XmlWriter writer)
//		{
//			XmlSerializer keySerializer = new XmlSerializer(typeof(TKey), Models.Export.SiteTemplate.NAMESPACE);
//			XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue), Models.Export.SiteTemplate.NAMESPACE);

//			foreach (TKey key in this.Keys)
//			{
//				writer.WriteStartElement("item");

//				writer.WriteStartElement("key");
//				keySerializer.Serialize(writer, key);
//				writer.WriteEndElement();

//				writer.WriteStartElement("value");
//				TValue value = this[key];
//				valueSerializer.Serialize(writer, value);

//				writer.WriteEndElement();
//				writer.WriteEndElement();
//			}
//		}
//		#endregion
//	}
//}
