using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Modules.ContactUs.ViewModels;

public class Viewer : Models.Settings
{
	public Boolean MessageSent { get; set; }

	public Models.Message Message { get; set; } = new();

	public Boolean IsAdmin { get; set; }

	public string RecaptchaVerificationToken { get; set; }
}
