using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace BioMap
{
  public class SessionData
  {
    public SessionData() {
       this.Filters = new Filters(()=>CurrentUser);
    }
    public User CurrentUser { get; } = new User();
    public bool ShowCustomMap { get; set; }
    public Filters Filters { get; }
    public bool SizeTimeChartShowVintageBoundaries { get; set; } = true;
    public string SizeTimeChartGrowingCurveMode { get; set; } = "GrowingCurve";
    public bool AlienateLocations { get; set; } = false;
    public bool MaySeeRealLocations {
      get {
        return (CurrentUser.Level>=400 && !AlienateLocations);
      }
    }
  }
}
