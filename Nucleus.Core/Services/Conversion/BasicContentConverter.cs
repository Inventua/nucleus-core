using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nucleus.Abstractions.Conversion;
using Nucleus.Abstractions.Models;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Nucleus.Core.Services.Conversion;

/// <summary>
/// <see cref="IContentConverter"/> implementation which can convert to and from text/plain, text/html and text/markdown, and can 
/// convert application/pdf to plain text.
/// </summary>
public class BasicContentConverter : Abstractions.Conversion.IContentConverter
{  
  private static readonly UglyToad.PdfPig.ParsingOptions PdfParsingOptions = new()
  {
    SkipMissingFonts = true,
    UseLenientParsing = true
  };

  public IContentConverter.Weights Weight(string sourceContentType, string targetContentType)
  {
    switch (sourceContentType)
    {
      case "application/pdf":
        switch (targetContentType)
        {
          case "text/plain":
            return IContentConverter.Weights.Default;
        }
        break;

      case "text/markdown":
        switch (targetContentType)
        {
          case "text/markdown":
            return IContentConverter.Weights.Best;

          case "text/html":
          case "text/plain":
            return IContentConverter.Weights.Good;        
        }
        break;
        
      case "text/html":
        switch (targetContentType)
        {
          case "text/html": return IContentConverter.Weights.Best;
          
          case "text/markdown":          
          case "text/plain":
            return IContentConverter.Weights.Good;
        }
        break;

      case "text/plain":
        switch (targetContentType)
        {
          case "text/plain": return IContentConverter.Weights.Best;

          case "text/markdown":
          case "text/html":
            return IContentConverter.Weights.Good;
        }
        break;
    }

    return IContentConverter.Weights.NotCapable;
  }

  /// <summary>
  /// Convert a <see cref="Content"/> object's <see cref="Content.Value"/> to the format specified by <paramref name="targetContentType"/>. 
  /// This function changes the values of the <see cref="Content.Value"/> and <see cref="Content.ContentType"/> properties in the specified 
  /// <see cref="Content"/> object.
  /// </summary>
  /// <param name="content"></param>
  /// <param name="targetContentType"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  /// <remarks>
  /// This function supports convertion to and from text/html, text/markdown and text/plain.
  /// </remarks>
  public Task<byte[]> ConvertTo(Site site, byte[] content, string sourceContentType, string targetContentType)
  {
    switch (targetContentType)
    {
      case "text/markdown":
        return Task.FromResult(ToMarkdown(content, sourceContentType));

      case "text/html":
        return Task.FromResult(ToHtml(content, sourceContentType));

      case "text/plain":
        return Task.FromResult(ToPlainText(content, sourceContentType));

      default:
        throw new InvalidOperationException($"{nameof(BasicContentConverter)} cannot convert to '{sourceContentType}' to '{targetContentType}'.");
    }
  }

  /// <summary>
  /// Return a string containing the conversion of the specified <see cref="Content"/> object's <see cref="Content.Value"/> to Html.
  /// </summary>
  /// <param name="content"></param>
  /// <param name="fromContentType">MIME type of the original content.</param>
  /// <returns>The converted content string.</returns>
  /// <remarks>
  /// The contentType can be text/markdown or text/plain.  All other content type values are treated as text/html and
  /// are not converted.
  /// </remarks>
  private static byte[] ToHtml(byte[] content, string fromContentType)
  {
    if (content.Length == 0) return [];

    string contentString = System.Text.Encoding.UTF8.GetString(content);

    switch (fromContentType)
    {
      case "text/html":
        return content;

      case "text/markdown":
      case "text/plain":
        return ToBytes(Nucleus.Extensions.ContentExtensions.ToHtml(contentString, fromContentType));

      default:
        throw new InvalidOperationException($"{nameof(BasicContentConverter)} cannot convert '{fromContentType}' to 'text/html'.");
    }
  }

  /// <summary>
  /// Converts the specified string content to Markdown. 
  /// </summary>
  /// <param name="content"></param>
  /// <param name="fromContentType">MIME type of the original content.</param>
  /// <returns>The converted content string.</returns>
  /// <remarks>
  /// The contentType can be text/html or text/plain.  All other content type values are treated as text/markdown and
  /// are not converted.
  /// </remarks>
  public static byte[] ToMarkdown(byte[] content, string fromContentType)
  {
    if (content.Length == 0) return [];

    string contentString = System.Text.Encoding.UTF8.GetString(content);

    switch (fromContentType)
    {
      case "text/markdown":
      case "text/plain":  // to convert plain text to markdown we dont have to do anything
        return content;

      case "text/html":
        return ToBytes(Nucleus.Extensions.ContentExtensions.ToMarkdown(contentString, fromContentType));

      default:
        throw new InvalidOperationException($"{nameof(BasicContentConverter)} cannot convert '{fromContentType}' to 'text/markdown'.");
    }
  }

  /// <summary>
  /// Convert the specified string content to plain text.
  /// </summary>
  /// <param name="content"></param>
  /// <param name="fromContentType">MIME type of the original content.</param>
  /// <returns>The converted content string.</returns>
  /// <remarks>
  /// The contentType can be text/markdown or text/html.  All other content type values are treated as text/plain and
  /// are not converted.
  /// </remarks>
  public static byte[] ToPlainText(byte[] content, string fromContentType)
  {
    if (content.Length == 0) return [];

    string contentString = System.Text.Encoding.UTF8.GetString(content);

    switch (fromContentType)
    {
      case "text/plain":
        // content is already plain text, do nothing 
        return content;

      case "text/markdown":
      case "text/html":
        return ToBytes(Nucleus.Extensions.ContentExtensions.ToText(contentString, fromContentType));
      
      case "application/pdf":
        string result = "";

        UglyToad.PdfPig.PdfDocument document = UglyToad.PdfPig.PdfDocument.Open(content, PdfParsingOptions);

        NearestNeighbourWordExtractor extractor = new(new NearestNeighbourWordExtractor.NearestNeighbourWordExtractorOptions()
        {

        });

        foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
        {
          result += String.Join
          (
            " ",
            extractor.GetWords(DuplicateOverlappingTextProcessor.Get(page.Letters))
              .Select(word => word.Text)
              .Where(word => !String.IsNullOrEmpty(word?.Trim()))
          );
        }

        // Some documents contain a table of contents with a lot of dashes and dots, these regex replacements are to reduce those

        // replace multiple dashes (where there are more than 10) with a single dash
        result = Regex.Replace(result, "[-]{10,100}", "-");

        // replace multiples of ". " (like ". . . . .", where there are more than 10) with a dash
        result = Regex.Replace(result, "[ .]{10,100}", "-");

        return ToBytes(result);

      default:
        throw new InvalidOperationException($"{nameof(BasicContentConverter)} cannot convert '{fromContentType}' to 'text/plain'.");
    }
  }
  
  private static byte[] ToBytes(string value)
  {
    return System.Text.Encoding.UTF8.GetBytes(value);
  }
}
