using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Nucleus.Modules.Account.Models;

public class SiteVerifyResponseToken
{
	[JsonProperty("challenge_ts")]
	public DateTime Challenge_ts { get; set; }
	[JsonProperty("score")]
	public float Score { get; set; }
	[JsonProperty("action")]
	public string Action { get; set; }
	[JsonProperty("error-codes")]
	public List<string> ErrorCodes { get; set; }
	[JsonProperty("success")]
	public bool Success { get; set; }
	[JsonProperty("hostname")]
	public string Hostname { get; set; }
}
