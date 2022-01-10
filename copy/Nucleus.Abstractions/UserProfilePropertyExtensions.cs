using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;

namespace Nucleus.Abstractions
{
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
	}
}
