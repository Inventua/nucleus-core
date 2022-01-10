using Microsoft.AspNetCore.Diagnostics;

namespace Nucleus.Modules.ErrorReport.ViewModels
{
	public class Viewer
	{
		public IExceptionHandlerFeature ExceptionInfo { get; set; }
	}
}
