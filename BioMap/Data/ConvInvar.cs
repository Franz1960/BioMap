using System;

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
    public static string ToString(float value) {
      return Convert.ToString(value,System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    public static string ToString(double value) {
      return Convert.ToString(value,System.Globalization.NumberFormatInfo.InvariantInfo);
    }
    /// <summary>
    /// Converts a double value to a decimal string with a given number of fraction digits.
    /// </summary>
    /// <param name="dValue">
    /// The double value.
    /// </param>
    /// <param name="nFraction">
    /// The number of fraction digits.
    /// </param>
    /// <returns>
    /// A decimal string with '.' as decimal separator.
    /// </returns>
    public static string ToDecimalString(double? dValue,int nFraction) {
      if (dValue.HasValue) {
        System.Text.StringBuilder sbFormat = new System.Text.StringBuilder("0.");
        for (int i = 0;i<nFraction;i++) {
          sbFormat.Append('0');
        }
        return dValue.Value.ToString(sbFormat.ToString(),System.Globalization.CultureInfo.InvariantCulture);
      }
      return "";
    }
    public static string ToString(DateTime value,bool bIncludeTime=true)
    {
      if (bIncludeTime) {
        return value.ToString("yyyy-MM-dd HH:mm:ss.fff");
      } else {
        return value.ToString("yyyy-MM-dd");
      }
    }
  }
}
