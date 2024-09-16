using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI.Chat;

namespace Nucleus.Modules.AzureAIChat.Models;
public class ChatItem
{
  public string Question { get; set; }

  public DateTimeOffset? DateTime { get; set; }

  public string Answer { get; set; }

  public Boolean IsError { get; set; }

  public List<AzureChatCitation> Citations { get; set; } = []; 
}
