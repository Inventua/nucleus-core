using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Key(name) / value pair representing a module setting.
	/// </summary>
	public class ModuleSetting : ModelBase
	{
		/// <summary>
		/// Setting name (key).
		/// </summary>
		public string SettingName { get; set; }

		/// <summary>
		/// Setting value.
		/// </summary>
		public string SettingValue { get; set; }
	}
}
