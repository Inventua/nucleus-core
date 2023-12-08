using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using SkiaSharp;

namespace Nucleus.Extensions.Configuration;

/// <summary>
/// Configuration file manager.
/// </summary>
public class ConfigurationFile
{
  /// <summary>
  /// Enum used to specify which configuration file to work with,
  /// </summary>
  public enum KnownConfigurationFiles
  {
    /// <summary>
    /// Specifies the application settings file.
    /// </summary>
    appSettings,
    /// <summary>
    /// Specifies the database settings file.
    /// </summary>
    databaseSettings
  }
    
  private string FilePath { get;  }
  private JObject Configuration { get;  }

  /// <summary>
  /// Opens the specified file.
  /// </summary>
  /// <param name="configurationFile"></param>
  /// <param name="environmentName"></param>
  public ConfigurationFile(KnownConfigurationFiles configurationFile, string environmentName)
  {
    this.FilePath = $"{configurationFile}.{environmentName}{(String.IsNullOrEmpty(environmentName) ? "" : ".")}json";
    JsonLoadSettings jsonSettings = new()
    {
      CommentHandling = CommentHandling.Load
    };

    if (System.IO.File.Exists(this.FilePath))
    {
      this.Configuration = JObject.Parse(System.IO.File.ReadAllText(this.FilePath), jsonSettings) as JObject;
    }
    else
    {
      this.Configuration = JObject.Parse("{}");
    }
  }

  /// <summary>
  /// Retrieve an existing section from the configuration file, or create the section if it does not exist.
  /// </summary>
  /// <param name="keys"></param>
  /// <returns></returns>
  public JContainer GetSection(params string[] keys)
  {
    JContainer section = null;

    foreach (string key in keys)
    {
      if (section == null)
      {
        JToken childSection = this.Configuration[key] as JContainer;
        if (childSection == null)
        {
          this.Configuration.Add(key, JToken.Parse("{}"));
          section = this.Configuration[key] as JContainer;
        }
        else
        {
          section = childSection as JObject;
        }
      }
      else
      {
        JToken childSection = section[key] as JContainer;
        if (childSection == null)
        {
          switch (section.Type)
          {
            case JTokenType.Object:
              (section as JObject).Add(key, JToken.Parse("{}"));
              break;

            default:
              throw new InvalidOperationException();              
          }
          
          section = this.Configuration[key] as JContainer;
        }
        else
        {
          section = childSection as JContainer;
        }
      }
    }

    return section;
  }

  /// <summary>
  /// Retrieve an existing section from the configuration file, or create the section if it does not exist.
  /// </summary>
  /// <param name="keys"></param>
  /// <returns></returns>
  public JArray GetArray(params string[] keys)
  {
    JContainer section = null;

    foreach (string key in keys)
    {
      if (section == null)
      {
        JToken childSection = this.Configuration[key] as JContainer;
        if (childSection == null)
        {
          if (key == keys.Last())
          {
            this.Configuration.Add(key, JToken.Parse("[]"));
          }
          else
          {
            this.Configuration.Add(key, JToken.Parse("{}"));
          }
          section = this.Configuration[key] as JContainer;
          if (section == null) throw new InvalidOperationException($"Failed to create node: {String.Join(",", keys)}");
        }
        else
        {
          section = childSection as JContainer;
        }
      }
      else
      {
        JToken childSection = section[key];
        if (childSection == null)
        {
          switch (section.Type)
          {
            case JTokenType.Object:
              if (key == keys.Last())
              {
                (section as JObject).Add(key, JToken.Parse("[]"));
              }
              else
              {
                (section as JObject).Add(key, JToken.Parse("{}"));
              }
              break;

            default:
              throw new InvalidOperationException();
          }

          section = section[key] as JContainer;
          if (section == null) throw new InvalidOperationException($"Failed to create node: {String.Join(",", keys)}");
        }
        else
        {
          section = childSection as JContainer;
        }
      }
    }

    return section as JArray;
  }

