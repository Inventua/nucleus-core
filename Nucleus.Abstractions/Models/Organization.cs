using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
  /// <summary>
  /// An organization is a collection of users
  /// </summary>
  public class Organization : ModelBase
  {
    /// <summary>
		/// Unique record identifier.
		/// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Organization name.
    /// </summary>
    public String Name { get; set; }

    /// <summary>
    /// Url-friendly organization name.
    /// </summary>
    public String EncodedName { get; set; }

    /// <summary>
    /// List of users who belong to the organization.
    /// </summary>
    public List<OrganizationUser> Users { get; set; }
  }
}
