using System.Runtime.Loader;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace Nucleus.WebAssembly;

public class Program
{
  public static async Task Main(string[] args)
  {
    WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

    // create a httpClient so that we can download assemblies
    HttpClient httpClient = new() { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

    // add the httpClient to the services container so that other WebAssembly components can use it to call Api methods, etc
    builder.Services.AddScoped<HttpClient>(services => httpClient);
    
    // Wasm Runtime assemblies are added using the Nucleus.ViewFeatures.HtmlHelpers.AddWebAssemblyHtmlHelper helper.  This emits javascript 
    // which calls Page.AddWasmRuntimeAssemblies (in nucleus-shared.js) to populate the private Page._WasmRuntimeAssemblies property, which is 
    // returned by the Page.WasmRuntimeAssemblies() function.
    IJSInProcessRuntime jsRuntime = (IJSInProcessRuntime)builder.Services.BuildServiceProvider().GetRequiredService<IJSRuntime>();
    string[]? assemblies = await jsRuntime.InvokeAsync<string[]>("window.Page.WasmRuntimeAssemblies");
    if (assemblies?.Any() == true)
    {
      await LoadExtensionAssemblies(httpClient, assemblies);
    }

    await builder.Build().RunAsync();
  }

  /// <summary>
  /// Load web assemblies from the server.
  /// </summary>
  /// <param name="httpClient"></param>
  /// <param name="assemblies"></param>
  /// <returns></returns>
  private static async Task LoadExtensionAssemblies(HttpClient httpClient, string[] assemblies)
  {
    foreach (string assembly in assemblies)
    {
      // https://www.reddit.com/r/Blazor/comments/12kzkr6/dynamically_load_dll/
      AssemblyLoadContext.Default.LoadFromStream(await httpClient.GetStreamAsync(assembly));
    }
  }
}