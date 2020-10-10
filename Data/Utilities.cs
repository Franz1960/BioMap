using System;

namespace BioMap
{
  public static class Utilities
  {
    public static void FireEvent(EventHandler ev,object sender,object e) {
      if (ev!=null) {
        foreach (Delegate d in ev.GetInvocationList()) {
          d.DynamicInvoke(new object[] { sender,e });
        }
      }
    }
    public static DateTime DateTime_from_Years(double dYears) {
      int nYears = (int)Math.Floor(dYears);
      var dt = new DateTime(nYears,1,1);
      dt+=TimeSpan.FromDays((dYears-nYears)*365);
      return dt;
    }
    public static double Years_from_DateTime(DateTime dt) {
      double dYears = dt.Year+Math.Min(365,dt.DayOfYear)/365.001;
      return dYears;
    }
  }
}
