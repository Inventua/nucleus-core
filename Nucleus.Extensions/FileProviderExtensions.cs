using System.Threading.Tasks;

namespace Nucleus.Extensions;

/// <summary>
/// Extensions for file providers.
/// </summary>
public static class FileProviderExtensions
{
  /// <summary>
  /// Read and return the content of the specified <paramref name="fileInfo"/>.
  /// </summary>
  /// <param name="fileInfo"></param>
  /// <returns></returns>
  public static async Task<string> ReadAllText(this Microsoft.Extensions.FileProviders.IFileInfo fileInfo)
  {
    using (System.IO.StreamReader reader = new(fileInfo.CreateReadStream()))
    {
      return await reader.ReadToEndAsync();
    }
  }
}
