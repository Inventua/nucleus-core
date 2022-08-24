using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		public enum FaqAlignmentStyles
		{
			Left = AlignmentStyles.Left,
			Right = AlignmentStyles.Right
		}

		public enum Icons
		{
			Default = 0,
			[System.ComponentModel.DataAnnotations.Display(Name = "Plus/Minus")] Plus = 1,
			Arrows = 2,
			[System.ComponentModel.DataAnnotations.Display(Name = "Drop-down arrow")] DropDownArrows = 3
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
	}
}
