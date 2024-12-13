using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Conversion;

/// <summary>
/// Interface for a class which returns a <see cref="IContentConverter"/> implementations to convert content from one MIME type to another.
/// </summary>
public interface IContentConverterFactory
{
  /// <summary>
  /// Loop through available <see cref="IContentConverter"/> implementations and return the best implementation to convert  
  /// <paramref name="sourceContentType"/> to <paramref name="targetContentType"/>. 
  /// </summary>
  /// <param name="sourceContentType"></param>
  /// <param name="targetContentType"></param>
  /// <returns></returns>
  public IContentConverter Create(string sourceContentType, string targetContentType);

  /// <summary>
  /// Perform conversion using the best available content converter (<see cref="IContentConverter"/> implementation).
  /// </summary>
  /// <param name="site"></param>
  /// <param name="content"></param>
  /// <param name="sourceContentType"></param>
  /// <param name="targetContentType"></param>
  /// <returns></returns>
  public Task<byte[]> ConvertTo(Site site, byte[] content, string sourceContentType, string targetContentType);

}
