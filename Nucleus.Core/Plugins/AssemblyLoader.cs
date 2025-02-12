﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Nucleus.Abstractions.Models.Configuration;
using Nucleus.Extensions.Logging;

namespace Nucleus.Core.Plugins;

/// <summary>
/// The AssemblyLoader class contains methods used by Nucleus Core which are used to load Nucleus Core application (plugin/extension) 
/// assemblies.  Assemblies within extension folders are loaded into their own AssemblyLoadContext, which ensures that extensions
/// can include private assemblies in order to prevent included assemblies from interfering with Nucleus Core or other extensions.
/// </summary>
/// <internal>
/// The methods within this class are used by Nucleus Core.  Nucleus applications (plugins) would not typically need to use the methods
/// of the AssemblyLoader class.
/// </internal>
/// <remarks>
/// <see cref="https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/AssemblyLoadContext.ContextualReflection.md" />
/// </remarks>
public class AssemblyLoader
{
  private static System.Collections.Concurrent.ConcurrentDictionary<string, AssemblyLoadContext> ExtensionLoadContexts { get; } = new(StringComparer.OrdinalIgnoreCase);

  // TypeContextCache is <typeName,loadContextName> where typeName is the type's AssemblyQualifiedName and loadContextName is the key of an 
  // entry in ExtensionLoadContexts.
  private static System.Collections.Concurrent.ConcurrentDictionary<string, string> TypeContextCache { get; } = new(StringComparer.OrdinalIgnoreCase);

  private static List<Assembly> LoadedAssemblies { get; set; }

  private static Boolean isResolvingEventHooked = false;

  /// <summary>
  /// Load an assembly into the appropriate AssemblyLoadContext. 
  /// </summary>
  /// <param name="path">The full path and filename of the assembly to load.</param>
  /// <returns>The loaded assembly.</returns>
  /// <remarks>
  /// The LoadFrom method checks the path of the assembly and separates extension assemblies into their own AssemblyLoadContext.  This
  /// allows extensions to use their own version of a referenced assembly, if it is present in the extension's bin folder, otherwise use the version
  /// in the Nucleus application folder.  This method returns NULL if the assembly couldn't be loaded.  This is typically the
  /// case when a dll is present that is not a managed assembly.
  /// </remarks>
  private static Assembly LoadFrom(string path)
  {
    AssemblyLoadContext assemblyLoadContext;
    string extensionFolder = "";
    string pluginPath;
    // Hook the default assembly load context's Resolving event so that we can override it and load extension assemblies from the correct
    // AssemblyLoadContext. This event is only called if an initial attempt to load the assembly fails, which will be the case when the 
    // assembly in in a sub-folder of /Extensions.
    if (!isResolvingEventHooked)
    {
      AssemblyLoadContext.Default.Resolving += HandleResolving;
      isResolvingEventHooked = true;
    }

    // Determine whether the assembly is located within an extension folder
    pluginPath = System.IO.Path.GetDirectoryName(path);

    extensionFolder = GetExtensionFolderName(pluginPath);

    // special case:  If a file named disable-private-loadcontext.json exists in the extension root, load the extension's assemblies into the
    // default context.  This was a special workaround for cases where .net components do not work properly in a private load context.
    // https://github.com/dotnet/runtime/issues/1388		 https://github.com/dotnet/runtime/pull/58932
    // The original issue in the XmlSerializer was fixed for .NET 6, and this option is no longer in use by any modules, but remains present in case
    // it is needed for some reason in the future. 
    if (System.IO.File.Exists(System.IO.Path.Combine(Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER, extensionFolder, "disable-private-loadcontext.json")))
    {
      extensionFolder = "";
    }

    // Retrieve or create an assembly load context for the specified extension folder, or use the Default assembly load context if the 
    // assembly is not located in an extension folder.
    if (String.IsNullOrEmpty(extensionFolder))
    {
      assemblyLoadContext = AssemblyLoadContext.Default;
    }
    else
    {
      if (ExtensionLoadContexts.TryGetValue(extensionFolder, out AssemblyLoadContext value))
      {
        assemblyLoadContext = value;
      }
      else
      {
        assemblyLoadContext = new PluginLoadContext(extensionFolder, pluginPath);
        ExtensionLoadContexts.TryAdd(extensionFolder, assemblyLoadContext);
      }
    }

    // Load the assembly
    try
    {
      // exclude anything in the bin/runtimes/**/native folder, these are not .net assemblies
      string[] assemblyPathParts = System.IO.Path.GetDirectoryName(path).Split(['/', '\\']);

      if (assemblyPathParts.Contains("runtimes", StringComparer.OrdinalIgnoreCase) && assemblyPathParts.Last()?.Equals("native", StringComparison.OrdinalIgnoreCase) == true)
      {
        return null;
      }
      else
      {
        // Microsoft design documentation on assembly load contexts is here:
        // https://github.com/dotnet/runtime/blob/main/docs/design/features/AssemblyLoadContext.ContextualReflection.md

        // The code in the HandleResolving event handler (below) is important, as it resolves assemblies from extension assembly load contexts
        // properly when the Nucleus.Core.Layout.ModuleContentRenderer calls actionInvoker.InvokeAsync().  Without the code in the handler, this
        // throws a System.IO.FileNotFoundException: Could not load file or assembly ... exception, presumably because .InvokeAsync 
        // uses one of the Activator classes or one of the Assembly.Load methods which don't work properly with assembly load contexts.  The code in 
        // HandleResolving ignores the requested assembly load context (which is .Default) and uses the module assembly load context instead.
        return assemblyLoadContext.LoadFromAssemblyPath(path);
      }
    }
    catch
    {
      // not a managed assembly
      return null;
    }
  }

