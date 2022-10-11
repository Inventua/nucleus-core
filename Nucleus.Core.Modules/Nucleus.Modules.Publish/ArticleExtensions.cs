using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.Publish
{
  public static class ArticleExtensions
  {
    public static DateTime GetPublishedDate(this Models.Article article)
    {
      return article.PublishDate.HasValue ? article.PublishDate.Value : article.DateAdded.Value;
    }
  }
}
