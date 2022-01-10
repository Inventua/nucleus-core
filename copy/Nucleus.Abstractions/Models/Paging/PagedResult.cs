using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Paging
{
	/// <summary>
	/// Class used to return information from functions which support paging
	/// </summary>
	public class PagedResult<T> : PagingSettings
  {
    /// <summary>
    /// constructor
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="settings"></param>
    public PagedResult(PagingSettings settings)
    {
      this.CurrentPageIndex = settings.CurrentPageIndex;
      this.PageSize = settings.PageSize;
      this.PageSizes = settings.PageSizes;
      this.TotalCount = settings.TotalCount;

      if (this.PageSize == 0)
      {
        if (this.PageSizes.Count > 0)
        {
          this.PageSize = this.PageSizes[0];
        }
      }

    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="items"></param>
    public PagedResult(PagingSettings settings, IList<T> items) : this(settings)
		{
      this.Items = items;
		}

    /// <summary>
    /// A list of items of type T for the currently selected page.
    /// </summary>
    public IList<T> Items { get; set; }

    
  }
}