  ////private static Boolean IsProtectedAssembly(string assemblyPath)
  ////{
  ////	string[] protectedAssemblies = 
  ////	{ 
  ////		nameof(Nucleus.Abstractions),
  ////		nameof(Nucleus.Data.Common),
  ////		nameof(Nucleus.Data.Sqlite),
  ////		nameof(Nucleus.Extensions),
  ////		nameof(Nucleus.Core),
  ////		"Nucleus.ViewFeatures"
  ////	};

  ////	string assemblyFileName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath);

  ////	return protectedAssemblies.Contains(assemblyFileName, StringComparer.OrdinalIgnoreCase);
  ////}

  /// <summary>
  /// Return the extension folder (the folder immediately following "extensions") from a full path.
  /// </summary>
  /// <param name="fullPath"></param>
  /// <returns></returns>
  internal static string GetExtensionFolderName(string fullPath)
  {
    if (fullPath.Contains(Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER))
    {
      string workingDirectory = fullPath;
      string workingDirectoryParent;

      while (!String.IsNullOrEmpty(workingDirectory))
      {
        workingDirectoryParent = System.IO.Path.GetDirectoryName(workingDirectory);
        if (System.IO.Path.GetFileName(workingDirectoryParent).Equals(Nucleus.Abstractions.Models.Configuration.FolderOptions.EXTENSIONS_FOLDER, StringComparison.OrdinalIgnoreCase))
        {
          return System.IO.Path.GetFileName(workingDirectory);
        }

        workingDirectory = workingDirectoryParent;
      }
    }

    return "";
  }

