using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents permission types
	/// </summary>
	public class PermissionType
	{
		/// <summary>
		/// Namespaces for core entity permission scopes
		/// </summary>
		public static class PermissionScopeNamespaces
		{
			/// <summary>
			/// Page permission scope namespace
			/// </summary>
			public const string Page =  Nucleus.Abstractions.Models.Page.URN + "/permissiontype";
			/// <summary>
			/// Module permission scope namespace
			/// </summary>
			public const string Module = Nucleus.Abstractions.Models.PageModule.URN + "/permissiontype";
			/// <summary>
			/// Folder permission scope namespace
			/// </summary>
			public const string Folder = Nucleus.Abstractions.Models.FileSystem.Folder.URN + "/permissiontype";
		}

		/// <summary>
		/// Scope namespaces for operations on core entities
		/// </summary>
		public static class PermissionScopes
		{
			public static string PAGE_VIEW = $"{PermissionScopeNamespaces.Page}/view";
			public static string PAGE_EDIT = $"{PermissionScopeNamespaces.Page}/edit";

			public static string MODULE_VIEW = $"{PermissionScopeNamespaces.Module}/view";
			public static string MODULE_EDIT = $"{PermissionScopeNamespaces.Module}/edit";

			public static string FOLDER_VIEW = $"{PermissionScopeNamespaces.Folder}/view";
			public static string FOLDER_EDIT = $"{PermissionScopeNamespaces.Folder}/edit";
		}

		/// <summary>
		/// Unique record identifier.
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// The module definition that the permission type belongs to, or NULL if the permission type applies to a page or page module.
		/// </summary>
		/// <remarks>
		/// Modules can specify custom permission types.  Modules must provide their own user interface to control custom permission types,
		/// the administrative interface does not display them.
		/// </remarks>
		public ModuleDefinition ModuleDefinition { get; set; }

		/// <summary>
		/// Permission type name, displayed on-screen in the administrative interface.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Display order for the permission type in the administrative interface.
		/// </summary>
		public int SortOrder { get; set; }

		/// <summary>
		/// Scope namespace (urn) for the entity and operation that the permission type applies to
		/// </summary>
		public string Scope { get; set; }
	}
}
