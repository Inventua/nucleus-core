using Nucleus.Abstractions.Models;

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
