using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BioMap.Shared;

namespace BioMap.Pages.Workflow
{
    public partial class MonitoringCalendar : ComponentBase
    {
        const int Interval = 4;
        const int kwMin = 16;
        const int kwMax = 44;
        private int kwNow = 0;
        private class Result
        {
            public int Catches = 0;
            public int ReCatches = 0;
        }
        private class PlannedMonitoring
        {
            public int Week;
            public string Color;
            public string UserId;
            public bool Definitely;
        }
        private class ResultOfPlace
        {
            public Dictionary<int, Result> Results;
            public PlannedMonitoring PlannedMonitoring;
            public int MostRecentVisit_kw = 0;
        }
        private Dictionary<string, ResultOfPlace> Results = new Dictionary<string, ResultOfPlace>();
        private string[] PlaceNames;
        private int Year
        {
            get
            {
                return _Year;
            }
            set
            {
                if (value != _Year)
                {
                    _Year = value;
                    RefreshData();
                    this.StateHasChanged();
                }
            }
        }
        private int _Year = (DateTime.Now - TimeSpan.FromDays(100)).Year;
        //
        protected override void OnInitialized()
        {
            base.OnInitialized();
            RefreshData();
        }
        private void RefreshData()
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            var dtNow = DateTime.Now;
            this.kwNow = dfi.Calendar.GetWeekOfYear(dtNow, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            this.Results.Clear();
            foreach (var place in DS.GetPlaces(SD))
            {
                int kw_MostRecent = 0;
                var dictResults = new Dictionary<int, Result>();
                foreach (var el in DS.GetElements(SD, null, WhereClauses.Is_FromPlace(place.Name)))
                {
                    int y = el.ElementProp.CreationTime.Year;
                    int kw = dfi.Calendar.GetWeekOfYear(el.ElementProp.CreationTime, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
                    if (y == this.Year && kw >= kwMin && kw <= kwMax)
                    {
                        if (el.Classification.IsMonitoring())
                        {
                            if (!dictResults.TryGetValue(kw, out Result result))
                            {
                                result = new Result();
                                dictResults[kw] = result;
                            }
                            if (el.Classification.IsIdPhoto())
                            {
                                result.Catches++;
                                var prevCatches = DS.GetElements(SD, null, "indivdata.iid=" + el.GetIId() + " AND elements.creationtime<'" + el.GetIsoDateTime() + "'");
                                if (prevCatches.Length >= 1)
                                {
                                    result.ReCatches++;
                                }
                            }
                            kw_MostRecent = kw;
                        }
                    }
                }
                var pm = new PlannedMonitoring
                {
                    Week = Math.Max(this.kwNow,kw_MostRecent+Interval),
                };
                pm.Color = (pm.Week==this.kwNow) ? "orange" : (pm.Week==this.kwNow+1) ? "green" : string.Empty;
                var rop = new ResultOfPlace {
                    Results = dictResults,
                    PlannedMonitoring = pm,
                    MostRecentVisit_kw = kw_MostRecent,
                };
                this.Results.Add(place.Name, rop);
            }
            var l = this.Results.Keys.ToList();
            l.Sort();
            this.PlaceNames = l.ToArray();
        }
    }
}
