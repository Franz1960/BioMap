using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BioMap
{
  public class SessionData
  {
    public SessionData(DataService ds) {
      this.DS = ds;
      this.Filters = new Filters(() => CurrentUser);
    }
    public DataService DS { get; }
    public string CurrentCultureName => System.Globalization.CultureInfo.CurrentCulture.Name;
    public User CurrentUser { get; } = new User();
    public Project CurrentProject { get; } = new Project();
    public event EventHandler CurrentProjectChanged;
    public void OnCurrentProjectChanged() {
      Utilities.FireEvent(this.CurrentProjectChanged, this, EventArgs.Empty);
    }
    public string SelectedElementName { get; set; }
    public Filters Filters { get; }
    public bool SizeTimeChartShowVintageBoundaries { get; set; } = true;
    public string SizeTimeChartGrowingCurveMode { get; set; } = "GrowingCurve";
    public bool AlienateLocations { get; set; } = false;
    public double GetSeasonizedTime(DateTime dt) {
      const int DaysBeforeSeason = 90;
      const int DaysInSeason = 183;
      var dtYearBegin = new DateTime(dt.Year, 1, 1);
      var nDaysInYear = (dt - dtYearBegin).Days;
      var nDaysInSeason = (int)(Math.Min(DaysInSeason, Math.Max(0, nDaysInYear - DaysBeforeSeason)));
      var nDaysAfterSeason = (int)Math.Max(0, nDaysInYear - (DaysBeforeSeason + DaysInSeason));
      double dResult = dt.Year;
      dResult += (0.05 * Math.Min(nDaysInYear, DaysBeforeSeason)) / DaysBeforeSeason;
      dResult += (0.90 * nDaysInSeason) / DaysInSeason;
      dResult += (0.05 * nDaysAfterSeason) / (DaysBeforeSeason + DaysInSeason);
      return dResult;
    }
    public bool MaySeeElements {
      get {
        return (CurrentUser.Level >= CurrentProject.MinLevelToSeeElements);
      }
    }
    public bool MaySeeRealLocations {
      get {
        return (CurrentUser.Level >= CurrentProject.MinLevelToSeeExactLocations && !AlienateLocations);
      }
    }
    public void SetPlace(Element el, string sPlaceName) {
      if (el.ElementProp.MarkerInfo.PlaceName != sPlaceName) {
        this.DS.AddLogEntry(this, $"Changed place of element \"{el.ElementName}\" from \"{el.ElementProp.MarkerInfo.PlaceName}\" to \"{sPlaceName}\".");
        el.ElementProp.MarkerInfo.PlaceName = sPlaceName;
        if (!string.IsNullOrEmpty(sPlaceName)) {
          el.ElementProp.MarkerInfo.position = this.DS.GetPlaceByName(this, sPlaceName)?.LatLng;
        }
      }
    }
    public double GetIdPhotoZoom(Element element) {
      if (element?.Classification != null && element.Classification.IsIdPhoto()) {
        return 0.80 * this.CurrentProject.MaxHeadBodyLength / Math.Max(this.CurrentProject.MinHeadBodyLength, element.GetHeadBodyLengthMm());
      }
      return 0;
    }
    public string GetIdPhotoZoomString(Element element) {
      double dZoom = this.GetIdPhotoZoom(element);
      if (dZoom != 0) {
        return ConvInvar.ToDecimalString(dZoom, 4);
      }
      return "0";
    }
  }
}
