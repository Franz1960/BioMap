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
    public string CurrentCultureName=>System.Globalization.CultureInfo.CurrentCulture.Name;
    public User CurrentUser { get; } = new User();
    public Project CurrentProject { get; } = new Project();
    public event EventHandler CurrentProjectChanged;
    public void OnCurrentProjectChanged() {
      Utilities.FireEvent(this.CurrentProjectChanged,this,EventArgs.Empty);
    }
    public string SelectedElementName { get; set; }
    public Filters Filters { get; }
    public bool SizeTimeChartShowVintageBoundaries { get; set; } = true;
    public string SizeTimeChartGrowingCurveMode { get; set; } = "GrowingCurve";
    public bool AlienateLocations { get; set; } = false;
    public bool MaySeeElements {
      get {
        return (CurrentUser.Level>=CurrentProject.MinLevelToSeeElements);
      }
    }
    public bool MaySeeRealLocations {
      get {
        return (CurrentUser.Level>=CurrentProject.MinLevelToSeeExactLocations && !AlienateLocations);
      }
    }
  }
}
