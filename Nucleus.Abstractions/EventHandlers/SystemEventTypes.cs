using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.EventHandlers;
	
namespace Nucleus.Abstractions.EventHandlers.SystemEventTypes
{
	/// <summary>
	/// Type representing a Create operation (event).  This class has no methods or properties, it is used as a key for the <see cref="ISystemEventHandler&lt;TModel, TEvent&gt;"/> class.
	/// </summary>
	public class Create { }
	
	/// <summary>
	/// Type representing an Update operation (event).  This class has no methods or properties, it is used as a key for the <see cref="ISystemEventHandler&lt;TModel, TEvent&gt;"/> class.
	/// </summary>
	public class Update { }
	
	/// <summary>
	/// Type representing a Delete operation (event).  This class has no methods or properties, it is used as a key for the <see cref="ISystemEventHandler&lt;TModel, TEvent&gt;"/> class.
	/// </summary>
	public class Delete { }

	
}
