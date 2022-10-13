using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish
{
	internal static class EnumerableExtensions
	{
    public static IEnumerable<TSource> TakeArticles<TSource>(this IEnumerable<TSource> source, int count)
    {
      return count == 0 ? source : source.Take(count);
    }
  }
}
