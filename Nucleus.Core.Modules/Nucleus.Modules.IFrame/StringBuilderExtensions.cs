using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.IFrame;

internal static class StringBuilderExtensions
{
  public static void Add(this StringBuilder builder, string name, string value)
  {
    if (builder.Length > 0)
    {
      builder.Append("; ");
    }
    builder.Append(name);
    builder.Append(':');
    builder.Append(value);
  }
}
