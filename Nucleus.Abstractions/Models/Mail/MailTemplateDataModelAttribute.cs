using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Mail;

/// <summary>
/// Attribute used to mark a class as being a mail template data model.  Classes which use this attribute should also specify their
/// display name using the System.ComponentModel.DisplayName attribute.
/// </summary>
public class MailTemplateDataModelAttribute : System.Attribute
{
}
