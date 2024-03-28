using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions;

/// <summary>
/// Extension methods for enums.
/// </summary>
public static class EnumExtensions
{
  /// <summary>
  /// Returns the name property of the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"></see> attribute for the specified enum 
  /// value, or the name if no display attribute is present.
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  public static string DisplayName(this Enum value)
  {
    FieldInfo field = value.GetType().GetField(value.ToString());
    return field.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.Name ?? field.Name;
  }

  /// <summary>
  /// Returns the description property of the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute" /> attribute for the specified enum value,
  /// or an empty string if no description attribute is present.
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  public static string Description(this Enum value)
  {
    FieldInfo field = value.GetType().GetField(value.ToString());
    return field.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.Description ?? "";
  }

  /// <summary>
  /// Returns the prompt property of the <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute" /> attribute for the specified enum value,
  /// or an empty string if no prompt attribute is present.
  /// </summary>
  /// <param name="value"></param>
  /// <returns></returns>
  public static string Prompt(this Enum value)
  {
    FieldInfo field = value.GetType().GetField(value.ToString());
    return field.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.Prompt ?? "";
  }
}
