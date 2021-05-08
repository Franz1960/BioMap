using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;

namespace BioMap
{
    public class Monitoring
    {
        public Monitoring(SessionData sd) {
            this.SD = sd;
        }
        private SessionData SD { get; }
        public readonly int Interval = 4;
        public readonly int kwMin = 16;
        public readonly int kwMax = 44;
        public int kwNow { get; private set; } = 0;
        public class Result
        {
            public int Catches = 0;
            public int ReCatches = 0;
        }
        public class PlannedMonitoring
        {
            public int Week;
            public string Color;
            public string UserId;
            public bool Definitely;
        }
        public class ResultOfPlace
        {
            public Dictionary<int,Result> Results;
            public PlannedMonitoring PlannedMonitoring;
            public int MostRecentVisit_kw = 0;
        }
        public Dictionary<string,ResultOfPlace> Results { get; } = new Dictionary<string,ResultOfPlace>();
        public string[] PlaceNames { get; private set; }
        public int Year {
            get {
                return _Year;
            }
            set {
                if (value != _Year) {
                    _Year = value;
                    RefreshData();
                    Utilities.FireEvent(this.DataChanged,this,EventArgs.Empty);
                }
            }
        }
        private int _Year = (DateTime.Now - TimeSpan.FromDays(100)).Year;
        public event EventHandler DataChanged;
        public void RefreshData() {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            var dtNow = DateTime.Now;
            this.kwNow = dfi.Calendar.GetWeekOfYear(dtNow,dfi.CalendarWeekRule,dfi.FirstDayOfWeek);
            this.Results.Clear();
            foreach (var place in this.SD.DS.GetPlaces(this.SD)) {
                int kw_MostRecent = 0;
                var dictResults = new Dictionary<int,Result>();
                foreach (var el in this.SD.DS.GetElements(this.SD,null,WhereClauses.Is_FromPlace(place.Name))) {
                    int y = el.ElementProp.CreationTime.Year;
                    int kw = dfi.Calendar.GetWeekOfYear(el.ElementProp.CreationTime,dfi.CalendarWeekRule,dfi.FirstDayOfWeek);
                    if (y == this.Year && kw >= kwMin && kw <= kwMax) {
                        if (el.Classification.IsMonitoring()) {
                            if (!dictResults.TryGetValue(kw,out Result result)) {
                                result = new Result();
                                dictResults[kw] = result;
                            }
                            if (el.Classification.IsIdPhoto()) {
                                result.Catches++;
                                var prevCatches = this.SD.DS.GetElements(this.SD,null,"indivdata.iid=" + el.GetIId() + " AND elements.creationtime<'" + el.GetIsoDateTime() + "'");
                                if (prevCatches.Length >= 1) {
                                    result.ReCatches++;
                                }
                            }
                            kw_MostRecent = kw;
                        }
                    }
                }
                var pm = new PlannedMonitoring {
                    Week = Math.Max(this.kwNow,kw_MostRecent + Interval),
                };
                pm.Color = (pm.Week == this.kwNow) ? "orange" : (pm.Week == this.kwNow + 1) ? "green" : string.Empty;
                var rop = new ResultOfPlace {
                    Results = dictResults,
                    PlannedMonitoring = pm,
                    MostRecentVisit_kw = kw_MostRecent,
                };
                this.Results.Add(place.Name,rop);
            }
            var l = this.Results.Keys.ToList();
            l.Sort();
            this.PlaceNames = l.ToArray();
        }
    }
}
