using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Abstractions.Models.Mail;
using Microsoft.Extensions.Options;

namespace Nucleus.Abstractions.Mail
{
	/// <summary>
	/// Creates instances of IMailClient
	/// </summary>
	public interface IMailClientFactory
	{
		/// <summary>
		/// Create a new instance of the <see cref="IMailClient"/> class for the site in the current context.
		/// </summary>
		/// <returns></returns>
		/// <remarks>
		/// This constructor is for use by code which is running in the context of a Http request.
		/// </remarks>
		//public IMailClient Create();

		/// <summary>
		/// Create a new instance of the <see cref="IMailClient"/> class for the specifed site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// This constructor is for use by code which is not running in the context of a Http request, such as a scheduled task.
		/// </remarks>

		public IMailClient Create(Site site);
	}
}
