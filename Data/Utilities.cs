using System;
using System.Collections.Generic;
using System.Timers;

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
    private class DelayedCaller
    {
      internal DelayedCaller(int delayMs,Action action) {
        this.DelayMs=delayMs;
        this.Action=action;
        this.Timer=new Timer(delayMs);
        this.Timer.Elapsed += this.Timer_Elapsed;
      }
      private void Timer_Elapsed(object sender,ElapsedEventArgs e) {
        this.Timer.Elapsed -= this.Timer_Elapsed;
        lock (DelayedCallers) {
          DelayedCallers.Remove(this.Action);
        }
        this.Action();
      }
      internal Action Action;
      internal Timer Timer;
      internal int DelayMs;
    }
    private static readonly Dictionary<Action,DelayedCaller> DelayedCallers = new Dictionary<Action, DelayedCaller>();
    /// <summary>
    /// Execute an action after a given delay. If this method is called again for the same action before the action 
    /// has been executed, the delay will start over again.
    /// Typical use case: Each change on a resource must lead to a refresh action, but if several changes occur in a 
    /// small span of time the refresh action shall be executed only once.
    /// </summary>
    /// <param name="delayMs">
    /// The delay in milliseconds.
    /// </param>
    /// <param name="action">
    /// The action.
    /// </param>
    public static void CallDelayed(int delayMs,Action action) {
      lock (DelayedCallers) {
        DelayedCaller dc=null;
        if (!DelayedCallers.TryGetValue(action,out dc)) {
          dc=new DelayedCaller(delayMs,action);
          DelayedCallers[action] = dc;
        }
        dc.Timer.Stop();
        dc.Timer.Start();
      }
    }
  }
}
