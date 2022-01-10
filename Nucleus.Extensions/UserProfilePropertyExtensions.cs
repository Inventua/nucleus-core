using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension methods for the <seealso cref="UserProfileProperty"/> class.
	/// </summary>
	public static class UserProfilePropertyExtensions
	{
		/// <summary>
		/// Get the the configured settings for the UserProfileProperty claim type.
		/// </summary>
		/// <param name="userProfileProperty"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		static public ClaimTypeOption ClaimTypeOption (this UserProfileProperty userProfileProperty, ClaimTypeOptions options)
		{
			return options.Find(userProfileProperty.TypeUri);
		}

		/// <summary>
		/// Returns a value which specifies whether the user profile values list specified by <paramref name="properties"/> contains a property 
		/// with the specified <paramref name="typeUri"/>.
		/// </summary>
		/// <param name="properties"></param>
		/// <param name="typeUri"></param>
		/// <returns></returns>
		static public Boolean HasProperty(this List<UserProfileValue> properties, string typeUri)
		{
			return properties.Exists(prop => prop.UserProfileProperty.TypeUri == typeUri);
		}

		/// <summary>
		/// Retrieves the user profile value matching <paramref name="typeUri"/> from the user profile values list specified 
		/// by <paramref name="properties"/>
		/// </summary>
		/// <param name="properties"></param>
		/// <param name="typeUri"></param>
		/// <returns></returns>
		static public UserProfileValue GetProperty(this List<UserProfileValue> properties, string typeUri)
		{
			return properties.Find(prop => prop.UserProfileProperty.TypeUri == typeUri);
		}
	}
}