  /// <summary>
  /// Retrieve an existing section from the configuration file.
  /// </summary>
  /// <param name="key"></param>
  /// <returns></returns>
  public JObject GetSection(string key)
  {
    return this.Configuration[key] as JObject;
  }

  /// <summary>
  /// Retrieve an existing section from the configuration file, or create a new section if it does not exist.
  /// </summary>
  /// <param name="section"></param>
  /// <param name="key"></param>
  /// <returns></returns>
  public JContainer GetSection(JContainer section, string key)
  {
    JContainer result = section[key] as JContainer;

    if (result == null)
    {
      switch (section.Type)
      {
        case JTokenType.Object:
          (section as JObject).Add(key, JToken.Parse("{}"));
          break;

        default:
          throw new InvalidOperationException();
      }

      result = section[key] as JContainer;
      if (result == null) throw new InvalidOperationException($"Failed to create node: {section.Path}: {key}");
    }

    return result;
  }

  /// <summary>
  /// Retrieve an existing section from the configuration file, or create a new section if it does not exist.
  /// </summary>
  /// <param name="section"></param>
  /// <param name="key"></param>
  /// <returns></returns>
  public JObject GetObject(JObject section, string key)
  {
    JObject result = section[key] as JObject;

    if (result == null)
    {
      switch (section.Type)
      {
        case JTokenType.Object:
          (section as JObject).Add(key, JToken.Parse($"{{ '{key}': {{}} }}"));
          break;

        default:
          throw new InvalidOperationException();
      }

      result = section[key] as JObject;
    }

    return result;
  }

  /// <summary>
  /// Retrieve an existing section from the configuration file, or create a new section if it does not exist.
  /// </summary>
  /// <param name="section"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  public JObject GetObject(JArray section, string key, string value)
  {
    JObject result = section.Children().Where(item => item.SelectToken(key)?.Value<string>() == value).FirstOrDefault() as JObject; 

    if (result == null)
    { 
      section.Add(JToken.Parse($"{{'{key}': '{value}'}}"));
      result = section.Children().Where(item => item.SelectToken(key)?.Value<string>() == value).FirstOrDefault() as JObject;
      if (result == null) throw new InvalidOperationException($"Failed to create node: {section.Path}: {key}");
    }

    return result;
  }

  /// <summary>
  /// Retrieve a token from the specified section.
  /// </summary>
  /// <param name="path"></param>
  /// <returns></returns>
  public JToken GetToken(string path)
  {
    return this.Configuration.SelectToken(path);
  }


  /// <summary>
  /// Retrieve a token from the specified section.
  /// </summary>
  /// <param name="section"></param>
  /// <param name="key"></param>
  /// <returns></returns>
  public JToken GetToken(JObject section, string key)
  {
    return section.SelectToken(key);
  }

  /// <summary>
  /// Add or replace a value.
  /// </summary>
  /// <param name="section"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public void Set(JObject section, string key, object value)
  {
    string settingName = $"['{key}']";

    JToken settingToken = GetToken(section, settingName);

    if (settingToken == null)
    {
      section.Add(key, JToken.FromObject(value));
    }
    else
    {
      settingToken.Replace(value.ToString());
    }
  }

  /// <summary>
  /// Add or replace a value.
  /// </summary>
  /// <param name="section"></param>
  /// <param name="value"></param>
  public void Set(JArray section, object value)
  {    
    section.Add(JToken.FromObject(value));   
  }

  /// <summary>
  /// Remove an existing section from the configuration file if a match exists.
  /// </summary>
  /// <param name="section"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  /// <returns></returns>
  public JObject RemoveObject(JArray section, string key, string value)
  {
    JObject result = section.Children().Where(item => item.SelectToken(key)?.Value<string>() == value).FirstOrDefault() as JObject;

    if (result != null)
    {
      section.Remove(result);
    }

    return result;
  }

  /// <summary>
  /// Save changes.
  /// </summary>
  public void CommitChanges()
  {
    System.IO.File.WriteAllText(this.FilePath, this.Configuration.ToString());
  }
}
