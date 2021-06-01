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
        public SessionData(DataService ds)
        {
            this.DS = ds;
            this.Filters = new Filters(() => CurrentUser);
        }
        public DataService DS { get; }
        public string CurrentCultureName => System.Globalization.CultureInfo.CurrentCulture.Name;
        public User CurrentUser { get; } = new User();
        public Project CurrentProject { get; } = new Project();
        public event EventHandler CurrentProjectChanged;
        public void OnCurrentProjectChanged()
        {
            Utilities.FireEvent(this.CurrentProjectChanged, this, EventArgs.Empty);
        }
        public string SelectedElementName { get; set; }
        public Filters Filters { get; }
        public bool SizeTimeChartShowVintageBoundaries { get; set; } = true;
        public string SizeTimeChartGrowingCurveMode { get; set; } = "GrowingCurve";
        public bool AlienateLocations { get; set; } = false;
        public bool MaySeeElements
        {
            get
            {
                return (CurrentUser.Level >= CurrentProject.MinLevelToSeeElements);
            }
        }
        public bool MaySeeRealLocations
        {
            get
            {
                return (CurrentUser.Level >= CurrentProject.MinLevelToSeeExactLocations && !AlienateLocations);
            }
        }
        public void SetPlace(Element el, string sPlaceName)
        {
            if (el.ElementProp.MarkerInfo.PlaceName != sPlaceName)
            {
                this.DS.AddLogEntry(this, $"Changed place of element \"{el.ElementName}\" from \"{el.ElementProp.MarkerInfo.PlaceName}\" to \"{sPlaceName}\".");
                el.ElementProp.MarkerInfo.PlaceName = sPlaceName;
                if (!string.IsNullOrEmpty(sPlaceName))
                {
                    el.ElementProp.MarkerInfo.position = this.DS.GetPlaceByName(this, sPlaceName)?.LatLng;
                }
            }
        }
    }
}