  /// <summary>
  /// Handle the assembly resolving event.  If the specified context is AssemblyLoadContext.Default, check the
  /// AssemblyLoadContext.CurrentContextualReflectionContext and if it is not null, use the
  /// CurrentContextualReflectionContext to load the requested assembly. 
  /// </summary>
  /// <param name="context">Request assembly load context.</param>
  /// <param name="assemblyName">Requested assembly.</param>
  /// <remarks>
  /// This code resolves extension assemblies from the PluginLoadContext when the Nucleus.Core.Layout.ModuleContentRenderer 
  /// calls actionInvoker.InvokeAsync().  Without this handler, .InvokeAsync throws "System.IO.FileNotFoundException: Could 
  /// not load file or assembly" because it is using one of the assembly loading methods that are not supported 
  /// by AssemblyLoadContext - like GetType(string typeName), when typeName includes a assembly-qualified type 
  /// reference.  If the requested context isn't .Default or the current context is null or Default,
  /// this code does nothing (returns null) and allows .net to do the work as normal.
  /// </remarks>
  /// <returns></returns>
  private static Assembly HandleResolving(AssemblyLoadContext context, AssemblyName assemblyName)
  {
    if (context == AssemblyLoadContext.Default)
    {
      if (AssemblyLoadContext.CurrentContextualReflectionContext != null)
      {
        return AssemblyLoadContext.CurrentContextualReflectionContext.Assemblies
          .Where(asm => Compare(asm.GetName(), assemblyName))
          .FirstOrDefault();
      }
      else
      {
        foreach (AssemblyLoadContext moduleContext in ExtensionLoadContexts.Values)
        {
          Assembly asm = moduleContext.Assemblies
            .Where(asm => Compare(asm.GetName(), assemblyName))
            .FirstOrDefault();
          if (asm != null) return asm;
        }
      }
    }

    // returning null from HandleResolving lets other handlers try to find the assembly			
    return null;
  }

  private static Boolean Compare(AssemblyName assemblyName1, AssemblyName assemblyName2)
  {
    return assemblyName1.Name == assemblyName2.Name &&
      (
        (assemblyName1.Version == null || assemblyName2.Version == null) ||
        assemblyName1.Version == assemblyName2.Version
      );
  }

  public static AssemblyLoadContext.ContextualReflectionScope EnterExtensionContext(string typeName)
  {
    return EnterExtensionContext(null, typeName);
  }

  /// <summary>
  /// Retrieves the AssemblyLoadContext for the specified type and sets the CurrentContextualReflectionContext to it.
  /// </summary>
  /// <param name="extensionName"></param>
  /// <param name="typeName"></param>
  /// <returns></returns>
  /// <remarks>
  /// For assemblies located in an extension folder, the ContextualReflectionScope for the extension is set to the 
  /// AssemblyLoadContext for the extension.  For assemblies which are located elsewhere, the Default AssemblyLoadContext
  /// is set.
  /// This method should be called with a using statement.  When the CurrentContextualReflectionContext is set, calls to Type.GetType
  /// and other type-resolving functions use the CurrentContextualReflectionContext.  When the ContextualReflectionScope is
  /// disposed, the CurrentContextualReflectionContext is reset back to the default AssemblyLoadContext.
  /// </remarks>
  /// <example>
  /// using (System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope = Nucleus.Core.Plugins.AssemblyLoader.EnterExtensionContext(actionDescriptor.ControllerTypeInfo.AssemblyQualifiedName))
  ///	{
  ///		moduleControllerTypeInfo = Type.GetType(moduleinfo.ModuleDefinition.ClassTypeName).GetTypeInfo();
  ///	}
  /// </example>
  public static AssemblyLoadContext.ContextualReflectionScope EnterExtensionContext(string extensionName, string typeName)
  {
    AssemblyLoadContext.ContextualReflectionScope scope;

    if (TypeContextCache.TryGetValue(typeName, out string assemblyLoadContextName))
    {
      if (assemblyLoadContextName == AssemblyLoadContext.Default.Name)
      {
        return AssemblyLoadContext.Default.EnterContextualReflection();
      }
      else
      {
        if (ExtensionLoadContexts.TryGetValue(assemblyLoadContextName, out AssemblyLoadContext assemblyLoadContext))
        {
          return assemblyLoadContext.EnterContextualReflection();
        }
      }
    }

    // If the type was not found in the cache, try to find it in a assembly load context.

    // if the type is available from the default assembly load context, use it
    if (TryAssemblyLoadContext(System.Runtime.Loader.AssemblyLoadContext.Default, typeName, out scope))
    {
      return scope;
    }

    // prefer the supplied extension's assembly load context, if specified
    if (extensionName != null)
    {
      if (ExtensionLoadContexts.TryGetValue(extensionName, out AssemblyLoadContext context))
      {
        if (TryAssemblyLoadContext(context, typeName, out scope))
        {
          return scope;
        }
      }
    }

    // loop through the ModuleLoadContexts until we find the type 
    foreach (AssemblyLoadContext context in ExtensionLoadContexts.Values)
    {
      if (TryAssemblyLoadContext(context, typeName, out scope))
      {
        return scope;
      }
    }

    // this is a catch-all.  There are no known scenarios where this code should never be reached, as the requested type should
    // be in an assembly which is in the default assembly load context, or one of the extension assembly load contexts.
    return System.Runtime.Loader.AssemblyLoadContext.Default.EnterContextualReflection();

  }

