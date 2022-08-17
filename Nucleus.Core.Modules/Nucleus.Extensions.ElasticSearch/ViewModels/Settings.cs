using Microsoft.AspNetCore.Mvc.ModelBinding;
using Nucleus.Abstractions.Models;
using Nucleus.Abstractions.Models.FileSystem;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.ElasticSearch.ViewModels
{
	public class Settings : ConfigSettings
	{
		public const string DUMMY_PASSWORD = "!@#NOT_CHANGED^&*";

		// This constructor is used by model binding
		public Settings() { }

		public Settings(Site site) : base(site) 
		{
			if (String.IsNullOrEmpty(base.EncryptedPassword))
			{
				this.Password = "";
			}
		}

		public string Password { get; set; } = DUMMY_PASSWORD;
	}
}
