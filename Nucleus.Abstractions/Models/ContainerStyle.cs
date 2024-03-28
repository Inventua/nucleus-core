using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models;

/// <summary>
/// Class used to specify the properties of a container style.
/// </summary>
/// <remarks>
/// Container styles are a representation of set of special Css @property elements and related css classes which provide generic style control 
/// for containers.
/// </remarks>
public class ContainerStyle
{
  /// <summary>
  /// The name of the style value.  This is derived from the Css @property name if the property which represents this container style.
  /// </summary>
  public string Name { get; set; }

  /// <summary>
  /// The display text for the container style, used for on-screen display.
  /// </summary>
  /// <remarks>
  /// To specify a container style title, add a comment inside the body of the relevant css @parameter class in the form:  /* title: your-title-value */
  /// </remarks>
  /// <example>
  /// @property --border-color
  /// {
  ///   /* title: Border Color */
  ///   /* baseClass: container-border */
  ///   syntax: "&lt;color&gt;";
  ///   inherits: true;
  ///   initial-value: black;
  /// }
  /// </example>
  public string Title { get; set; }

  /// <summary>
  /// Specifies the group for the container style.  Styles are group by 'Group' in the module settings page.
  /// </summary>
  /// <remarks>
  /// To specify a container style group, add a comment inside the body of the relevant css @parameter class in the form:  /* group: your-group */
  /// </remarks>
  /// <example>
  /// @property --border-color
  /// {
  ///   /* title: Border Color */
  ///   /* group: Border */
  ///   syntax: "&lt;color&gt;";
  ///   inherits: true;
  ///   initial-value: black;
  /// }
  /// </example>
  public string Group { get; set; }

  /// <summary>
  /// Specifies a "base" css class which must be applied to any container which has a style value from this container style selected.
  /// </summary>
  /// <remarks>
  /// This is used for cases where a css class must be added for any of the available style values to work.  For example, the container-border css class
  /// must be added to a container which has any of the border values selected.
  /// To specify a container style base class, add a comment inside the body of the relevant css @parameter class in the form:  /* baseClass: your-class */
  /// </remarks>
  /// <example>
  /// @property --border-color
  /// {
  ///   /* title: Border Color */
  ///   /* baseClass: container-border */
  ///   syntax: "&lt;color&gt;";
  ///   inherits: true;
  ///   initial-value: black;
  /// }
  /// </example>
  public string BaseCssClass { get; set; }

  /// <summary>
  /// Specifies that the values should appear in the order that they are specified in the css file.
  /// </summary>
  /// <remarks>
  /// This is used for cases where values should not be sorted alphabetically.  
  /// To specify the preserveOrder property, add a comment inside the body of the relevant css @parameter class in the form:  /* preserveOrder: true */
  /// </remarks>
  /// <example>
  /// @property --title-alignment
  /// {
  ///   /* title: Title Alignment */
  ///   /* preserveOrder: true */
  ///   ...
  /// }
  /// </example>
  public Boolean PreserveOrder { get; set; } = false;

  /// <summary>
  /// Specifies the syntax of the underlying css property in the form specified by <see href="https://drafts.css-houdini.org/css-properties-values-api/#the-syntax-descriptor"/>.
  /// </summary>
  public string Syntax { get; set; }

  /// <summary>
  /// A list of available values for the container style.
  /// </summary>
  public List<ContainerStyleValue> Values { get; set; } = new();

  /// <summary>
  /// The Css class of the selected container style value.  This value can be null.
  /// </summary>
  public string SelectedValue { get; set; }

  /// <summary>
  /// A custom value for the container style.  This value can be null.
  /// </summary>
  public string CustomValue { get; set; }


  /// <summary>
  /// Constructor.
  /// </summary>
  public ContainerStyle() { }

  /// <summary>
  /// Constructor.
  /// </summary>
  /// <param name="name"></param>
  public ContainerStyle(string name)
  {
    this.Name = name;
  }
}

/// <summary>
/// Represents an available value for a container style.
/// </summary>
public class ContainerStyleValue
{
  /// <summary>
  /// The name of the style value.  This is derived from the Css class name.
  /// </summary>
  public string Name { get; set; }

  /// <summary>
  /// The value used for on-screen display of the style value.
  /// </summary>
  /// <remarks>
  /// To specify a style value title, add a comment inside the body of the relevant css class in the form:  /* title: your-title-value */
  /// </remarks>
  /// <example>
  /// .container-style.border-size-1
  ///{
  ///  /* title: Smallest */
  ///  --border-size: var(--size-1);
  ///}
  /// </example>
  public string Title { get; set; }

  /// <summary>
  /// The Css class name to use when the style value is selected.
  /// </summary>
  public string CssClass { get; set; }

  /// <summary>
  /// Constructor.
  /// </summary>
  public ContainerStyleValue() { }
 

}