  private static Boolean TryAssemblyLoadContext(AssemblyLoadContext context, string typeName, out System.Runtime.Loader.AssemblyLoadContext.ContextualReflectionScope scope)
  {
    scope = context.EnterContextualReflection();

    Type type = Type.GetType(typeName);
    if (type != null)
    {
      if (AssemblyLoadContext.CurrentContextualReflectionContext.Assemblies.Contains(type.Assembly))
      {
        TypeContextCache.TryAdd(typeName, context.Name);
      }
      return true;      
    }

    scope.Dispose();
    return false;
  }

  /// <summary>
  /// Unload all AssemblyLoadContexts.
  /// </summary>
  /// <remarks>
  /// This method is used to unload extension AssemblyLoadContexts in order to complete the installation or upgrade of an extension.
  /// Note: At present, unloading AssemblyLoadContexts does not work.
  /// </remarks>
  public static void UnloadAll()
  {
    LoadedAssemblies?.Clear();
    TypeContextCache.Clear();

    string extensionsFolder = Nucleus.Abstractions.Models.Configuration.FolderOptions.GetExtensionsFolderStatic(false);

    if (System.IO.Directory.Exists(extensionsFolder))
    {
      foreach (string path in System.IO.Directory.GetDirectories(extensionsFolder))
      {
        UnloadPlugin(GetExtensionFolderName(path));
      }
    }

    ExtensionLoadContexts.Clear();

    GC.Collect();
    GC.WaitForPendingFinalizers();
  }

  /// <summary>
  /// Unload the AssemblyLoadContext for the specified extension.
  /// </summary>
  /// <param name="pluginName"></param>
  /// <remarks>
  /// At present, unloading plugins does not work.
  /// </remarks>
  private static void UnloadPlugin(string pluginName)
  {
    if (ExtensionLoadContexts.TryGetValue(pluginName, out AssemblyLoadContext context))
    {
      ExtensionLoadContexts.Remove(pluginName, out AssemblyLoadContext _);

      context.Unload();

      GC.Collect();
      GC.WaitForPendingFinalizers();
    }
  }

  internal static string[] EXCLUDED_ASSSEMBLY_PATHS = { "BlazorDebugProxy", "refs" };

  internal static Boolean IsExcludedAssembly(string assemblyPath)
  {
    string assemblySubFolder = System.IO.Path.GetDirectoryName(assemblyPath).Split(['/', '\\']).LastOrDefault();
    return EXCLUDED_ASSSEMBLY_PATHS.Contains(assemblySubFolder);
  }

  internal static Boolean IsExcludedAssembly(Assembly assembly)
  {
    return IsExcludedAssembly(assembly.Location);
  }

