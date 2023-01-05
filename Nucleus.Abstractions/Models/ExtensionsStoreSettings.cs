using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents the key/value for an instance setting.
	/// </summary>
	public class ExtensionsStoreSettings : ModelBase
	{
    /// <summary>
    /// Specifies the type(s) of packages that can be installed.
    /// </summary>
    public enum PackageTracks
    {
      /// <summary>
      /// Specifies that the package is for general release.
      /// </summary>
      Standard = 0,
      /// <summary>
      /// Specifies that the package is for beta-testing use.
      /// </summary>
      Beta = 1,
      /// <summary>
      /// Specifies that the package is for pre-release testing use.
      /// </summary>
      Testing = 2
    }

    /// <summary>
    ///  
    /// </summary>
    public string StoreUri { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public Guid StoreId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public PackageTracks Track { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public DateTime RegistrationDate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Guid RegisteredBy { get; set; }

  }
}
