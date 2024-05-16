// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a copy from https://github.com/dotnet/aspnetcore/tree/da3aa27233a2cec2f6780884f71934b2f5e686ce/src/Shared/ThrowHelpers with
// no changes.  It is copied because the .net class is marked internal.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System;

namespace Nucleus.Core.FileProviders;

internal static partial class ArgumentNullThrowHelper
{
#nullable enable
  /// <summary>Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
  /// <param name="argument">The reference type argument to validate as non-null.</param>
  /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
  public static void ThrowIfNull(
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
      [NotNull]
#endif
        object? argument, [CallerArgumentExpression("argument")] string? paramName = null)
  {
#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
        if (argument is null)
        {
            Throw(paramName);
        }
#else
    ArgumentNullException.ThrowIfNull(argument, paramName);
#endif
  }

#if !NET7_0_OR_GREATER || NETSTANDARD || NETFRAMEWORK
#if INTERNAL_NULLABLE_ATTRIBUTES || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    [DoesNotReturn]
#endif
    internal static void Throw(string? paramName) =>
        throw new ArgumentNullException(paramName);
#endif
}
