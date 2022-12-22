using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
  /// <summary>
  /// A link between an organization and a user.
  /// </summary>
  public class OrganizationUser
  {
    /// <summary>
    /// Specifies the special rights for an organization user.
    /// </summary>
    public enum UserTypes
    {
      /// <summary>
      /// A regular user with no special rights.
      /// </summary>
      Member = 0,
      /// <summary>
      /// An organization administrator who can add/remove users from the organization.
      /// </summary>
      Administrator = 1
    }

    /// <summary>
    /// Organization that the user belongs to.
    /// </summary>
    public Organization Organization { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public User User { get; set; }

    /// <summary>
    /// Specifies the organization user type.
    /// </summary>
    public UserTypes UserType { get; set; }
  }
}
