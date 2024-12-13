using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nucleus.Abstractions.Conversion;
using Nucleus.Abstractions.Models;

namespace Nucleus.Core.Services.Conversion;

/// <summary>
/// Class which returns a <see cref="IContentConverter"/> implementatios to convert content from one MIME type to another.
/// </summary>
public class ContentConverterFactory : IContentConverterFactory
{
  private IEnumerable<IContentConverter> Converters { get; }

  /// <summary>
  /// Constructor.
  /// </summary>
  /// <param name="converters"></param>
  public ContentConverterFactory(IEnumerable<IContentConverter> converters)
  {
    this.Converters = converters;
  }

  /// <summary>
  /// Loop through available <see cref="IContentConverter"/> implementations and return the best implementation to convert  
  /// <paramref name="sourceContentType"/> to <paramref name="targetContentType"/>. 
  /// </summary>
  /// <param name="sourceContentType">Source MIME type</param>
  /// <param name="targetContentType">Target MIME type</param>
  /// <returns>
  /// Returns NULL if the content could not be converted.
  /// </returns>
  public IContentConverter Create(string sourceContentType, string targetContentType)
  {
    IContentConverter best = null;
    IContentConverter.Weights bestWeight = IContentConverter.Weights.Default;

    foreach (IContentConverter converter in Converters)
    {
      IContentConverter.Weights weight = converter.Weight(sourceContentType, targetContentType);
      if (weight != IContentConverter.Weights.NotCapable && (best == null || bestWeight < weight))
      {
        best = converter;
        bestWeight = weight;  
      }
    }

    return best;
  }

  /// <summary>
  /// Perform conversion using the best available content converter (<see cref="IContentConverter"/> implementation).
  /// </summary>
  /// <param name="content"></param>
  /// <param name="sourceContentType"></param>
  /// <param name="targetContentType"></param>
  /// <returns></returns>
  /// <exception cref="System.InvalidOperationException">
  /// No content converter (<see cref="IContentConverter"/> implementation) could perform the conversion.
  /// </exception>
  public async Task<byte[]> ConvertTo(Site site, byte[] content, string sourceContentType, string targetContentType)
  {
    return await this.Create(sourceContentType, targetContentType)?.ConvertTo(site, content, sourceContentType, targetContentType) ?? throw new InvalidOperationException($"No content converters could convert '{sourceContentType}' to '{targetContentType}'.");
  }
}
