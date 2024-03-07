using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.CopilotClient.Models;

/// <summary>
/// class for serialization/deserialization DirectLineToken
/// </summary>
public class DirectLineToken
{
  public string Token { get; set; }
  public int Expires_in { get; set; }
  public string ConversationId { get; set; }
}
