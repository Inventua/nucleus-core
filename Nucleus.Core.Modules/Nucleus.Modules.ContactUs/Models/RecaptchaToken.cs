using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.ContactUs.Models;

public class Recaptcha
{
	private const string MINION_SITE_SITE_KEY = "6Le0DVgpAAAAAPpOOQfsViDaxWQafHxDFsc4sRjV";
	private const string MINION_SITE_SECRET_KEY = "6Le0DVgpAAAAAFVS9N5NMoS3GuDmlkGSszgGK7fj";

	public string SiteKey
	{ get 
		{ 
			return MINION_SITE_SITE_KEY; 
		} 
	} 

}

public class RecaptchaToken
{

	public DateTime Challenge_ts { get; set; }
	public float Score { get; set; }
	public List<string> ErrorCodes { get; set; }
	public bool Success { get; set; }
	public string Hostname { get; set; }
}
