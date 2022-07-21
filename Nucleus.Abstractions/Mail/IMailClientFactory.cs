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
		/// Create a new instance of the <see cref="IMailClient"/> class for the specifed site.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		/// <remarks>
		/// This constructor is for use by code which is not running in the context of a Http request, such as a scheduled task.  For
		/// other uses, get an instance of this class from dependency injection by including a parameter in your class constructor.
		/// </remarks>

		public IMailClient Create(Site site);
	}
}