  /// <summary>
  /// Enumerate all Assemblies (dlls) including core assemblies and plugin (extension) assemblies.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// This method returns a list of filenames in /bin and /{Extensions}/** with the .dll extension.  Not all dlls are assemblies.  Callers
  /// should implement checking or exception handling for cases where a returned dll file is not an assembly.
  /// </remarks>
  private static IEnumerable<string> EnumerateAssemblyNames()
  {
    // assemblies (dlls) in /bin must be returned first, so that if an assembly/version exists in both /bin and in an extension/bin folder, the 
    // "core" bin assembly is added to the type cache & gets used in preference to the other one.

    // get all assemblies (dlls) in /bin
    string binariesFolder = System.IO.Path.GetDirectoryName(typeof(AssemblyLoader).Assembly.Location);
    foreach (string filename in System.IO.Directory.EnumerateFiles(binariesFolder, "*.dll", System.IO.SearchOption.TopDirectoryOnly))
    {
      if (!IsExcludedAssembly(filename))
      {
        yield return FolderOptions.NormalizePath(filename);
      }
    }

    // get all assemblies (dlls) in /bin/runtimes/**
    string runtimesFolder = System.IO.Path.Join(System.IO.Path.GetDirectoryName(typeof(AssemblyLoader).Assembly.Location), "runtimes");
    
    // platform-specific installs do not have a runtimes folder
    if (System.IO.Directory.Exists(runtimesFolder))
    {
      foreach (string filename in System.IO.Directory.EnumerateFiles(runtimesFolder, "*.dll", System.IO.SearchOption.AllDirectories))
      {
        if (!IsExcludedAssembly(filename))
        {
          yield return FolderOptions.NormalizePath(filename);
        }
      }
    }

    string extensionsFolder = Nucleus.Abstractions.Models.Configuration.FolderOptions.GetExtensionsFolderStatic(false);

    if (System.IO.Directory.Exists(extensionsFolder))
    {
      // get all extension assemblies (dlls) in /extensions/**
      foreach (string foldername in System.IO.Directory.EnumerateFiles(extensionsFolder, "*.dll", System.IO.SearchOption.AllDirectories))
      {
        yield return FolderOptions.NormalizePath(foldername);
      }
    }
  }

  /// <summary>
  /// Return a list of assemblies which have the assembly attribute specified by T.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <returns></returns>
  /// <remarks>
  /// A side effect of this function is that assemblies are loaded into their assembly load contexts.
  /// </remarks>
  internal static IEnumerable<Assembly> GetAssembliesWithAttribute<T>()
  {
    foreach (string assemblyFileName in EnumerateAssemblyNames())
    {
      Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

      if (assembly != null)
      {
        CustomAttributeData attributes = assembly.CustomAttributes
          .FirstOrDefault(assm => typeof(T).IsAssignableFrom(assm.AttributeType));

        if (attributes != null)
        {
          yield return assembly;
        }
      }
    }
  }

  /// <summary>
  /// Return a list of assemblies which implement the class specified by T.  
  /// </summary>
  /// <typeparam name="T">The type to search for.</typeparam>
  /// <param name="path">The content root path for Nucleus Core.</param>
  /// <returns></returns>
  /// <remarks>
  /// A side effect of this function is that assemblies are loaded into their assembly load contexts by <see cref="LoadAssemblies"/>.
  /// </remarks>
  internal static IEnumerable<Assembly> GetAssembliesImplementing<T>(ILogger logger)
  {
    foreach (Assembly assembly in ListAssemblies())
    {
      Type type;

      try
      {
        type = assembly.GetExportedTypes().FirstOrDefault
        (
          typ => !typ.IsAbstract && typeof(T).IsAssignableFrom(typ) && !typ.Equals(typeof(T))
        );
      }
      catch (System.Reflection.ReflectionTypeLoadException e)
      {
        logger?.LogError(e, "Unable to load assembly {0}", assembly.FullName);
        type = null;
      }
      catch (System.IO.FileNotFoundException e)
      {
        logger?.LogError(e, "Unable to load assembly {0}", assembly.FullName);
        type = null;
      }

      if (type != null)
      {
        yield return assembly;
      }
    }
  }

  /// <summary>
  /// Return a list of all types. 
  /// </summary>
  /// <returns>A list of .net types in the main application folder and in extension folders.</returns>
  public static IEnumerable<Type> GetTypes()
  {
    foreach (Assembly assembly in ListAssemblies())
    {
      if (assembly != null)
      {
        foreach (Type type in GetTypes(assembly))
        {
          yield return type;
        }
      }
    }
  }

