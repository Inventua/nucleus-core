using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Represents Html editor options from configuration.
	/// </summary>
	public class HtmlEditorOptions
	{
		/// <summary>
		/// Configuration file section path for Html Editor options.
		/// </summary>
		public const string Section = "Nucleus:HtmlEditor";

		/// <summary>
		/// Default selected Html editor, specified by key.  If not specified, the first editor will be selected as default.
		/// </summary>
		public string Default { get; set; }

		/// <summary>
		/// Cached default config, used to improve runtime performance.
		/// </summary>
		public HtmlEditorConfig DefaultHtmlEditorConfig { get; private set; }

		/// <summary>
		/// Html editor configurations.
		/// </summary>
		public List<HtmlEditorConfig> HtmlEditors { get; private set; } = new();

		/// <summary>
		/// Set cached default config, used to improve runtime performance.
		/// </summary>
		/// <param name="config"></param>
		public void SetDefaultHtmlEditorConfig(HtmlEditorConfig config)
		{
			this.DefaultHtmlEditorConfig = config;
		}
	}
}
