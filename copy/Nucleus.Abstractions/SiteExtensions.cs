using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;

namespace Nucleus.Abstractions
{
	/// <summary>
	/// Extension methods to transform <see cref="Site"/> <see cref="Site.SiteSettings"/> from name/value pairs to strongly-typed classes and back again.
	/// </summary>
	public static class SiteExtensions
	{
		public const string UNCHANGED_PASSWORD = "#$UNCHANGED";

		/// <summary>
		/// Sets <see cref="Site.SiteSettings"/> based on a <see cref="SiteTemplateSelections"/> object.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="siteTemplateSelections"></param>
		public static void SetSiteMailTemplates(this Site site, SiteTemplateSelections siteTemplateSelections)
		{
			site.SiteSettings.TrySetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_WELCOMENEWUSER, siteTemplateSelections.WelcomeNewUserTemplateId);
			site.SiteSettings.TrySetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_PASSWORDRESET, siteTemplateSelections.PasswordResetTemplateId);
			site.SiteSettings.TrySetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_ACCOUNTNAMEREMINDER, siteTemplateSelections.AccountNameReminderTemplateId);
		}

		/// <summary>
		/// Gets a <see cref="SiteTemplateSelections"/> object based on  <see cref="Site.SiteSettings"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public static SiteTemplateSelections GetSiteTemplateSelections(this Site site)
		{
			SiteTemplateSelections result = new();
			Guid id;

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_WELCOMENEWUSER), out id))
			{
				result.WelcomeNewUserTemplateId = id;
			}

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_PASSWORDRESET), out id))
			{
				result.PasswordResetTemplateId = id;
			}

			if (Guid.TryParse(site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_ACCOUNTNAMEREMINDER), out id))
			{
				result.AccountNameReminderTemplateId = id;
			}

			return result;
		}

		/// <summary>
		/// Sets <see cref="Site.SiteSettings"/> based on a <see cref="MailSettings"/> object.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="mailSettings"></param>
		public static void SetSiteMailSettings(this Site site, MailSettings mailSettings)
		{
			site.SiteSettings.TrySetValue(Site.SiteMailSettingKeys.MAIL_HOSTNAME, mailSettings.HostName);
			site.SiteSettings.TrySetValue(Site.SiteMailSettingKeys.MAIL_PORT, mailSettings.Port);
			site.SiteSettings.TrySetValue(Site.SiteMailSettingKeys.MAIL_USESSL, mailSettings.UseSsl);

			site.SiteSettings.TrySetValue(Site.SiteMailSettingKeys.MAIL_SENDER, mailSettings.Sender);
			site.SiteSettings.TrySetValue(Site.SiteMailSettingKeys.MAIL_USERNAME, mailSettings.UserName);

			SetPassword(site, mailSettings);			
		}

		/// <summary>
		/// Gets a <see cref="MailSettings"/> object based on  <see cref="Site.SiteSettings"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="defaultValues"></param>
		/// <returns></returns>
		public static MailSettings GetSiteMailSettings(this Site site, MailSettings defaultValues)
		{
			MailSettings result = new MailSettings()
			{
				DeliveryMethod = defaultValues.DeliveryMethod,
				HostName = defaultValues.HostName,
				Password = defaultValues.Password,
				Port = defaultValues.Port,
				Sender = defaultValues.Sender,
				PickupDirectoryLocation = defaultValues.PickupDirectoryLocation,
				UserName = defaultValues.UserName,
				UseSsl = defaultValues.UseSsl
			}; 
			int port;
			Boolean useSsl;

			result.HostName = site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_HOSTNAME);

			if (int.TryParse(site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_PORT), out port))
			{
				result.Port = port;
			}
			if (result.Port==0)
			{
				result.Port = MailSettings.DEFAULT_SMTP_PORT;
			}

			if (Boolean.TryParse(site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_USESSL), out useSsl))
			{
				result.UseSsl = useSsl;
			}

			result.Sender = site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_SENDER);

			result.UserName = site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_USERNAME);
			result.Password = site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_PASSWORD);

			return result;
		}

		/// <summary>
		/// Gets a new <see cref="MailSettings"/> object.
		/// </summary>
		/// <param name="site"></param>
		/// <returns></returns>
		public static MailSettings GetSiteMailSettings(this Site site)
		{
			return GetSiteMailSettings(site, new());
		}

		/// <summary>
		/// Set the <see cref="MailSettings"/> password if the user has changed the value.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="settings"></param>
		private static void SetPassword(Site site, MailSettings settings)
		{
			if (settings.Password != UNCHANGED_PASSWORD)
			{
				site.SiteSettings.TrySetValue(Site.SiteMailSettingKeys.MAIL_PASSWORD, EncryptPassword(site, settings.Password));			
			}
		}

		/// <summary>
		/// Encrypt and encode a password and return the result.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		private static string EncryptPassword(Site site, string password)
		{
			if (String.IsNullOrEmpty(password ))
			{
				return null;
			}

			// Convert string to byte array
			byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(password);

			// Preparing the memory stream for encrypted string.
			System.IO.MemoryStream msOut = new System.IO.MemoryStream();

			// Create the ICryptoTransform instance.
			System.Security.Cryptography.AesManaged aes = new System.Security.Cryptography.AesManaged();
			aes.Key = site.Id.ToByteArray();
			aes.IV = site.Id.ToByteArray();

			// Create the CryptoStream instance.
			System.Security.Cryptography.CryptoStream cryptStreem = new System.Security.Cryptography.CryptoStream(msOut, aes.CreateEncryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

			// Encoding.
			cryptStreem.Write(bytesIn, 0, bytesIn.Length);
			cryptStreem.FlushFinalBlock();

			// Get the encrypted byte array.
			byte[] bytesOut = msOut.ToArray();

			cryptStreem.Close();
			msOut.Close();

			// Convert to string and return result value
			return System.Convert.ToBase64String(bytesOut);
		}

		
	}
}
