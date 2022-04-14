using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models
{
	/// <summary>
	/// Represents an API key which is used for authentication by API clients.
	/// </summary>
	public class ApiKey : ModelBase
	{
		/// <summary>
		/// API Key id
		/// </summary>
		public Guid Id { get; set; }	

		/// <summary>
		/// API Key Name.  
		/// </summary>
		/// <remarks>
		/// This value is used by admin pages to help identify the key.
		/// </remarks>
		public string Name { get; set; }	

		/// <summary>
		/// API Key notes.
		/// </summary>
		/// <remarks>
		/// Use notes to record any other information about the usage or purpose of the API key.
		/// </remarks>
		public string Notes { get; set; }

		/// <summary>
		/// API Key Secret.  This value is used when encrypting API data.
		/// </summary>
		public string Secret { get; set; }

		/// <summary>
		/// API Key scope.
		/// </summary>
		/// <remarks>
		/// Space-separated list of scopes for the API key.  Scopes represent the operations that the API key can be used for.
		/// </remarks>
		public string Scope { get; set; }
	}
}
