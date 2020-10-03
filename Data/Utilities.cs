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
  }
}
