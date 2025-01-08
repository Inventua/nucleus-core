using System;
using System.ComponentModel.DataAnnotations;
using Nucleus.Abstractions.Models;
using Nucleus.Extensions;

namespace Nucleus.Modules.ContactUs.Models;

public class Settings
{
  private class ModuleSettingsKeys
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

    public const string MODULESETTING_RECAPTCHA_ENABLED = "contactus:recaptcha-enabled";
    public const string MODULESETTING_RECAPTCHA_SITE_KEY = "contactus:recaptcha-site-key";
    public const string MODULESETTING_RECAPTCHA_SECRET_KEY = "contactus:recaptcha-secret-key";
    public const string MODULESETTING_RECAPTCHA_ACTION = "contactus:recaptcha-action";
    public const string MODULESETTING_RECAPTCHA_SCORE_THRESHOLD = "contactus:recaptcha-score-threshold";
  }

  [Required(ErrorMessage = "Send to recipients is required.", AllowEmptyStrings = false)]
  public string SendTo { get; set; } = "";

  [Required(ErrorMessage = "Category is required.")]
	public Guid? CategoryListId { get; set; }

  [Required(ErrorMessage = "Mail template is required.")] 
  public Guid? MailTemplateId { get; set; }

  public Boolean ShowName { get; set; } = true;

	public Boolean ShowCompany { get; set; }= true;

	public Boolean ShowPhoneNumber { get; set; }= true;

	public Boolean ShowCategory { get; set; } = true;

  public Boolean ShowSubject { get; set; } = true;

  public Boolean RequireName { get; set; } = true;

  public Boolean RequireCompany { get; set; } = true;
  
	public Boolean RequirePhoneNumber { get; set; } = true;

  public Boolean RequireCategory { get; set; } = true;

  public Boolean RequireSubject { get; set; } = true;

  public Boolean RecaptchaEnabled { get; set; } = false;

  public string RecaptchaSiteKey { get; set; } = "";

  private string RecaptchaEncryptedSecretKey { get; set; } = "";

  public Boolean IsSecretKeySet 
  {
    get
    {
      return !String.IsNullOrEmpty(this.RecaptchaEncryptedSecretKey);
    }
  }

  [RegularExpression("^[A-Za-z0-9/_]+$", ErrorMessage = "Action can only contain alphanumeric characters, slashes, and underscores.")]
  public string RecaptchaAction { get; set; } = "";

  [Range(0.0, 1.0, ErrorMessage = "Score threshold must be between 0.0 and 1.0.")]
  public double RecaptchaScoreThreshold { get; set; } = 0.5;

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
		this.MailTemplateId = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_MAILTEMPLATE_ID, this.MailTemplateId);
    this.CategoryListId = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_CATEGORYLIST_ID, this.CategoryListId);
    this.SendTo = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SEND_TO, this.SendTo);

		this.ShowName = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOWNAME, this.ShowName);
		this.ShowCompany = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOWCOMPANY, this.ShowCompany);
		this.ShowPhoneNumber = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOWPHONENUMBER, this.ShowPhoneNumber);
		this.ShowCategory = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOWCATEGORY, this.ShowCategory);
		this.ShowSubject = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_SHOWSUBJECT, this.ShowSubject);

		this.RequireName = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_REQUIRENAME, this.RequireName);
		this.RequireCompany = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_REQUIRECOMPANY, this.RequireCompany);
		this.RequirePhoneNumber = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_REQUIREPHONENUMBER, this.RequirePhoneNumber);
		this.RequireCategory = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_REQUIRECATEGORY, this.RequireCategory);
		this.RequireSubject = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_REQUIRESUBJECT, this.RequireSubject);

    this.RecaptchaEnabled = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_ENABLED, this.RecaptchaEnabled);

    ReadRecaptchaSettings(module);
  }

  public void ReadEncryptedKeys(PageModule module)
  {
    this.RecaptchaEncryptedSecretKey = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SECRET_KEY, this.RecaptchaEncryptedSecretKey);
  }

  public void ReadRecaptchaSettings(PageModule module)
  {
    this.RecaptchaSiteKey = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SITE_KEY, this.RecaptchaSiteKey);
    this.RecaptchaEncryptedSecretKey = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SECRET_KEY, this.RecaptchaEncryptedSecretKey);
    this.RecaptchaAction = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_ACTION, this.RecaptchaAction);
    this.RecaptchaScoreThreshold = module.ModuleSettings.Get(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SCORE_THRESHOLD, this.RecaptchaScoreThreshold);
  }

  public void SetSettings(PageModule module)
	{
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_MAILTEMPLATE_ID, this.MailTemplateId);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_CATEGORYLIST_ID, this.CategoryListId);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SEND_TO, this.SendTo);

		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOWNAME, this.ShowName);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOWCOMPANY, this.ShowCompany);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOWPHONENUMBER, this.ShowPhoneNumber);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOWCATEGORY, this.ShowCategory);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_SHOWSUBJECT, this.ShowSubject);

		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_REQUIRENAME, this.RequireName);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_REQUIRECOMPANY, this.RequireCompany);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_REQUIREPHONENUMBER, this.RequirePhoneNumber);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_REQUIRECATEGORY, this.RequireCategory);
		module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_REQUIRESUBJECT, this.RequireSubject);

    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_ENABLED, this.RecaptchaEnabled);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SITE_KEY, this.RecaptchaSiteKey);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SECRET_KEY, this.RecaptchaEncryptedSecretKey);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_ACTION, this.RecaptchaAction);
    module.ModuleSettings.Set(ModuleSettingsKeys.MODULESETTING_RECAPTCHA_SCORE_THRESHOLD, this.RecaptchaScoreThreshold);
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
