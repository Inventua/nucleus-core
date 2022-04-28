using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Extensions;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using MailKit;
using Nucleus.Abstractions.Mail;

namespace Nucleus.Core.Mail
{
	/// <summary>
	/// Allows Nucleus core and extensions to sent email using SMTP.
	/// </summary>
	public class MailClient : IMailClient, IDisposable
	{
		public MailSettings MailSettings { get; private set; }
			
		internal MailClient (Context context, IOptions<SmtpMailOptions> smtpMailOptions) : this(context.Site, smtpMailOptions)	{	}

		internal MailClient(Site site, IOptions<SmtpMailOptions> smtpMailOptions)
		{
			if (site == null)
			{
				throw new InvalidOperationException($"{nameof(site)} cannot be null");
			}

			// Apply site settings
			this.MailSettings = site.GetSiteMailSettings(smtpMailOptions.Value);
		}

		/// <summary>
		/// Parse the specified template, and send the resulting email to the specified 'to' address. The 'to' address can be a list of email addresses
		/// separated by commas or semicolons.
		/// </summary>
		/// <param name="template"></param>
		/// <param name="args"></param>
		/// <param name="to"></param>
		public void Send(MailTemplate template, MailArgs args, string to)
		{
			MimeKit.MimeMessage message = new();
			MimeKit.BodyBuilder builder = new();
			
			message.From.Add(MimeKit.MailboxAddress.Parse(this.MailSettings.Sender));

			foreach (string address in to.Split(new char[] { ',' , ';' }, StringSplitOptions.RemoveEmptyEntries))
      {
				message.To.Add(MimeKit.MailboxAddress.Parse(address));
			}

			message.Subject = ParseTemplate(template.Subject, args);
			builder.HtmlBody = ParseTemplate(template.Body, args);
			message.Body = builder.ToMessageBody();

			if (this.MailSettings.DeliveryMethod == System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory)
			{
				message.WriteTo(System.IO.Path.Combine(this.MailSettings.PickupDirectoryLocation, $"{Guid.NewGuid()}.eml"));
			}
			else
			{
				using (MailKit.Net.Smtp.SmtpClient client = new())
				{
					client.Connect(this.MailSettings.HostName, this.MailSettings.Port, MailKit.Security.SecureSocketOptions.Auto);
					client.Authenticate(this.MailSettings.UserName, this.MailSettings.Password);

					client.Send(message);

					client.Disconnect(true);
				}
			}
		}

		/// <summary>
		/// Parse a template string, replacing template values in the form {key.property} with values from the args dictionary.
		/// </summary>
		/// <param name="template"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private static string ParseTemplate(string template, MailArgs args)
		{
			string result = System.Text.RegularExpressions.Regex.Replace(template, "{(.*?)}", new TemplateParser(args).MatchEvaluator);

			return result;
		}

		public void Dispose()
		{
			this.MailSettings = null;
			GC.SuppressFinalize(this);
		}

		private class TemplateParser
		{
			private MailArgs MailArgs { get; }

			public TemplateParser(MailArgs args)
			{
				this.MailArgs = args;
			}

			internal string MatchEvaluator(System.Text.RegularExpressions.Match match)
			{
				string key;
				string prop;

				if (match.Groups.Count > 1 && match.Groups[1].Value.Contains('.'))
				{
					key = match.Groups[1].Value.Split('.')[0];
					prop = match.Groups[1].Value[(key.Length + 1)..];
				}
				else
				{
					key = "";
					prop = match.Groups[1].Value;
				}

				switch (key)
				{
					case "":
						// handle standalone values
						return MatchStandaloneValue(prop);
					default:
						return MatchDictionaryValue(key, prop);
				}
			}

			private static string MatchStandaloneValue(string prop)
			{
				switch (prop)
				{
					case "time":
						return DateTime.Now.TimeOfDay.ToString();

					case "date":
						return DateTime.Now.ToString();

					default:
						return "";
				}
			}

			private string MatchDictionaryValue(string key, string prop)
			{
				
				if (!this.MailArgs.ContainsKey(key))
				{
					return "";
				}

				// loop through '.' separated property name - to handle properties of objects (like User.Secrets.PasswordResetToken).  A single 
				// property (like User.UserName) will only loop through once.
				object value = this.MailArgs[key];

				foreach (string part in prop.Split('.', StringSplitOptions.RemoveEmptyEntries))
				{
					if (value is IDictionary<string, object>)
					{
						if ((value as IDictionary<string, object>).ContainsKey(part))
						{
							value = (value as IDictionary<string, object>)[part];
						}
					}
					else
					{
						System.Reflection.PropertyInfo propInfo = value.GetType().GetProperty(part, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance );

						if (propInfo != null && propInfo.CanRead)
						{
							value = propInfo.GetValue(value);
						}
						else
						{
							return "";
						}
					}
				}				

				return value.ToString();
				//return "";
			}
		}
	}
}
