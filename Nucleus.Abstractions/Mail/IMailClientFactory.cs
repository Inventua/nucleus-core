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

		public IMailClient Create(Site site);
	}
}
