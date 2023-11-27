using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Managers;

/// <summary>
/// Copy permission operation types
/// </summary>
public enum CopyPermissionOperation
{
	/// <summary>
	/// Replace existing permissions with the new permissions
	/// </summary>
	Replace = 1,
	/// <summary>
	/// Merge 
	/// </summary>
	Merge = 2
}