  /// <summary>
  /// Return a list of types which implement the class specified by T, but are not T. 
  /// </summary>
  /// <typeparam name="T">The type to search for.</typeparam>
  /// <returns>A list of .Net types which implement T.</returns>
  /// <remarks>
  /// A side effect of this function is that assemblies are loaded into their assembly load contexts by <see cref="LoadAssemblies"/>.
  /// </remarks>
  public static IEnumerable<Type> GetTypes<T>()
  {
    foreach (Assembly assembly in ListAssemblies())
    {
      if (assembly != null)
      {
        foreach (Type type in GetTypes(assembly)
          .Where(type => !type.IsAbstract && typeof(T).IsAssignableFrom(type) && !type.Equals(typeof(T))))
        {
          yield return type;
        }
      }
    }
  }

  /// <summary>
  /// Return a list of types which have the specified attribute assigned. 
  /// </summary>
  /// <typeparam name="TAttribute">The System.Attribute type which is assigned to the returned classes.</typeparam>
  /// <returns>A list of .Net types which have the attribute specified by <typeparamref name="TAttribute"/>.</returns>
  public static IEnumerable<Type> GetTypesWithAttribute<TAttribute>()
    where TAttribute : Attribute
  {
    foreach (Assembly assembly in ListAssemblies())
    {
      if (assembly != null)
      {
        foreach (Type type in GetTypes(assembly)
          .Where(type => !type.IsAbstract && type.GetCustomAttributes<TAttribute>().Any()))
        {
          yield return type;
        }
      }
    }
  }

  /// <summary>
  /// Return a list of types in the specified <paramref name="assembly"/> which have the specified attribute assigned. 
  /// </summary>
  /// <typeparam name="TAttribute"></typeparam>
  /// <param name="assembly"></param>
  /// <returns></returns>
  public static IEnumerable<Type> GetTypesWithAttribute<TAttribute>(Assembly assembly)
    where TAttribute : Attribute
  {
    if (assembly != null)
    {
      foreach (Type type in GetTypes(assembly)
        .Where(type => !type.IsAbstract && type.GetCustomAttributes<TAttribute>().Any()))
      {
        yield return type;
      }
    }
  }

  private static Type[] GetTypes(System.Reflection.Assembly assembly)
  {
    try
    {
      return assembly.GetExportedTypes();
    }
    catch (System.Reflection.ReflectionTypeLoadException e)
    {
      // ReflectionTypeLoadException.Types contains a list of the types which were loaded
      return e.Types;
    }
  }

  /// <summary>
  /// List assemblies in /bin and in extension folders.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// A side effect of this function is a call to LoadAssemblies().  This loads assemblies into their AssemblyLoadContexts.  When 
  /// LoadAssemblies() is called, the result is cached in _loadedAssemblies to improve startup performance.
  /// </remarks>
  internal static IEnumerable<Assembly> ListAssemblies()
  {
    LoadedAssemblies ??= LoadAssemblies();//.Where(assembly => !IsExcludedAssembly(assembly)).ToList();
    return LoadedAssemblies;
  }

  /// <summary>
  /// Load assemblies in /bin and in extension folders into their assembly load contexts and return a list of loaded assemblies.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// For assemblies located in an extension folder, the assembly is loaded into an AssemblyLoadContext for the extension.  For 
  /// assemblies which are located elsewhere, the Default AssemblyLoadContext is used.
  /// </remarks>
  private static List<Assembly> LoadAssemblies()
  {
    List<Assembly> assemblies = [];

    // All assemblies must be loaded into their correct AssemblyLoadContexts in this function (don't use yield return).  This is to ensure that
    // assemblies which reference other assemblies (in an extension/bin folder) load them from the correct folder and using the correct
    // AssemmblyLoadContext.
    foreach (string assemblyFileName in EnumerateAssemblyNames())
    {
      Assembly assembly = AssemblyLoader.LoadFrom(assemblyFileName);

      if (assembly != null)
      {
        assemblies.Add(assembly);
      }
    }

    return assemblies;
  }

  public static AssemblyLoadContext[] ListExtensionLoadContexts()
  {
    return ExtensionLoadContexts.Values.ToArray();
  }

  public static ReadOnlyDictionary<string, string> ListTypeContextCache()
  {
    return new(TypeContextCache);
  }
}
