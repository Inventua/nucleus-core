using System;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.ContactUs.Models;

public class Settings
{
	public const string MODULESETTING_CATEGORYLIST_ID = "contactus:categorylist:id";
	public const string MODULESETTING_MAILTEMPLATE_ID = "contactus:mailtemplate:id";
	public const string MODULESETTING_SEND_TO = "contactus:sendto";

	public const string MODULESETTING_SHOWNAME = "contactus:show-name";
	public const string MODULESETTING_SHOWCOMPANY = "contactus:show-company";
	public const string MODULESETTING_SHOWPHONENUMBER = "contactus:show-phonenumber";
	public const string MODULESETTING_SHOWCATEGORY = "contactus:show-category";
	public const string MODULESETTING_SHOWSUBJECT = "contactus:show-subject";
	public const string MODULESETTING_REQUIRENAME = "contactus:require-name";
	public const string MODULESETTING_REQUIRECOMPANY = "contactus:require-company";
	public const string MODULESETTING_REQUIREPHONENUMBER = "contactus:require-phonenumber";
	public const string MODULESETTING_REQUIRECATEGORY = "contactus:require-category";
	public const string MODULESETTING_REQUIRESUBJECT = "contactus:require-subject";

	public const string MODULESETTING_RECAPTCHA_SITE_KEY = "contactus:recaptcha-site-key";
	public const string MODULESETTING_RECAPTCHA_SECRET_KEY = "contactus:recaptcha-secret-key";
	public const string MODULESETTING_RECAPTCHA_ACTION = "contactus:recaptcha-action";

	public string SendTo { get; set; }

	public List CategoryList { get; set; }

	public Guid MailTemplateId { get; set; }

	public Boolean ShowName { get; set; }

	public Boolean ShowCompany { get; set; }

	public Boolean ShowPhoneNumber { get; set; }

	public Boolean ShowCategory { get; set; }

	public Boolean ShowSubject { get; set; }

	public Boolean RequireName { get; set; }

	public Boolean RequireCompany { get; set; }

	public Boolean RequirePhoneNumber { get; set; }

	public Boolean RequireCategory { get; set; }

	public Boolean RequireSubject { get; set; }

	public string RecaptchaSiteKey { get; set; }

	private string RecaptchaEncryptedSecretKey { get; set; }

	public string RecaptchaAction {  get; set; }

	public string GetSecretKey(Site site)
	{
		if (String.IsNullOrEmpty(this.RecaptchaEncryptedSecretKey))
		{
			return "";
		}
		else
		{
			return DecryptSecretKey(site, this.RecaptchaEncryptedSecretKey);
		}
	}
	
	public void SetSecretKey(Site site, string key)
	{
		if (String.IsNullOrEmpty(key))
		{
			this.RecaptchaEncryptedSecretKey = "";
		}
		else
		{
			this.RecaptchaEncryptedSecretKey = EncryptSecretKey(site, key);
		}
	}

