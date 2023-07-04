using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Portable;

/// <summary>
/// Extensions for implementations of the <see cref="IPortable"/> interface.
/// </summary>
public static class PortableExtensions
{
  /// <summary>
	/// Copy the source object to a new object of T.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
  public static T CopyTo<T>(this object source) where T : class
  {  
    string thisObject = Newtonsoft.Json.JsonConvert.SerializeObject(source, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings
    {
      PreserveReferencesHandling = PreserveReferencesHandling.Objects
    });
    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(thisObject);
  }
}
