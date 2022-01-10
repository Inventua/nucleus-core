//using System;
//using System.Diagnostics;
//using System.Data;

//namespace Nucleus.DataProviders.Abstractions
//{
//  /// <summary>
//  /// Helper methods to retrieve values from an IDataReader.
//  /// </summary>
//  public static class DataHelper
//  {
//    public static string GetString(IDataReader objReader, string FieldName)
//    {
//      int intIndex;

//      if (FieldExists(objReader, FieldName))
//      {
//        intIndex = objReader.GetOrdinal(FieldName);
//        if (objReader.IsDBNull(intIndex))
//          return "";
//        else
//          return objReader.GetString(intIndex);
//      }
//      else
//        return "";
//    }

//    public static Guid GetGUID(IDataReader objReader, string FieldName)
//    {
//      int intIndex;

//      if (FieldExists(objReader, FieldName))
//      {
//        intIndex = objReader.GetOrdinal(FieldName);
//        if (objReader.IsDBNull(intIndex))
//          return Guid.Empty;
//        else
//          return objReader.GetGuid(intIndex);
//      }
//      else
//        return Guid.Empty;
//    }

//    public static bool GetBoolean(IDataReader objReader, string FieldName)
//    {
//      int intIndex;

//      if (FieldExists(objReader, FieldName))
//      {
//        intIndex = objReader.GetOrdinal(FieldName);
//        if (objReader.IsDBNull(intIndex))
//          return false;
//        else
//          return objReader.GetBoolean(intIndex);
//      }
//      else
//        return false;
//    }

//    public static double GetDouble(IDataReader objReader, string FieldName)
//    {
//      int intIndex;

//      if (FieldExists(objReader, FieldName))
//      {
//        intIndex = objReader.GetOrdinal(FieldName);
//        if (objReader.IsDBNull(intIndex))
//          return 0;
//        else
//          return objReader.GetDouble(intIndex);
//      }
//      else
//        return 0;
//    }

//    public static int GetInteger(IDataReader objReader, string FieldName, int DefaultValue = 0)
//    {
//      int intIndex;

//      if (FieldExists(objReader, FieldName))
//      {
//        intIndex = objReader.GetOrdinal(FieldName);
//        if (objReader.IsDBNull(intIndex))
//          return -1;
//        else
//          return objReader.GetInt32(intIndex);
//      }
//      else
//        return DefaultValue;
//    }

//    public static long GetLong(IDataReader objReader, string FieldName, long DefaultValue = 0)
//    {
//      int intIndex;

//      if (FieldExists(objReader, FieldName))
//      {
//        intIndex = objReader.GetOrdinal(FieldName);
//        if (objReader.IsDBNull(intIndex))
//          return -1;
//        else
//          return objReader.GetInt64(intIndex);
//      }
//      else
//        return DefaultValue;
//    }

//    public static DateTime GetDateTime(IDataReader objReader, string FieldName)
//    {
//      int intIndex;

//      if (FieldExists(objReader, FieldName))
//      {
//        intIndex = objReader.GetOrdinal(FieldName);
//        if (objReader.IsDBNull(intIndex))
//          return new DateTime();
//        else
//          // '	Return ConvertDateTime(objReader.GetInt64(intIndex))
//          return objReader.GetDateTime(intIndex);
//      }
//      else
//        return new DateTime();
//    }

//    public static Boolean IsNull(IDataReader objReader, string FieldName)
//    {
//      int intIndex;

//      if (FieldExists(objReader, FieldName))
//      {
//        intIndex = objReader.GetOrdinal(FieldName);
//        if (objReader.IsDBNull(intIndex))
//          return true;
//        else
//          return false;
//      }

//      return true;
//    }

//    [DebuggerStepThrough()]
//    public static bool FieldExists(IDataReader objReader, string FieldName)
//    {
//      for (int intCount = 0; intCount <= objReader.FieldCount - 1; intCount++)
//      {
//        if (objReader.GetName(intCount) == FieldName)
//          return true;
//      }
      
//      return false;
//    }
//  }
//}
