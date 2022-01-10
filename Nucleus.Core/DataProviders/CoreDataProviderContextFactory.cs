using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nucleus.Core.DataProviders
{
	public class CoreDataProviderContextFactory : IDesignTimeDbContextFactory<CoreDataProviderDbContext>
	{
		public CoreDataProviderDbContext CreateDbContext(string[] args)
		{
			//var optionsBuilder = new DbContextOptionsBuilder<CoreDataProviderDbContext>();
			//optionsBuilder.UseSqlite("Data Source=blog.db");
			
			return new CoreDataProviderDbContext(new EFToolsDbContextConfigurator<CoreDataProvider>(),null,null);
		}
	}
}
