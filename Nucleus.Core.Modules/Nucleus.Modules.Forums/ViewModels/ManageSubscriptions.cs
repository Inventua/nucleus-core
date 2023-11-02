using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Modules.Forums.Models;

namespace Nucleus.Modules.Forums.ViewModels
{
	public class ManageSubscriptions
	{
    public IList<Group> Groups { get; set; }
    public IList<Forum> Forums { get; set; }

    public UserSubscriptions Subscriptions { get; set; }
	}
}
