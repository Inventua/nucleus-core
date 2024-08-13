using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Mail;

/// <summary>
/// Creates instances of IMailClient
/// </summary>
public interface IMailClientFactory
{
  /// <summary>
  /// Create a new mail client. The mail provider type is that which is configured for the specifed site.
  /// </summary>
  /// <param name="site"></param>
  /// <returns>
  /// A new instance of <see cref="IMailClient"/>.
  /// </returns>

  public IMailClient Create(Site site);
}
