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
	public class PermissionType : ModelBase
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

      /// <summary>
      /// Namespace representing a disabled permission.
      /// </summary>
      [Obsolete("Permission.PermissionType.Disabled is deprecated, use Permission.IsDisabled instead.")]
      public const string Disabled = "urn:nucleus:entities:disabled/permissiontype";
		}

		/// <summary>
		/// Constants for well-known permission scope types.
		/// </summary>
		public static class PermissionScopeTypes
		{
			/// <summary>
			/// Standard permission scope suffix for view permission scopes.
			/// </summary>
			public static string VIEW = "view";

			/// <summary>
			/// Standard permission scope suffix for edit permission scopes.
			/// </summary>
			public static string EDIT = "edit";

      /// <summary>
			/// Standard permission scope suffix for browse permission scopes.
			/// </summary>
			public static string BROWSE = "browse";
    }

		/// <summary>
		/// Scope namespaces for operations on core entities
		/// </summary>
		public static class PermissionScopes
		{
			/// <summary>
			/// URN of scope which represents page view permission.
			/// </summary>
			public static string PAGE_VIEW = $"{PermissionScopeNamespaces.Page}/{PermissionScopeTypes.VIEW}";
			/// <summary>
			/// URN of scope which represents page edit permission.
			/// </summary>
			public static string PAGE_EDIT = $"{PermissionScopeNamespaces.Page}/{PermissionScopeTypes.EDIT}";

			/// <summary>
			/// URN of scope which represents module view permission.
			/// </summary>
			public static string MODULE_VIEW = $"{PermissionScopeNamespaces.Module}/{PermissionScopeTypes.VIEW}";
			/// <summary>
			/// URN of scope which represents module edit permission.
			/// </summary>
			public static string MODULE_EDIT = $"{PermissionScopeNamespaces.Module}/{PermissionScopeTypes.EDIT}";

			/// <summary>
			/// URN of scope which represents folder view permission.
			/// </summary>
			/// <remarks>
			/// For folders, the folder view permission includes the ability to view the folder, its contents and view or download files within it.
			/// </remarks>
			public static string FOLDER_VIEW = $"{PermissionScopeNamespaces.Folder}/{PermissionScopeTypes.VIEW}";

			/// <summary>
			/// URN of scope which represents folder edit permission.
			/// </summary>
			/// <remarks>
			/// For folders, the folder edit permission includes upload, rename, delete and all other operations which modify a folder or the files within it.
			/// </remarks>
			public static string FOLDER_EDIT = $"{PermissionScopeNamespaces.Folder}/{PermissionScopeTypes.EDIT}";

      /// <summary>
			/// URN of scope which represents folder browse permission.
			/// </summary>
			/// <remarks>
			/// For folders, the folder browse permission determines whether a user can view the files and sub-folders in a folder.
			/// </remarks>
			public static string FOLDER_BROWSE = $"{PermissionScopeNamespaces.Folder}/{PermissionScopeTypes.BROWSE}";
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