	public void ReadSettings(PageModule module)
	{
		this.MailTemplateId = module.ModuleSettings.Get(Models.Settings.MODULESETTING_MAILTEMPLATE_ID, Guid.Empty);
		this.SendTo = module.ModuleSettings.Get(Models.Settings.MODULESETTING_SEND_TO, "");

		this.ShowName = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWNAME, true);
		this.ShowCompany = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWCOMPANY, true);
		this.ShowPhoneNumber = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWPHONENUMBER, true);
		this.ShowCategory = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWCATEGORY, true);
		this.ShowSubject = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_SHOWSUBJECT, true);

		this.RequireName = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIRENAME, true);
		this.RequireCompany = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIRECOMPANY, true);
		this.RequirePhoneNumber = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIREPHONENUMBER, true);
		this.RequireCategory = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIRECATEGORY, true);
		this.RequireSubject = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_REQUIRESUBJECT, true);

		this.RecaptchaSiteKey = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_RECAPTCHA_SITE_KEY, "");
		this.RecaptchaEncryptedSecretKey = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_RECAPTCHA_SECRET_KEY, "");
		this.RecaptchaAction = module.ModuleSettings.Get(ViewModels.Settings.MODULESETTING_RECAPTCHA_ACTION, "");
	}

	public void SetSettings(Site site, PageModule module)
	{
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_CATEGORYLIST_ID, this.CategoryList.Id);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_MAILTEMPLATE_ID, this.MailTemplateId);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_SEND_TO, this.SendTo);

		module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWNAME, this.ShowName);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWCOMPANY, this.ShowCompany);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWPHONENUMBER, this.ShowPhoneNumber);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWCATEGORY, this.ShowCategory);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_SHOWSUBJECT, this.ShowSubject);

		module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIRENAME, this.RequireName);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIRECOMPANY, this.RequireCompany);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIREPHONENUMBER, this.RequirePhoneNumber);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIRECATEGORY, this.RequireCategory);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_REQUIRESUBJECT, this.RequireSubject);

		module.ModuleSettings.Set(Models.Settings.MODULESETTING_RECAPTCHA_SITE_KEY, this.RecaptchaSiteKey);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_RECAPTCHA_SECRET_KEY, this.RecaptchaEncryptedSecretKey);
		module.ModuleSettings.Set(Models.Settings.MODULESETTING_RECAPTCHA_ACTION, this.RecaptchaAction);
	}

	/// <summary>
	/// Encrypt and encode a secret key and return the result.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="secretKey"></param>
	/// <returns></returns>
	private static string EncryptSecretKey(Site site, string secretKey)
	{
		if (String.IsNullOrEmpty(secretKey))
		{
			return null;
		}

		// Convert string to byte array
		byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(secretKey);

		// Preparing the memory stream for encrypted string.
		System.IO.MemoryStream msOut = new();

		// Create the ICryptoTransform instance.
		System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
		aes.Key = site.Id.ToByteArray();
		aes.IV = site.Id.ToByteArray();

		// Create the CryptoStream instance.
		System.Security.Cryptography.CryptoStream cryptStream = new(msOut, aes.CreateEncryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

		// Encoding.
		cryptStream.Write(bytesIn, 0, bytesIn.Length);
		cryptStream.FlushFinalBlock();

		// Get the encrypted byte array.
		byte[] bytesOut = msOut.ToArray();

		cryptStream.Close();
		msOut.Close();

		// Convert to string and return result value
		return System.Convert.ToBase64String(bytesOut);
	}

	/// <summary>
	/// Decrypt and decode a secret key and return the result.
	/// </summary>
	/// <param name="site"></param>
	/// <param name="secretKey"></param>
	/// <returns></returns>
	private static string DecryptSecretKey(Site site, string secretKey)
	{
		if (String.IsNullOrEmpty(secretKey))
		{
			return null;
		}

		// Convert string to byte array
		byte[] bytesIn = System.Convert.FromBase64String(secretKey);

		// Preparing the memory stream for encrypted string.
		System.IO.MemoryStream msOut = new();

		// Create the ICryptoTransform instance.
		System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create();
		aes.Key = site.Id.ToByteArray();
		aes.IV = site.Id.ToByteArray();

		// Create the CryptoStream instance.
		System.Security.Cryptography.CryptoStream cryptStream = new(msOut, aes.CreateDecryptor(aes.Key, aes.IV), System.Security.Cryptography.CryptoStreamMode.Write);

		// Encoding.
		cryptStream.Write(bytesIn, 0, bytesIn.Length);
		cryptStream.FlushFinalBlock();

		// Get the encrypted byte array.
		byte[] bytesOut = msOut.ToArray();

		cryptStream.Close();
		msOut.Close();

		// Convert to string and return result value
		return System.Text.Encoding.UTF8.GetString(bytesOut);
	}

}
