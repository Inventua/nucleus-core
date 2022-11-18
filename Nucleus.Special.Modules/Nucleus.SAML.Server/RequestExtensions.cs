using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Http;
using Nucleus.SAML.Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.SAML.Server
{
	public static class RequestExtensions
	{
		public static string Issuer(this HttpRequest request)
		{
			return request.Host.ToUriComponent();

			//Uri defaultSite = new(new(request.Scheme + Uri.SchemeDelimiter + request.Host.ToUriComponent()), "/saml2/idp");

			//return defaultSite.ToString();
		}
	}
}
