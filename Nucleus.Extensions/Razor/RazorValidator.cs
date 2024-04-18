using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Vml;
using RazorEngineCore;
using static Nucleus.Extensions.Razor.RazorParser;

namespace Nucleus.Extensions.Razor;

/// <summary>
/// Razor script validation methods for Razor Engine.
/// </summary>
internal class RazorValidator : RazorEngineCore.RazorEngine
{
  public RazorValidatorResult TestCompile(Type modelType, string content, Action<IRazorEngineCompilationOptionsBuilder> builderAction = null, CancellationToken cancellationToken = default) 
  {
    Type desiredType = typeof(RazorEngineTemplate<>).MakeGenericType(modelType);

    IRazorEngineCompilationOptionsBuilder compilationOptionsBuilder = new RazorEngineCompilationOptionsBuilder();
    compilationOptionsBuilder.AddAssemblyReference(desiredType.Assembly);
    compilationOptionsBuilder.Inherits(desiredType);

    builderAction?.Invoke(compilationOptionsBuilder);

    try
    {
      RazorEngineCompiledTemplateMeta meta = this.CreateAndCompileToStream(content, compilationOptionsBuilder.Options, cancellationToken);
    }
    catch (RazorEngineCompilationException ex)
    {
      return new RazorValidatorResult
      (
        false, 
        ex.Errors.Select(err =>
          $"<p>The template has an error near: '{GetSource(ex.GeneratedCode, err)}'.</p>" +
          $"<p>{err.GetMessage()}</p>" +
          (String.IsNullOrEmpty(err.Descriptor.HelpLinkUri) ? "" : $"<a target='_blank' href={err.Descriptor.HelpLinkUri}>Help for error {err.Descriptor.Id}</a>"))
      );
    }

    return new RazorValidatorResult(true);
  }

  public Task<RazorValidatorResult> TestCompileAsync(Type modelType, string content, Action<IRazorEngineCompilationOptionsBuilder> builderAction = null, CancellationToken cancellationToken = default)
  {
    return Task.Run(() => this.TestCompile(modelType, content: content, builderAction: builderAction, cancellationToken: cancellationToken));
  }

  private static string GetSource(string code, Microsoft.CodeAnalysis.Diagnostic error)
  {
    string[] codeLines = code.Split(Environment.NewLine);

    Microsoft.CodeAnalysis.FileLinePositionSpan location = error.Location.GetLineSpan();

    if (location.EndLinePosition.Line != location.StartLinePosition.Line)
    {
      // problem source spans multiple lines (rare case)
      return String.Join(" ", codeLines
        .Skip(location.StartLinePosition.Line)
        .Take(location.EndLinePosition.Line - location.StartLinePosition.Line + 1));
    }
    else
    {
      // problem source is a single line of code
      return codeLines[location.StartLinePosition.Line][location.StartLinePosition.Character..location.EndLinePosition.Character];
    }    
  }
}
