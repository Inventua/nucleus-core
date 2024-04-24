using System;

namespace Nucleus.Modules.ContactUs.ViewModels;

public class Viewer : Models.Settings
{
  public Abstractions.Models.List CategoryList { get; set; }

  public Boolean MessageSent { get; set; }

	public Models.Message Message { get; set; } = new();

	public Boolean IsAdmin { get; set; }

	public string RecaptchaVerificationToken { get; set; }
}
