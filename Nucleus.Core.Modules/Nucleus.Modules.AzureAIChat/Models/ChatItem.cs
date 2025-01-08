using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.AI.OpenAI.Chat;

namespace Nucleus.Modules.AzureAIChat.Models;

// ChatCitation is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable AOAI001 

public class ChatItem
{
  public string Question { get; set; }

  public DateTimeOffset? DateTime { get; set; }

  public string Answer { get; set; }

  public Boolean? IsError { get; set; }

  public List<ChatCitation> Citations { get; set; } = [];

  public List<string> Intents { get; set; } = [];
}
