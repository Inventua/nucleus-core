using Nucleus.Abstractions.Models;
using Nucleus.Modules.Links.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Links.DataProviders
{
    public interface ILinksDataProvider : IDisposable, Nucleus.Core.DataProviders.Abstractions.IDataProvider
    {
        public Link Get(Guid Id);
        public IList<Link> List(PageModule pageModule);
        public void Save(PageModule pageModule, Link links);
        public void Delete(Link links);

    }
}
