using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents the key/value for a site setting.
	/// </summary>
	public class SiteSetting : ModelBase
	{
		/// <summary>
		/// Site setting name (key)
		/// </summary>
		public string SettingName { get; set; }

		/// <summary>
		/// Site setting value.
		/// </summary>
		public string SettingValue { get; set; }
	}
}
