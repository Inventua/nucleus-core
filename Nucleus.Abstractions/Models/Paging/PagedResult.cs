using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Paging
{
	/// <summary>
	/// Reprents a "page" of results, used to return information from functions which support paging.
	/// </summary>
  /// <typeparam name="TEntity">Entity type in results.</typeparam>
	public class PagedResult<TEntity> : PagingSettings
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{TEntity}" /> class.
    /// </summary>
    public PagedResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{TEntity}" /> class with the settings specified by <paramref name="settings"/>.
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
    /// Initializes a new instance of the <see cref="PagedResult{TEntity}" /> class with the settings specified by <paramref name="settings"/> and the page of items specified
    /// by <paramref name="items"/>.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="items"></param>
    public PagedResult(PagingSettings settings, IList<TEntity> items) : this(settings)
		{
      this.Items = items;
		}

    /// <summary>
    /// Gets or sets a list of items of <typeparamref name="TEntity"/> for the selected result page.
    /// </summary>
    /// <value>
    /// A list of items of type <typeparamref name="TEntity"/> for the currently selected page.
    /// </value>
    public IList<TEntity> Items { get; set; }

    
  }
}

