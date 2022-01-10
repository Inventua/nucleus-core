using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration
{
	/// <summary>
	/// Class used to read cache options from configuration files.
	/// </summary>
	public class CacheOptions
	{
		/// <summary>
		/// Location of cache items in the configuration file.
		/// </summary>
		public const string Section = "Nucleus:CacheOptions";

		public CacheOption PageCache { get; private set; } = new();
		public CacheOption PageModuleCache { get; private set; }= new();
		public CacheOption RoleGroupCache { get; private set; }= new();
		public CacheOption SiteGroupCache { get; private set; } = new();
		public CacheOption RoleCache { get; private set; }= new();
		public CacheOption SiteCache { get; private set; }= new();
		public CacheOption SiteDetectCache { get; private set; }= new();
		public CacheOption UserCache { get; private set; }= new();
		public CacheOption MailTemplateCache { get; private set; } = new();
		public CacheOption ScheduledTaskCache { get; private set; } = new();
		public CacheOption FolderCache { get; private set; } = new();

		public CacheOption PageMenuCache { get; private set; } = new();
		public CacheOption ListCache { get; private set; } = new();

	}
}
