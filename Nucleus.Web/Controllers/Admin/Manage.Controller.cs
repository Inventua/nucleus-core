using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Nucleus.Abstractions.Managers;
using Nucleus.Extensions.Authorization;

namespace Nucleus.Web.Controllers.Admin
{
	[Area("Admin")]
	[Authorize(Policy = Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY)]
	public class ManageController : Controller
	{
		private IExtensionManager ExtensionManager { get; }
		private Context Context { get; }

		private IEnumerable<System.Type> ControllerTypes { get; }

		public ManageController(Context context, IExtensionManager extensionManager)
		{
			this.Context = context;
			this.ExtensionManager = extensionManager;
			this.ControllerTypes = Nucleus.Core.Plugins.AssemblyLoader.GetTypes<Controller>();
		}

		/// <summary>
		/// Display the "manage" admin page
		/// </summary>
		[HttpGet]
		public async Task<ActionResult> Index()
		{
			ViewModels.Admin.Manage viewModel = new()
			{
				Extensions = (await this.ExtensionManager.ListControlPanelExtensions(ControlPanelExtensionDefinition.ControlPanelExtensionScopes.Site))
					.Where(extension => HasPermission(extension))
					.ToList()
			};

			return View("Index", viewModel);
		}

		/// <summary>
		/// Return whether the current user has permission to use the specified control panel extension.
		/// </summary>
		/// <param name="controlPanelExtension"></param>
		/// <returns></returns>
		/// <remarks>
		/// This function is intended for use with the standard Nucleus authorization policies and only checks the system admin
		/// policy and site admin policy.  This function checks the AuthorizeAttribute policy property only, it does not check the 
		/// AuthorizationSchemes or Roles properties. 
		/// </remarks>
		private Boolean HasPermission(ControlPanelExtensionDefinition controlPanelExtension)
		{
			// Look for controller types with the same name and extension name as the control panel extension.  We have to do it this way
			// because controlPanelExtension.ControllerName is actually a route value, not a fully-qualified type name.
			Type controllerType = this.ControllerTypes
				.Where(type => ControllerNameIs(type, controlPanelExtension.ControllerName) && ExtensionNameIs(type, controlPanelExtension.ExtensionName))
				.FirstOrDefault();
						
			if (controllerType == null)
			{
				// if the controller class could not be loaded, suppress display
				return false;
			}

			System.Reflection.MemberInfo member = controllerType.GetMember(controlPanelExtension.EditAction).FirstOrDefault();
			if (member == null)
			{
				// if the edit method could not be loaded, suppress display
				return false;
			}

			AuthorizeAttribute[] authAttributes = member.GetCustomAttributes(typeof(AuthorizeAttribute), true) as AuthorizeAttribute[];
			if (authAttributes.Length == 0)
			{
				// if the edit action member does not have any AuthorizeAttributes, use the controller class AuthorizeAttributes
				authAttributes = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true) as AuthorizeAttribute[];
			}

			if (authAttributes.Length == 0)
			{
				// If neither the controller or the edit action have any AuthorizeAttributes, there are no permissions set, so return true
				return true;
			}
			else
			{
				// Controller classes and actions can have multiple AuthorizeAttributes.  If they do, then the user must have ALL of the 
				// specified permissions.
				foreach (AuthorizeAttribute authAttribute in authAttributes)
				{
					if (!String.IsNullOrEmpty(authAttribute.Policy))
					{
						// Check permissions					
						switch (authAttribute.Policy)
						{
							case Nucleus.Abstractions.Authorization.Constants.SYSTEM_ADMIN_POLICY:
								if (!User.IsSystemAdministrator()) return false;
								break;
							case Nucleus.Abstractions.Authorization.Constants.SITE_ADMIN_POLICY:
								if (!User.IsSiteAdmin(this.Context.Site)) return false;
								break;
						}
					}
				}

				return true;
			}
		}

		/// <summary>
		/// Return whether the specified type's name matches the specified controller name.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="controllerName"></param>
		/// <returns></returns>
		/// <remarks>
		/// By convention, controller class names end with "Controller" but are generally specified without the "Controller" suffix,
		/// so we check for either the controller name as specified, or with a "Controller" suffix.
		/// </remarks>
		private Boolean ControllerNameIs(Type type, string controllerName)
		{
			return type.Name == controllerName || type.Name == controllerName + "Controller";
		}

		/// <summary>
		/// Return whether the specified type has an Extension attribute with a matching extension name.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="extensionName"></param>
		/// <returns></returns>
		private Boolean ExtensionNameIs(Type type, string extensionName)
		{
			ExtensionAttribute extensionAttribute = type.GetCustomAttributes(typeof(ExtensionAttribute), false).FirstOrDefault() as ExtensionAttribute;
			if (extensionAttribute != null)
			{
				return extensionAttribute.RouteValue == extensionName;
			}
			return false;
		}
	}
}
