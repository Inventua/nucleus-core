using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.Razor
{
	/// <summary>
	/// Internal-use class used to submit a strongly-typed model to the Razor engine.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class RazorEngineTemplate<T>: RazorEngineCore.RazorEngineTemplateBase<T>	{	}
}
