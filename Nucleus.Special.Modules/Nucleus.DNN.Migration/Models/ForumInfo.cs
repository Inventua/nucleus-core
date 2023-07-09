using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models.FileSystem;

namespace Nucleus.DNN.Migration.Models;

public class ForumInfo
{
  public Guid Id { get; set; }
  public string Name { get; set; }

  public ForumGroupInfo ForumGroup { get; set; }

}

public class ForumGroupInfo
{
  public Guid Id { get; set; }
  public Guid ModuleId { get; set; }
  public string Name { get; set; }
  

}