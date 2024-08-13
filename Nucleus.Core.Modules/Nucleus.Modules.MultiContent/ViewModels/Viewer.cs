using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using System.Globalization;

namespace Nucleus.Modules.MultiContent.ViewModels
{
	public class Viewer
	{
		public List<Content> Contents { get; set; }
		public string Layout { get; set; }
		public PageModule Module { get; set; }

		public LayoutSettings LayoutSettings { get; set; }

    public string CssClass()
    {
      string result = "";

      switch (this.Layout)
      {
        case "ViewerLayouts/Grid.cshtml":
          switch (this.LayoutSettings.Columns)
          {
            case LayoutSettings.ColumnStyles.Columns_Auto:
              break;
            case LayoutSettings.ColumnStyles.Columns_1:
              result += " columns-1";
              break;
            case LayoutSettings.ColumnStyles.Columns_2:
              result += " columns-2";
              break;
            case LayoutSettings.ColumnStyles.Columns_3:
              result += " columns-3";
              break;
            case LayoutSettings.ColumnStyles.Columns_4:
              result += " columns-4";
              break;
          }

          break;

        default:
          break;
      }

      return result; 
    }

    public string ItemCssClass()
    {
      string result = "";

      switch (this.Layout)
      {
        case "ViewerLayouts/Grid.cshtml":
          switch (this.LayoutSettings.Alignment)
          {
            case LayoutSettings.AlignmentStyles.Left:
              result += " text-start";
              break;
            case LayoutSettings.AlignmentStyles.Center:
              result += " text-center";
              break;
            case LayoutSettings.AlignmentStyles.Right:
              result += " text-end";
              break;
          }

          switch (this.LayoutSettings.BorderStyle)
          {
            case LayoutSettings.BorderStyles.Border_Default:
              result += " border-0";
              break;
            case LayoutSettings.BorderStyles.Border_Solid:
              result += " border border-solid";
              break;
          }

          switch (this.LayoutSettings.BorderSize)
          {
            case LayoutSettings.LineWidths.Width_None:
              result += " border-0";
              break;
            case LayoutSettings.LineWidths.Width_1:
              result += " border-1";
              break;
            case LayoutSettings.LineWidths.Width_2:
              result += " border-2";
              break;
            case LayoutSettings.LineWidths.Width_3:
              result += " border-3";
              break;
            case LayoutSettings.LineWidths.Width_4:
              result += " border-4";
              break;
          }

          switch (this.LayoutSettings.PaddingSize)
          {
            case LayoutSettings.LineWidths.Width_None:
              result += " p-0";
              break;
            case LayoutSettings.LineWidths.Width_1:
              result += " p-1";
              break;
            case LayoutSettings.LineWidths.Width_2:
              result += " p-2";
              break;
            case LayoutSettings.LineWidths.Width_3:
              result += " p-3";
              break;
            case LayoutSettings.LineWidths.Width_4:
              result += " p-4";
              break;
          }

          break;

        default:
          break;
      }

      return result;
    }
  }
}
