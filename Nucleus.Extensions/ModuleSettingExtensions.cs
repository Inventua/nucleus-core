﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nucleus.Abstractions.Models;

namespace Nucleus.Extensions;

/// <summary>
/// Extension methods for getting and setting Module settings.
/// </summary>
public static class ModuleSettingsExtensions
{
  /// <summary>
  /// Gets the module setting value specified by <paramref name="key"/> or returns <paramref name="defaultValue"/> if the key is not present.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="defaultValue"></param>
  /// <returns>string</returns>
  public static string Get(this List<ModuleSetting> settings, string key, string defaultValue)
  {
    ModuleSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    return value == null ? defaultValue : value.SettingValue;
  }

  /// <summary>
  /// Gets the module setting value specified by <paramref name="key"/> or returns <paramref name="defaultValue"/> if the key is not present.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="defaultValue"></param>
  /// <returns>Guid</returns>
  public static Guid Get(this List<ModuleSetting> settings, string key, Guid defaultValue)
  {
    ModuleSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (value == null)
    {
      return defaultValue;
    }
    else
    {
      if (Guid.TryParse(value.SettingValue, out Guid result))
      {
        return result;
      }
      else
      {
        return defaultValue;
      }
    }
  }

  /// <summary>
  /// Gets the module setting value specified by <paramref name="key"/> or returns <paramref name="defaultValue"/> if the key is not present.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="defaultValue"></param>
  /// <returns>Guid?</returns>
  public static Guid? Get(this List<ModuleSetting> settings, string key, Guid? defaultValue)
  {
    ModuleSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (value == null)
    {
      return defaultValue;
    }
    else
    {
      if (Guid.TryParse(value.SettingValue, out Guid result))
      {
        return result;
      }
      else
      {
        return defaultValue;
      }
    }
  }

  /// <summary>
  /// Gets the module setting value specified by <paramref name="key"/> or returns <paramref name="defaultValue"/> if the key is not present.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="defaultValue"></param>
  /// <returns></returns>
  public static DateTime? Get(this List<ModuleSetting> settings, string key, DateTime? defaultValue)
  {
    ModuleSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (value == null)
    {
      return defaultValue;
    }
    else
    {
      if (DateTime.TryParse(value.SettingValue, out DateTime result))
      {
        return result;
      }
      else
      {
        return defaultValue;
      }
    }
  }

  /// <summary>
  /// Gets the module setting value specified by <paramref name="key"/> or returns <paramref name="defaultValue"/> if the key is not present.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="defaultValue"></param>
  /// <returns></returns>
  public static int Get(this List<ModuleSetting> settings, string key, int defaultValue)
  {
    ModuleSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();

    if (value == null) return defaultValue;

    if (int.TryParse(value.SettingValue, out int result))
    {
      return result;
    }
    else
    {
      return defaultValue;
    }
  }

  /// <summary>
  /// Gets the module setting value specified by <paramref name="key"/> or returns <paramref name="defaultValue"/> if the key is not present.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="defaultValue"></param>
  /// <returns></returns>
  public static double Get(this List<ModuleSetting> settings, string key, double defaultValue)
  {
    ModuleSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();

    if (value == null) return defaultValue;

    if (double.TryParse(value.SettingValue, out double result))
    {
      return result;
    }
    else
    {
      return defaultValue;
    }
  }

  /// <summary>
  /// Gets the module setting value specified by <paramref name="key"/> or returns <paramref name="defaultValue"/> if the key is not present.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="defaultValue"></param>
  /// <returns></returns>
  public static Boolean Get(this List<ModuleSetting> settings, string key, Boolean defaultValue)
  {
    ModuleSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();

    if (value == null) return defaultValue;

    if (Boolean.TryParse(value.SettingValue, out Boolean result))
    {
      return result;
    }
    else
    {
      return defaultValue;
    }
  }

  /// <summary>
  /// Gets the module setting value specified by <paramref name="key"/> or returns <paramref name="defaultValue"/> if the key is not present.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="defaultValue"></param>
  /// <typeparam name="T">Type to return.</typeparam>
  /// <returns></returns>
  public static T Get<T>(this List<ModuleSetting> settings, string key, T defaultValue) where T : Enum
  {
    ModuleSetting value = settings.Where(setting => setting.SettingName == key).FirstOrDefault();

    if (value == null) return defaultValue;

    if (System.Enum.TryParse(typeof(T), value.SettingValue, out object result))
    {
      return (T)result;
    }
    else
    {
      return defaultValue;
    }
  }

  /// <summary>
  /// Adds or replaces the value for the specified key.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(this List<ModuleSetting> settings, string key, string value)
  {
    ModuleSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (existing != null)
    {
      existing.SettingValue = value;
    }
    else
    {
      settings.Add(new ModuleSetting() { SettingName = key, SettingValue = value });
    }
  }

  /// <summary>
  /// Adds or replaces the value for the specified key.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(this List<ModuleSetting> settings, string key, Boolean? value)
  {
    ModuleSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (existing != null)
    {
      existing.SettingValue = value.ToString();
    }
    else
    {
      settings.Add(new ModuleSetting() { SettingName = key, SettingValue = value.ToString() });
    }
  }

  /// <summary>
  /// Adds or replaces the value for the specified key.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(this List<ModuleSetting> settings, string key, int? value)
  {
    ModuleSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (existing != null)
    {
      existing.SettingValue = value.ToString();
    }
    else
    {
      settings.Add(new ModuleSetting() { SettingName = key, SettingValue = value.ToString() });
    }
  }

  /// <summary>
  /// Adds or replaces the value for the specified key.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(this List<ModuleSetting> settings, string key, int value)
  {
    ModuleSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (existing != null)
    {
      existing.SettingValue = value.ToString();
    }
    else
    {
      settings.Add(new ModuleSetting() { SettingName = key, SettingValue = value.ToString() });
    }
  }

  /// <summary>
  /// Adds or replaces the value for the specified key.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(this List<ModuleSetting> settings, string key, Guid? value)
  {
    ModuleSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (existing != null)
    {
      existing.SettingValue = value.ToString();
    }
    else
    {
      settings.Add(new ModuleSetting() { SettingName = key, SettingValue = value.ToString() });
    }
  }

  /// <summary>
  /// Adds or replaces the value for the specified key.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(this List<ModuleSetting> settings, string key, DateTime? value)
  {
    string serializedValue = value.HasValue ? value.Value.ToString("O") : "";
    ModuleSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (existing != null)
    {
      existing.SettingValue = serializedValue;
    }
    else
    {
      settings.Add(new ModuleSetting() { SettingName = key, SettingValue = value.ToString() });
    }
  }

  /// <summary>
  /// Adds or replaces the value for the specified key.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void Set(this List<ModuleSetting> settings, string key, double value)
  {
    ModuleSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (existing != null)
    {
      existing.SettingValue = value.ToString();
    }
    else
    {
      settings.Add(new ModuleSetting() { SettingName = key, SettingValue = value.ToString() });
    }
  }

  /// <summary>
  /// Adds or replaces the value for the specified key.
  /// </summary>
  /// <param name="settings"></param>
  /// <param name="key"></param>
  /// <param name="value">The value of any enum type.</param>
  public static void Set(this List<ModuleSetting> settings, string key, Enum value)
  {
    ModuleSetting existing = settings.Where(setting => setting.SettingName == key).FirstOrDefault();
    if (existing != null)
    {
      existing.SettingValue = value.ToString();
    }
    else
    {
      settings.Add(new ModuleSetting() { SettingName = key, SettingValue = value.ToString() });
    }
  }
}
