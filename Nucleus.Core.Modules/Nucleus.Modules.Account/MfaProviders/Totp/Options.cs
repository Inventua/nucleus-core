// THIS FILE IS NOT IN USE
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Nucleus.Modules.Account.MfaProviders.Totp;

//public class Options: IMfaProviderOptions
//{
//  /// <summary>
//  /// Secret key for multifactor authentication using TOTP (RFC6238).
//  /// </summary>
//  public string EncryptedTotpSecretKey { get; set; }

//  /// <summary>
//  /// Algorithm used to encrypt the <see cref="EncryptedTotpSecretKey"/>.
//  /// </summary>
//  public string TotpSecretKeyEncryptionAlgorithm { get; set; }

//  /// <summary>
//  /// Determines how many numeric characters are in the one-time passcode.
//  /// </summary>
//  public int TotpDigits { get; set; }

//  /// <summary>
//  /// Number of seconds that a one-time passcode is valid for.
//  /// </summary>
//  public int TotpPeriod { get; set; }

//  ////internal string GetSecretKey()
//  ////{
//  ////  // Decrypt EncryptedTotpSecretKey
//  ////}

//  public void GenerateSecretKey()
//  {

//  }
//}
