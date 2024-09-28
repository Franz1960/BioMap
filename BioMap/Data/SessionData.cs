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
    public SessionData(DataService ds, Filters filters) {
      this.DS = ds;
      this.Filters = filters;
    }
    public SessionData(DataService ds) {
      this.DS = ds;
      this.Filters = new Filters(() => this.CurrentUser);
    }
    public DataService DS { get; }
    public string CurrentCultureName => System.Globalization.CultureInfo.CurrentCulture.Name;
    public bool IsOwner { get => (!string.IsNullOrWhiteSpace(this.CurrentUser?.EMail) && string.CompareOrdinal(this.DS.GetProjectProperty(this.CurrentUser.Project, "Owner", ""), this.CurrentUser.EMail) == 0); }
    public bool IsSuperAdmin { get => this.DS.GetSuperAdmins().Contains(this.CurrentUser?.EMail); }
    public User CurrentUser { get; } = new User();
    public Project CurrentProject { get; } = new Project();
    public event EventHandler CurrentProjectChanged;
    public void OnCurrentProjectChanged() {
      Utilities.FireEvent(this.CurrentProjectChanged, this, EventArgs.Empty);
    }
    public event EventHandler CurrentProjectLoaded;
    public void OnCurrentProjectLoaded() {
      if (this.CurrentProject.InitialHistoryYears >= 1 && this.Filters != null) {
        DateTime dtFrom = DateTime.Now - TimeSpan.FromDays(this.CurrentProject.InitialHistoryYears) * 365;
        this.Filters.DateFromFilter = new DateTime(dtFrom.Year, dtFrom.Month, 1);
      }
      Utilities.FireEvent(this.CurrentProjectLoaded, this, EventArgs.Empty);
    }
    public string SelectedElementName { get; set; }
    public IEnumerable<Taxon> MostRecentTaxons => this._MostRecentTaxons;
    public void AddMostRecentTaxon(Taxon taxon) {
      if (taxon != null) {
        if (this._MostRecentTaxons.Contains(taxon)) {
          this._MostRecentTaxons.Remove(taxon);
        }
        this._MostRecentTaxons.Insert(0, taxon);
        while (this._MostRecentTaxons.Count > 3) {
          this._MostRecentTaxons.RemoveAt(this._MostRecentTaxons.Count - 1);
        }
      }
    }
    private readonly List<Taxon> _MostRecentTaxons = new();
    public Filters Filters { get; }
    public bool ShowFilterSettings { get; set; } = true;
    public bool SizeTimeChartShowVintageBoundaries { get; set; } = true;
    public string SizeTimeChartGrowingCurveMode { get; set; } = "GrowingCurve";
    public bool IndividualDevLimitToCurrentTimeOfYear { get; set; } = false;
    public bool AlienateLocations { get; set; } = false;
    public double GetSeasonizedTime(DateTime dt) {
      const int DaysBeforeSeason = 90;
      const int DaysInSeason = 183;
      var dtYearBegin = new DateTime(dt.Year, 1, 1);
      int nDaysInYear = (dt - dtYearBegin).Days;
      int nDaysInSeason = (int)(Math.Min(DaysInSeason, Math.Max(0, nDaysInYear - DaysBeforeSeason)));
      int nDaysAfterSeason = (int)Math.Max(0, nDaysInYear - (DaysBeforeSeason + DaysInSeason));
      double dResult = dt.Year;
      dResult += (0.05 * Math.Min(nDaysInYear, DaysBeforeSeason)) / DaysBeforeSeason;
      dResult += (0.90 * nDaysInSeason) / DaysInSeason;
      dResult += (0.05 * nDaysAfterSeason) / (DaysBeforeSeason + DaysInSeason);
      return dResult;
    }
    public bool MaySeeElements {
      get {
        return (this.CurrentUser.Level >= this.CurrentProject.MinLevelToSeeElements);
      }
    }
    public bool MaySeeRealLocations {
      get {
        return (this.CurrentUser.Level >= this.CurrentProject.MinLevelToSeeExactLocations && !this.AlienateLocations);
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
      if (element?.Classification != null && element.Classification.IsIdAnyPhoto()) {
        return Math.Max(1.0, 0.80 * this.CurrentProject.MaxHeadBodyLength / Math.Max(this.CurrentProject.MinHeadBodyLength, element.GetHeadBodyLengthMm()));
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
