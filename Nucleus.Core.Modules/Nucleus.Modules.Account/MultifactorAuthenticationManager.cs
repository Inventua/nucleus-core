using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Abstractions.Models;
using OtpNet;
using QRCoder;

namespace Nucleus.Modules.Account;

public class MultifactorAuthenticationManager
{
  /// <summary>
  /// Generates a one-time-password QR code to setup the authentication exchange.
  /// </summary>
  /// <param name="issuer"></param>
  /// <param name="viewModel"></param>
  /// <param name="loginUser"></param>
  /// <returns></returns>
  public string GenerateUserMFAQRCodeSetup(string issuer, User loginUser)
  {
    // Add both the issuer label prefix and issuer parameter for backward compatibility as older Google Authenticator implementations
    // ignored the issuer paramaters while the newer versions support it according RFC2289. 
    // Format: "otpauth://totp/{issuer}:{loginUser.UserName}?secret={loginUser.Secrets.TotpSecretKey}&issuer={issuer}&algorithm={loginUser.Secrets.TotpSecretKeyAlgorithm}&digits={loginUser.Secrets.TotpDigits}&period={loginUser.Secrets.TotpPeriod}"
    string totpSecretKey = loginUser.Secrets.EncryptedTotpSecretKey;

    // Creates url format as "otpauth://totp/{issuerName}:{userName}?secret={secret}&issuer={issuerName}" and correctly encodes the values
    QRCoder.PayloadGenerator.OneTimePassword generator = new()
    {
      Secret = totpSecretKey,
      AuthAlgorithm = PayloadGenerator.OneTimePassword.OneTimePasswordAuthAlgorithm.SHA1,
      Issuer = issuer,
      Label = loginUser.UserName,
      Digits = loginUser.Secrets.TotpDigits,
      Period = loginUser.Secrets.TotpPeriod
    };

    QRCoder.QRCodeGenerator qrGenerator = new QRCodeGenerator();
    QRCodeData qrCodeData = qrGenerator.CreateQrCode(generator.ToString(), QRCodeGenerator.ECCLevel.M);
    SvgQRCode qrCode = new SvgQRCode(qrCodeData);

    return qrCode.GetGraphic(new System.Drawing.Size(150, 150), sizingMode: SvgQRCode.SizingMode.WidthHeightAttribute);
  }

  public Boolean VerifyTOTP(string userName, string oneTimePassword, string secretKey, int totpDigits, int totpPeriod, int previousTimeStepDelay, int futureTimeStepDelay)
  {

    byte[] secret = Base32Encoding.ToBytes(secretKey);

    Totp totp = new(secret, step: totpPeriod, mode: OtpHashMode.Sha1, totpSize: totpDigits);
    VerificationWindow window = new(previous: previousTimeStepDelay, future: futureTimeStepDelay);

    string result = totp.ComputeTotp(); // Defaults to DateTime.UtcNow

    Boolean isValid = totp.VerifyTotp(oneTimePassword, out long timeWindowUsed, window);

    //if (isValid)
    //{
    //  this.Logger.LogInformation("User '{userName}' OTP verified. Time window: {timeWindowUsed}.", userName, timeWindowUsed);
    //}
    //else
    //{
    //  this.Logger.LogWarning("User '{userName}' OTP invalid.", userName);
    //}

    return isValid;


  }
}
