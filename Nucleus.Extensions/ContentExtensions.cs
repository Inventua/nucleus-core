using System;
using Markdig;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions;

/// <summary>
/// Content extensions.
/// </summary>
public static class ContentExtensions
{
  private readonly static MarkdownPipeline MarkdownPipeline = new Markdig.MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .UsePipeTables()
    .UseAutoIdentifiers()
    .UseFootnotes()
    .UseGridTables()
    .UseBootstrap()
    .EnableTrackTrivia()
    .UseGenericAttributes()
    .Build();

  private static readonly ReverseMarkdown.Config ReverseMarkdownConfig = new()
  {
    CleanupUnnecessarySpaces = true,
    SuppressDivNewlines = true
  };

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
  public static Content ConvertTo(this Content content, string targetContentType)
  {
    switch (targetContentType)
    {
      case "text/markdown":
        content.Value = ToMarkdown(content);
        break;

      case "text/html":
        content.Value = ToHtml(content);
        break;

      case "text/plain":
        content.Value = ToText(content);
        break;

      default:
        throw new InvalidOperationException($"Unsupported target type '{targetContentType}'.");
    }

    content.ContentType = targetContentType;

    return content;
  }

  /// <summary>
  /// Convert the specified content to Html.
  /// </summary>
  /// <param name="content"></param>
  /// <returns></returns>
  public static string ToHtml(this Content content)
  {
    return ToHtml(content.Value, content.ContentType);
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
  public static string ToHtml(this string content, string fromContentType)
  {
    if (String.IsNullOrEmpty(content)) return "";

    switch (fromContentType)
    {
      case "text/markdown":
        return Markdown.ToHtml(content, MarkdownPipeline);

      case "text/plain":
        // replace double-line feeds with two <br> elements so that paragraphs work. Single line feeds get removed as they are
        // interpreted as being for ease of editing rather than intended for display.
        return content
          .Replace("\r\n", "\n")
          .Replace("\r", "\n")
          .Replace("  \n", "<br /><br />")
          .Replace("\n\n", "<br /><br />")
          .Replace("\n", "");

      default:  // "text/html"
        return content;
    }
  }

  /// <summary>
  ///  Return a string containing the conversion of the specified <see cref="Content"/> object's <see cref="Content.Value"/> to Markdown.
  /// </summary>
  /// <param name="content"></param>
  /// <returns></returns>
  public static string ToMarkdown(this Content content)
  {
    return ToMarkdown(content.Value, content.ContentType);
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
  public static string ToMarkdown(this string content, string fromContentType)
  {
    if (String.IsNullOrEmpty(content)) return "";

    switch (fromContentType)
    {
      case "text/markdown":
        return content;

      case "text/html":
        ReverseMarkdown.Converter reverseMarkdownConverter = new(ReverseMarkdownConfig);
        return reverseMarkdownConverter.Convert(content);

      case "text/plain":
        // to convert plain text to markdown we dont have to do anything
        return content;

      default:  // "text/markdown"
        return content;
    }
  }

  /// <summary>
  ///  Return a string containing the conversion of the specified <see cref="Content"/> object's <see cref="Content.Value"/> to plain text.
  /// </summary>
  /// <param name="content"></param>
  /// <returns></returns>
  public static string ToText(this Content content)
  {
    return ToText(content.Value, content.ContentType);
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
  public static string ToText(this string content, string fromContentType)
  {
    if (String.IsNullOrEmpty(content)) return "";

    switch (fromContentType)
    {
      case "text/plain":
        // to convert plain text to markdown we dont have to do anything
        return content;

      case "text/markdown":
        // to convert markdown to plain text, we render as HTML first then convert the results to plain text. The Markdig.Markdown.ToPlainText
        // can also do this, but loses line breaks
        return ToText(ToHtml(content, fromContentType), "text/html");

      case "text/html":
        HtmlAgilityPack.HtmlDocument doc = new();
        doc.OptionWriteEmptyNodes = true;
        doc.LoadHtml(content.Replace("</p>", "</p>\n"));
        return doc.DocumentNode.InnerText;

      default:  // "text/plain"
        return content;
    }
  }
}


