﻿using System;

namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Options for the text file logger.
/// </summary>
public class TextFileLoggerOptions
{
  /// <summary>
  /// Configuration file section path for text file logger options.
  /// </summary>
  public const string Section = "Nucleus:TextFileLoggerOptions";

  /// <summary>
  /// Specifies whether the text file logger is enabled.
  /// </summary>
  public Boolean Enabled { get; set; } = true;

  /// <summary>
  /// Gets or sets the TextFileLogger log file path.
  /// </summary>
  /// <remarks>
  /// If the specified path does not exist, it is automatically created.
  /// </remarks>
  public string Path { get; set; }

  /// <summary>
  /// Specifies how long log files are retained before being automatically deleted.
  /// </summary>
  /// <remarks>
  /// The default value is 7 days.
  /// </remarks>
  public TimeSpan LogFileExpiry { get; set; } = TimeSpan.FromDays(7);

}
