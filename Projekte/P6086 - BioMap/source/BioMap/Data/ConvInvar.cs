﻿using System;

namespace BioMap
{
  public static class ConvInvar
  {
    public static int ToInt(string value,int nDefVal=0)
    {
      return string.IsNullOrEmpty(value)?nDefVal:Convert.ToInt32(value, System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    public static float ToFloat(string value, float nDefVal = 0)
    {
      return string.IsNullOrEmpty(value) ? nDefVal : Convert.ToSingle(value, System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    public static double ToDouble(string value, double nDefVal = 0)
    {
      return string.IsNullOrEmpty(value) ? nDefVal : Convert.ToDouble(value, System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    public static string ToString(int value)
    {
      return Convert.ToString(value, System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    public static string ToString(float value)
    {
      return Convert.ToString(value, System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    public static string ToString(double value)
    {
      return Convert.ToString(value, System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    public static string ToString(DateTime value)
    {
      return value.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
  }
}