using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Nucleus.Abstractions;
using Microsoft.Extensions.Options;

namespace Nucleus.Core.Logging
{
	public class TextFileLoggerOptions
	{
		internal const string Section = "Nucleus:TextFileLoggerOptions";

		private string _path;

		public Boolean Enabled { get; set; } = true;

		/// <summary>
		/// Gets or sets the <see cref="TextFileLogger"/> log file path.
		/// </summary>
		/// <remarks>
		/// If the specified path does not exist, it is automatically created.
		/// </remarks>
		public string Path 
		{ 
			get
			{
				return _path;
			}
			internal set
			{
				_path = Environment.ExpandEnvironmentVariables(value); 
			}
		}		
	}
}
