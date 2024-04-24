using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Extensions;

/// <summary>
/// Extensions for <seealso cref="System.Type"/>.
/// </summary>
public static class TypeExtensions
{
  /// <summary>
  /// Return a string representation for the type with the full type name and assembly but no version information.
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static string ShortTypeName(this Type type)
  {
    return $"{type.FullName},{type.Assembly.GetName().Name}";
  }

  /// <summary>
  /// Return a string containing the type's DisplayName, or the type full name if no DisplayName attribute is present.
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static string GetFriendlyName(this Type type)
  {
    System.ComponentModel.DisplayNameAttribute displayNameAttribute = type.GetCustomAttributes(false)
      .Where(attr => attr is System.ComponentModel.DisplayNameAttribute)
      .Select(attr => attr as System.ComponentModel.DisplayNameAttribute)
      .FirstOrDefault();

    if (displayNameAttribute == null)
    {
      return $"{type.FullName}";
    }
    else
    {
      return displayNameAttribute.DisplayName;
    }
  }
}
