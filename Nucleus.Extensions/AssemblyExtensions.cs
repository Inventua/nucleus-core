using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Nucleus.Extensions
{
	/// <summary>
	/// The AssemblyExtensions class extends the System.Assembly class and provides easy access to assembly attributes.
	/// </summary>
	public static class AssemblyExtensions
	{
		/// <summary>
		/// Gets the assembly product name.
		/// </summary>
		/// <param name="assembly">The <see href="https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly">Assembly</see> to read data from.</param>
		/// <returns type="string"></returns>
		public static string Product(this Assembly assembly)
		{
			AssemblyProductAttribute attr = assembly.GetCustomAttribute<AssemblyProductAttribute>();
			return (attr == null ? String.Empty : attr.Product);
		}

		/// <summary>
		/// Gets the assembly title.
		/// </summary>
		/// <param name="assembly">The <see href="https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly">Assembly</see> to read data from.</param>
		/// <returns></returns>
		public static string Title(this Assembly assembly)
		{
			AssemblyTitleAttribute attr = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
			return (attr == null ? String.Empty : attr.Title);
		}

		/// <summary>
		/// Gets the assembly company.
		/// </summary>
		/// <param name="assembly">The <see href="https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly">Assembly</see> to read data from.</param>
		/// <returns></returns>
		public static string Company(this Assembly assembly)
		{
			AssemblyCompanyAttribute attr = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
			return (attr == null ? String.Empty : attr.Company);
		}

		/// <summary>
		/// Gets the assembly version.
		/// </summary>
		/// <param name="assembly">The <see href="https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly">Assembly</see> to read data from.</param>
		/// <returns></returns>
		public static string Version(this Assembly assembly)
		{
      AssemblyInformationalVersionAttribute attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
      return System.Text.RegularExpressions.Regex.Match(attr?.InformationalVersion ?? "", "(?<version>[^+]*)")?.Value ?? String.Empty;
      // return (attr == null ? String.Empty : attr.InformationalVersion);
    }

		/// <summary>
		/// Gets the assembly copyright.
		/// </summary>
		/// <param name="assembly">The <see href="https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly">Assembly</see> to read data from.</param>
		/// <returns></returns>
		public static string Copyright(this Assembly assembly)
		{
			AssemblyCopyrightAttribute attr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
			return (attr == null ? String.Empty : attr.Copyright);
		}

    /// <summary>
		/// Gets the assembly name and version.
		/// </summary>
		/// <param name="assembly">The <see href="https://docs.microsoft.com/en-us/dotnet/api/system.reflection.assembly">Assembly</see> to read data from.</param>
		/// <returns></returns>
    /// <remarks>
    /// This value is typically used in application log messages.
    /// </remarks>
		public static string ShortName(this Assembly assembly)
    {
      return ($"{assembly.ManifestModule} version {assembly.Version()}");
    }

    /// <summary>
    /// Returns the assembly name, version and relative location.
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    /// This value is typically used in application log messages.
    public static string LogName(this Assembly assembly)
    {
      return $"{assembly.ShortName()} [{System.IO.Path.GetRelativePath(Environment.CurrentDirectory, assembly.Location)}]";
    }
  }
}
