using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Portable;

/// <summary>
/// Payload for import/export operations/implementations and users of the IPortable interface.
/// </summary>
public class PortableContent
{
  /// <summary>
  /// Parameterless constructor, used when you want to set the properties of this class after creating it.
  /// </summary>
  public PortableContent() { }

  /// <summary>
  /// Constructor used to populate type URN and a list of items.
  /// </summary>
  /// <param name="typeURN"></param>
  /// <param name="items"></param>
  public PortableContent(string typeURN, List<object> items)
  {
    this.TypeURN = typeURN;
    this.Items = items;
  }

  /// <summary>
  /// Constructor used to populate type URN and a single item.
  /// </summary>
  /// <param name="typeURN"></param>
  /// <param name="item"></param>
  public PortableContent(string typeURN, object item)
  {
    this.TypeURN = typeURN;
    this.Items = new List<Object> { item };
  }

  /// <summary>
  /// String value which specifies the type of data in the <see cref="Items"/> list.  Available values are under 
  /// control of the module or extension which implements <seealso cref="IPortable"/>.
  /// </summary>
  public string TypeURN { get; set; }

  /// <summary>
  /// A list of objects to export or import.
  /// </summary>
  public List<object> Items { get; set; }

}
