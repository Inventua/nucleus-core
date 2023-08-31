using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Abstractions.Models.Configuration;

/// <summary>
/// Class used to read authentication protocols from configuration files.
/// </summary>
public class AuthenticationProtocols : List<AuthenticationProtocol>
{
  /// <summary>
  /// Configuration file section path for authentication protocols.
  /// </summary>
  public const string Section = "Nucleus:AuthenticationProtocols";

}
