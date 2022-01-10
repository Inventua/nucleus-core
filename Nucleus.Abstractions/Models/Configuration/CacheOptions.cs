//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Nucleus.Abstractions.Models.Configuration
//{
//	/// <summary>
//	/// Class used to read cache options from configuration files.
//	/// </summary>
//	public class CacheOptions
//	{
//		/// <summary>
//		/// Location of cache items in the configuration file.
//		/// </summary>
//		public const string Section = "Nucleus:CacheOptions";

//		/// <summary>
//		/// Page cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption PageCache { get; private set; } = new();

//		/// <summary>
//		/// Page Module cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption PageModuleCache { get; private set; }= new();

//		/// <summary>
//		/// Role group cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption RoleGroupCache { get; private set; }= new();

//		/// <summary>
//		/// SiteGroup cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption SiteGroupCache { get; private set; } = new();

//		/// <summary>
//		/// Role cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption RoleCache { get; private set; }= new();
//		/// <summary>
//		/// Site cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption SiteCache { get; private set; }= new();

//		/// <summary>
//		/// Content cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption ContentCache { get; private set; }= new();

//		/// <summary>
//		/// User cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption UserCache { get; private set; }= new();

//		/// <summary>
//		/// Mail template cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption MailTemplateCache { get; private set; } = new();

//		/// <summary>
//		/// Scheduled task cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption ScheduledTaskCache { get; private set; } = new();

//		/// <summary>
//		/// Folder cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption FolderCache { get; private set; } = new();
//		/// <summary>
//		/// Page menu cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption PageMenuCache { get; private set; } = new();
//		/// <summary>
//		/// List cache options
//		/// </summary>
//		/// <remarks>
//		/// Values are read from configuration.
//		/// </remarks>
//		public CacheOption ListCache { get; private set; } = new();

//	}
//}
