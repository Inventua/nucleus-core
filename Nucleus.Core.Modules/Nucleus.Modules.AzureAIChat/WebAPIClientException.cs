using System;
using System.Collections.Generic;
using System.Text;

namespace MICAS.Mobile.Service.Services;

public class WebAPIClientException : Exception
{
  public System.Net.Http.HttpResponseMessage Response { get; private set; }
  private WebAPIResponseMessage ResponseMessage { get; set; }

  public WebAPIClientException(System.Net.Http.HttpResponseMessage Response)
  {
    this.Response = Response;

    ParseMessage();
  }

  public override string Message
  {
    get
    {
      return ResponseMessage.Message.Replace("\n", ", ");
    }
  }

  public System.Net.HttpStatusCode StatusCode
  {
    get
    {
      return this.Response.StatusCode;
    }
  }

  private void ParseMessage()
  {
    try
    {
      this.ResponseMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<WebAPIResponseMessage>(Response.Content.ReadAsStringAsync().Result);
    }
    catch 
    {
      // if the message is not in JSON format, we will get a parse error
      this.ResponseMessage = null; // new WebAPIResponseMessage() { Message = this.Response.ReasonPhrase };
    }

    if (this.ResponseMessage == null)
      this.ResponseMessage = new WebAPIResponseMessage() { Message = this.Response.ReasonPhrase };

  }

  private class WebAPIResponseMessage
  {
    public string Message { get; set; }
  }
}
