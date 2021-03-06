using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.Managers;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Extension methods to transform <see cref="Site"/> <see cref="Site.SiteSettings"/> from name/value pairs to strongly-typed classes and back again.
	/// </summary>
	public static class SiteExtensions
	{
		/// <summary>
		/// Value used to present a password whose value has not been changed.
		/// </summary>
		public const string UNCHANGED_PASSWORD = "#$UNCHANGED";

		/// <summary>
		/// Returns a value which specifies whether the site's user profile values contains a property with the specified 
		/// <paramref name="typeUri"/>.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="typeUri"></param>
		/// <returns></returns>
		static public Boolean HasProperty(this Site site, string typeUri)
		{
			return site.UserProfileProperties.Exists(prop => prop.TypeUri == typeUri);
		}

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

			if (site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_WELCOMENEWUSER, out Guid id))
			{
				result.WelcomeNewUserTemplateId = id;
			}

			if (site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_PASSWORDRESET, out id))
			{
				result.PasswordResetTemplateId = id;
			}

			if (site.SiteSettings.TryGetValue(Site.SiteTemplatesKeys.MAILTEMPLATE_ACCOUNTNAMEREMINDER, out id))
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
			MailSettings result = new()
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

			if (site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_HOSTNAME, out string hostName))
			{
				result.HostName = hostName;
			}

			if (site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_PORT, out int port))
			{
				result.Port = port;
			}
			if (result.Port==0)
			{
				result.Port = MailSettings.DEFAULT_SMTP_PORT;
			}

			if (site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_USESSL, out bool useSsl))
			{
				result.UseSsl = useSsl;
			}

			if (site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_SENDER, out string sender))
			{
				result.Sender = sender;
			}

			if (site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_USERNAME, out string username))
			{
				result.UserName = username;
			}

			if (site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_PASSWORD, out string password))
			{
				result.Password = password;
			}

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
		/// Return the relative path to the site's icon image file.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="fileSystemManager"></param>
		/// <returns></returns>
		public async static Task<string> GetIconPath(this Site site, IFileSystemManager fileSystemManager)
		{
			if (site.SiteSettings.TryGetValue(Site.SiteFilesKeys.FAVICON_FILEID, out Guid fileId))
			{
				if (fileId == Guid.Empty)
				{
					return "";
				}
				else
				{
					return await GetDirectFilePath(site, fileId, fileSystemManager);
					//return $"/files/{FileExtensions.EncodeFileId(fileId)}";
				}
			}
			return null;
		}

		/// <summary>
		/// Return the relative path to the site's icon image file.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="fileSystemManager"></param>
		/// <returns></returns>
		public async static Task<string> GetCssFilePath(this Site site, IFileSystemManager fileSystemManager)
		{
			if (site.SiteSettings.TryGetValue(Site.SiteFilesKeys.CSSFILE_FILEID, out Guid fileId))
			{
				if (fileId == Guid.Empty)
				{
					return "";
				}
				else
				{
					return await GetDirectFilePath(site, fileId, fileSystemManager);
					//return $"/files/{FileExtensions.EncodeFileId(fileId)}";
				}
			}
			return null;
		}

		/// <summary>
		/// Return a direct file path, if the file system provider supports it.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="fileId"></param>
		/// <param name="fileSystemManager"></param>
		/// <returns></returns>
		/// <remarks>
		/// Return a direct link if the file system provider supports it (because it is faster than returning a redirect to azure storage).  This "skips"
		/// the Nucleus permissions check, but the performance difference is > 200ms.  This function should only be used for cases where it is ok to
		/// skip the permission check, like site logo/css/favicon.
		/// 
		/// If the file system provider does not support a direct link, a link to the FileController with an encoded file id parameter
		/// is returned, with a ~/ prefix.  Callers must call IUrlHelper.Content on the result of this function.
		/// </remarks>
		private async static Task<string> GetDirectFilePath(this Site site, Guid fileId, IFileSystemManager fileSystemManager)
		{
			File file = await fileSystemManager.GetFile(site, fileId);
			// render a direct link if the file system provider supports it (because it is faster than returning a redirect to azure storage).  This "skips"
			// the Nucleus permissions check, but the performance difference is > 200ms.
			if (file.Capabilities.CanDirectLink)
			{
				System.Uri uri = fileSystemManager.GetFileDirectUrl(site, file);
				if (uri != null)
				{
					return uri.AbsoluteUri;
				}
				else
				{
					return "";
				}
			}
			else
			{
				return $"~/files/{FileExtensions.EncodeFileId(file.Id)}";
			}


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
		/// Get (decrypt) the <see cref="MailSettings"/> password.
		/// </summary>
		/// <param name="site"></param>
		public static string GetPassword(this Site site)
		{
			if (site.SiteSettings.TryGetValue(Site.SiteMailSettingKeys.MAIL_PASSWORD, out string encryptedPassword))
			{
				return DecryptPassword(site, encryptedPassword);
			}
			return "";
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
			System.IO.MemoryStream msOut = new();

			// Create the ICryptoTransform instance.
			System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
			aes.Key = site.Id.ToByteArray();
			aes.IV = site.Id.ToByteArray();

			// Create the CryptoStream instance.
			System.Security.Cryptography.CryptoStream cryptStreem = new(msOut, aes.CreateEncryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

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

		/// <summary>
		/// Encrypt and encode a password and return the result.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		private static string DecryptPassword(Site site, string password)
		{
			if (String.IsNullOrEmpty(password))
			{
				return null;
			}

			// Convert string to byte array
			byte[] bytesIn = System.Convert.FromBase64String(password);

			// Preparing the memory stream for encrypted string.
			System.IO.MemoryStream msOut = new();

			// Create the ICryptoTransform instance.
			System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
			aes.Key = site.Id.ToByteArray();
			aes.IV = site.Id.ToByteArray();

			// Create the CryptoStream instance.
			System.Security.Cryptography.CryptoStream cryptStreem = new(msOut, aes.CreateDecryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

			// Encoding.
			cryptStreem.Write(bytesIn, 0, bytesIn.Length);
			cryptStreem.FlushFinalBlock();

			// Get the encrypted byte array.
			byte[] bytesOut = msOut.ToArray();

			cryptStreem.Close();
			msOut.Close();

			// Convert to string and return result value
			return System.Text.Encoding.UTF8.GetString(bytesOut);
		}

		/// <summary>
		/// Return the absolute Uri for the specified site as a string.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="useSSL"></param>
		/// <returns></returns>
		public static string AbsoluteUrl(this Site site, Boolean useSSL)
		{
			return AbsoluteUri(site, useSSL).ToString(); 
		}

		/// <summary>
		/// Return the absolute Uri for the specified site.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="useSSL"></param>
		/// <returns></returns>
		public static System.Uri AbsoluteUri(this Site site, Boolean useSSL)
		{
			return new($"http{(useSSL ? "s" : "")}://{site.DefaultSiteAlias.Alias}/");
		}

		/// <summary>
		/// Return the absolute Uri for the specified site and page as a string.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <param name="useSSL"></param>
		/// <returns></returns>
		public static string AbsoluteUrl(this Site site, Page page, Boolean useSSL)
		{
			return AbsoluteUri(site, page, useSSL).ToString();
		}

		/// <summary>
		/// Return the absolute Uri for the specified site and page.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <param name="useSSL"></param>
		/// <returns></returns>
		public static System.Uri AbsoluteUri(this Site site, Page page, Boolean useSSL)
		{
			return new System.Uri(AbsoluteUri(site, useSSL), page.DefaultPageRoute().Path + "/");
		}

		/// <summary>
		/// Return the absolute Uri for the specified site and relative url.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="relativeUrl"></param>
		/// <param name="useSSL"></param>
		/// <returns></returns>
		public static System.Uri AbsoluteUri(this Site site, string relativeUrl, Boolean useSSL)
		{
			return new System.Uri(AbsoluteUri(site, useSSL), relativeUrl + "/");
		}

		/// <summary>
		/// Return the absolute Uri for the specified site and relative url as a string.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="relativeUrl"></param>
		/// <param name="useSSL"></param>
		/// <returns></returns>
		public static string AbsoluteUrl(this Site site, string relativeUrl, Boolean useSSL)
		{
			return AbsoluteUri(site, relativeUrl, useSSL).ToString();
		}

		/// <summary>
		/// Return the absolute Uri for the specified site and page and relative url as a string.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <param name="relativeUrl"></param>
		/// <param name="useSSL"></param>
		/// <returns></returns>
		public static string AbsoluteUrl(this Site site, Page page, string relativeUrl, Boolean useSSL)
		{
			return AbsoluteUri(site, page, relativeUrl, useSSL).ToString();
		}


		/// <summary>
		/// Return the absolute Uri for the specified site and page and relative url.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="page"></param>
		/// <param name="relativeUrl"></param>
		/// <param name="useSSL"></param>
		/// <returns></returns>
		public static System.Uri AbsoluteUri(this Site site, Page page, string relativeUrl, Boolean useSSL)
		{
			return new System.Uri(AbsoluteUri(site, page, useSSL), relativeUrl + "/");
		}

	}
}
