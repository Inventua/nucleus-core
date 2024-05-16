using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.Mail;
using Nucleus.Abstractions;
using Nucleus.Abstractions.Models.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

namespace Nucleus.Abstractions.Mail
{
	/// <summary>
	/// Allows Nucleus core and extensions to sent email using SMTP.
	/// </summary>
	/// <remarks>
	/// Use <see cref="IMailClientFactory.Create(Site)"/> to create an instance of this class.
	/// </remarks>
	public interface IMailClient : IDisposable
	{		
    /// <summary>
    /// Specifies the <seealso cref="Site"/> that is using this instance.
    /// </summary>
    public Site Site { get; set; }

    /// <summary>
    /// Generate a fully qualified path to the settings (cshtml) partial view for the mail client.
    /// </summary>
    /// <remarks>
    /// Return null or an empty string if there is no settings view for the mail client.
    /// </remarks>
    public abstract string SettingsPath { get; }

    /// <summary>
    /// Return the extended settings type for the mail client.
    /// </summary>
    /// <remarks>
    /// Return null if there is no settings view for the mail client.
    /// </remarks>
    public abstract IMailSettings GetSettings(Site site);

    /// <summary>
    /// Return the settings type for the mail client.
    /// </summary>
    /// <remarks>
    /// Return null if there is no settings view for the mail client.
    /// </remarks>
    public abstract void SetSettings(Site site, IMailSettings settings);

    /// <summary>
    /// Parse the specified template, and send the resulting email to the specified to address.
    /// </summary>
    /// <param name="template"></param>
    /// <param name="model"></param>
    /// <param name="to"></param>
    /// <typeparam name="TModel"></typeparam>
    public Task Send<TModel>(MailTemplate template, TModel model, string to)
			where TModel : class;
	}
}
