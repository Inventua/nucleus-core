﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models;
using OtpNet;
using QRCoder;

namespace Nucleus.Modules.Account;

public class MultifactorAuthenticationManager
{
  public const int PREVIOUS_TIME_STEP_DELAY_DEFAULT = 1;
  public const int FUTURE_TIME_STEP_DELAY_DEFAULT = 1;


  /// <summary>
  /// Generates a one-time-password QR code to setup the authentication exchange.
  /// </summary>
  /// <param name="issuer"></param>
  /// <param name="viewModel"></param>
  /// <param name="loginUser"></param>
  /// <returns></returns>
  public string GenerateUserMFAQRCodeSetup(string issuer, string userName, string encryptedTotpSecretKey, string totpSecretKeyEncryptionAlgorithm, int totpDigits, int totpPeriod)
  {
    // Add both the issuer label prefix and issuer parameter for backward compatibility as older Google Authenticator implementations
    // ignored the issuer paramaters while the newer versions support it according RFC2289. 
    // Format: "otpauth://totp/{issuer}:{loginUser.UserName}?secret={loginUser.Secrets.TotpSecretKey}&issuer={issuer}&algorithm={loginUser.Secrets.TotpSecretKeyAlgorithm}&digits={loginUser.Secrets.TotpDigits}&period={loginUser.Secrets.TotpPeriod}"
    string totpSecretKey = encryptedTotpSecretKey;

    // Creates url format as "otpauth://totp/{issuerName}:{userName}?secret={secret}&issuer={issuerName}" and correctly encodes the values
    QRCoder.PayloadGenerator.OneTimePassword generator = new()
    {
      Secret = totpSecretKey,
      AuthAlgorithm = PayloadGenerator.OneTimePassword.OneTimePasswordAuthAlgorithm.SHA1,
      Issuer = issuer,
      Label = userName,
      Digits = totpDigits,
      Period = totpPeriod
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

    return totp.VerifyTotp(oneTimePassword, out long timeWindowUsed, window);
  }

}
