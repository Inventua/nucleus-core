﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;

namespace Nucleus.Abstractions.Search
{
	/// <summary>
	/// Specifies the contract for components which can supply meta-data.  
	/// </summary>
	/// <remarks>
	/// Meta data can be used for search content, RSS feeds or other purposes.
	/// </remarks>
	public abstract class IContentMetaDataProducer
	{
		/// <summary>
		/// Return a list of search feed content items for the specified module.  
		/// </summary>
		/// <param name="site"></param>
		/// <returns>
		/// This overload returns a IAsyncEnumerable, so that implementations can use "async enumerables" (yield) to return items to the
		/// caller as they are created.  Implementations should return search items for all instances (in all sites and pages) of the 
		/// entities which they support.
		/// </returns>
		public abstract IAsyncEnumerable<ContentMetaData> ListItems(Site site);
	}
}
