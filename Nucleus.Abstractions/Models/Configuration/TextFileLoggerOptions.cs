using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Nucleus.Abstractions;
using Microsoft.Extensions.Options;

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
	}
