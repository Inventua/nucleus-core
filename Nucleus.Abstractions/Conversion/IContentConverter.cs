using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Conversion;

/// <summary>
/// Interface used by implementations which can convert content types.
/// </summary>
public interface IContentConverter
{
  /// <summary>
  /// Enum used by the <see cref="Weight(string, string)"/> function, used to specity the priority for 
  /// </summary>
  public enum Weights
  {
    /// <summary>
    /// Specifies that the <see cref="IContentConverter"/> is unable to perform a conversion for the specified source and target MIME types.
    /// </summary>
    NotCapable = 0,

    /// <summary>
    /// Base weight for implementations.
    /// </summary>
    Default = 1,

    /// <summary>
    /// Specifies that this implementation can perform the conversion but its implementation may not be best.
    /// </summary>
    Capable = 50,

    /// <summary>
    /// Specifies that this implementation can perform the conversion.
    /// </summary>
    Good = 1000,

    /// <summary>
    /// Specifies that this implementation can perform the conversion.
    /// </summary>
    Better = 2000,

    /// <summary>
    /// Specifies that this implementation should have top weighting.
    /// </summary>
    Best = 10000
  }

  /// <summary>
  /// Convert the specified <paramref name="content"/> from <paramref name="sourceContentType"/> to <paramref name="targetContentType"/>.
  /// </summary>
  /// <param name="content"></param>
  /// <param name="sourceContentType"></param>
  /// <param name="targetContentType"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException">Thrown when the content converter is unable to convert the specified <paramref name="content"/> 
  /// from <paramref name="sourceContentType"/> to the specified to <paramref name="targetContentType"/></exception>
  public byte[] ConvertTo(byte[] content, string sourceContentType, string targetContentType);

  /// <summary>
  /// Allows the <see cref="IContentConverter"/> to nominate its priority for converting <paramref name="sourceContentType"/> to <paramref name="targetContentType"/>.
  /// </summary>
  /// <param name="sourceContentType"></param>
  /// <param name="targetContentType"></param>
  /// <returns></returns>
  /// <remarks>
  /// 
  /// </remarks>
  public Weights Weight(string sourceContentType, string targetContentType);
}
