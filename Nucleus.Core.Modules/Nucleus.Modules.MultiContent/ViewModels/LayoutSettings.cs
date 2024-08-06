using System;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.Modules.MultiContent.ViewModels
{
  public class LayoutSettings
	{
		public enum OrientationStyles
		{
			Horizontal = 0,
			Vertical = 1
		}

		public enum AlignmentStyles
		{
			Left = 0,
			Center = 1,
			Right = 2
		}

    public enum LineWidths
    {
      [Display(Name = "(not selected)")]
      Width_Default = -1,

      [Display(Name = "None")]
      Width_None = 0,

      [Display(Name = "1")]
      Width_1 = 1,

      [Display(Name = "2")]
      Width_2 = 2,

      [Display(Name = "3")]
      Width_3 = 3,

      [Display(Name = "4")]
      Width_4 = 4
    }

    public enum ColumnStyles
    {
      [Display(Name = "Auto")]
      Columns_Auto = 0,

      [Display(Name = "1")]
      Columns_1 = 1,

      [Display(Name = "2")]
      Columns_2 = 2,

      [Display(Name = "3")]
      Columns_3 = 3,

      [Display(Name = "4")]
      Columns_4 = 4
    }

    public enum BorderStyles
    {
      [Display(Name = "(not selected)")]
      Border_Default = -1,

      [Display(Name = "Solid")]
      Border_Solid = 1
    }

    public enum FaqAlignmentStyles
		{
			Left = AlignmentStyles.Left,
			Right = AlignmentStyles.Right
		}

		public enum Icons
		{
			Default = 0,
			[Display(Name = "Plus/Minus")] Plus = 1,
			Arrows = 2,
			[Display(Name = "Drop-down arrow")] DropDownArrows = 3
		}

		// Carousel settings
		public Boolean RenderFlush { get; set; }
		public Boolean ShowControls { get; set; }
		public Boolean ShowIndicators { get; set; }
		public int Interval { get; set; } = 5000;

		// Accordion settings
		public Boolean OpenFirst { get; set; }  

		// Alert layout settings
		public Boolean ShowCloseButton { get; set; }
		public string AlertStyle { get; set; }
		public string[] AlertStyles = { "Primary", "Secondary", "Success", "Danger", "Warning", "Info", "Light", "Dark" };

		// tabs and pills styles
		public OrientationStyles Orientation { get; set; }
		public AlignmentStyles Alignment { get; set; }
		public Boolean Fill { get; set; }
		public Boolean Justify { get; set; }

		public Icons Icon { get; set; }

    // grid styles
    public LineWidths PaddingSize { get; set; }

    public LineWidths BorderSize { get; set; }

    public BorderStyles BorderStyle { get; set; }

    public ColumnStyles Columns { get; set; }

  }
}
