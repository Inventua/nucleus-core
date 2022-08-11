using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Paging
{
	/// <summary>
	/// Class used to submit paging data to functions which support paging.
	/// </summary>
	public class PagingSettings
	{
		/// <summary>
		/// constructor
		/// </summary>
		public PagingSettings()
		{
			this.CurrentPageIndex = 1;
			this.PageSize = this.PageSizes[0];
		}			

		/// <summary>
		/// Currently selected page number.
		/// </summary>
		/// <remarks>
		/// The first page is page 1.  The value zero has no meaning for this property.
		/// </remarks>
		public int CurrentPageIndex { get; set; } = 1;

    /// <summary>
    /// The maximum number of controls to show in the paging panel.
    /// </summary>
    public int MaxPageControls { get; set; } = 5;

    /// <summary>
    /// The count of available items in the database.
    /// </summary>
    public int TotalCount { get; set; } = -1;

    /// <summary>
    /// Currently selected page size
    /// </summary>
    public int PageSize { get; set; }

		/// <summary>
		/// List of available page sizes
		/// </summary>
		public List<int> PageSizes { get; set; } = new List<int>() { 10, 20, 50, 100, 250 };

    /// <summary>
    /// Returns the number of available pages
    /// </summary>
    public int TotalPages
    {
      get
      {
        return (int)Math.Ceiling((double)this.TotalCount / this.PageSize);
      }
    }

    /// <summary>
		/// Returns a list of pages which are available to be selected.
		/// </summary>
		public IDictionary<int, string> Pages
    {
      get
      {
        Dictionary<int, string> results = new();

        for (int count = 1; count <= this.TotalPages; count++)
        {
          results.Add(count, $"Page {count}");
        }

        return results;
      }
    }

		/// <summary>
		/// Return the row index of the first result to return, based on CurrentPageIndex and PageSize
		/// </summary>
		public int FirstRowIndex
		{
      get
			{
			  if (this.TotalCount != -1 && this.CurrentPageIndex > this.TotalPages)
			  {
				  this.CurrentPageIndex = 1;
			  }
			  return ((this.CurrentPageIndex - 1) * PageSize);		
			}				
		}

    /// <summary>
    /// Return the index of the last item shown on the current page.
    /// </summary>
    public int LastDisplayedRowIndex
		{
      get
			{
        if (CurrentPageIndex * PageSize > TotalCount)
				{
          return TotalCount;
				}
        else
				{
          return CurrentPageIndex * PageSize;
        }
			}
		}

    /// <summary>
    /// Returns a list of pages to display in a paging control.  
    /// </summary>
    /// <remarks>
    /// The paging control only displays a limited number of buttons for paging.  This function executes logic to include a 
    /// first page and last page control, in addition to 5 pages centered on the currently selected page, unless the currently selected
    /// page is less than 3, in which case it shows the first five pages, or the currently selected page is greater than total pages 
    /// minus 3, in which case the last five pages are shown.
    /// </remarks>
    public IList<int> PageControlNumbers
    {
      get
      {
        List<int> objPages = new();
        int intStart;
        int intFinish;

        if (this.TotalPages > this.MaxPageControls - 1)
        {
          if (this.CurrentPageIndex - (this.MaxPageControls / (double)2) - 1 < 0)
          {
            intStart = 1;
          }
          else
          {
            intStart = this.CurrentPageIndex - Convert.ToInt32(Math.Floor(this.MaxPageControls / (double)2));
          }

          if (intStart + this.MaxPageControls - 1 > this.TotalPages - 1)
            intFinish = this.TotalPages;
          else
            intFinish = intStart + this.MaxPageControls - 1;

          if (intFinish - intStart < this.MaxPageControls - 1)
            intStart = intFinish - (this.MaxPageControls);

          for (int intCount = intStart; intCount <= intFinish; intCount++)
          {
            objPages.Add(intCount);
          }
        }
        else
        {
          for (int intCount = 1; intCount <= TotalPages; intCount++)
          {
            objPages.Add(intCount);
          }
        }

        return objPages;
      }
    }

  }
}
