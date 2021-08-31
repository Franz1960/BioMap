using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GoogleMapsComponents;
using ChartJs.Blazor;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.Common.Handlers;
using ChartJs.Blazor.Common.Time;
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;

namespace BioMap.Pages.Diagrams
{
    public partial class FrequencyByPlace : ComponentBase
    {
        [Inject]
        protected DataService DS { get; set; }
        [Inject]
        protected SessionData SD { get; set; }
        //
        private LineConfig _config;
        private Chart _chartJs;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _config = new LineConfig
            {
                Options = new LineOptions
                {
                    Animation = new Animation
                    {
                        Duration = 0,
                    },
                    Title = new OptionsTitle
                    {
                        Text = "XXX",
                        Display = false,
                    },
                    Legend = new Legend
                    {
                        Display = false,
                    },
                    Scales = new Scales
                    {
                        XAxes = new List<CartesianAxis>
                        {
                            new LinearCartesianAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = Localize["Time"]
                                },
                                GridLines = new GridLines
                                {
                                    Display = true,
                                },
                                Ticks = new LinearCartesianTicks
                                {
                                    StepSize=1,
                                    Precision=0,
                                },
                            }
                        },
                        YAxes = new List<CartesianAxis>
                        {
                            new CategoryAxis
                            {
                            }
                        },
                    },
                }
            };
            SD.Filters.FilterChanged += (sender, ev) =>
            {
                RefreshData();
                base.InvokeAsync(this.StateHasChanged);
            };
            RefreshData();
        }
        private void RefreshData()
        {
            var dtProjectStart = SD.CurrentProject.StartDate.Value;
            var aIndividuals = DS.GetElements(SD, SD.Filters, WhereClauses.Is_Individuum, "elements.creationtime ASC");
            _config.Data.Datasets.Clear();
            var countByPlaceAndDate = new Dictionary<string, Dictionary<DateTime, int>>();
            foreach (var el in aIndividuals)
            {
                string sPlace = el.GetPlaceName();
                var date = el.ElementProp.CreationTime.Date;
                if (!countByPlaceAndDate.TryGetValue(sPlace, out var countByDate))
                {
                    countByDate = new Dictionary<DateTime, int>();
                    countByPlaceAndDate.Add(sPlace, countByDate);
                }
                if (!countByDate.ContainsKey(date))
                {
                    countByDate[date] = 0;
                }
                countByDate[date]++;
            }
            var lPlaces = new List<string>(countByPlaceAndDate.Keys);
            lPlaces.Sort();
            _config.Data.YLabels.Clear();
            for (int idxPlace = 0; idxPlace < lPlaces.Count; idxPlace++)
            {
                string sPlace = lPlaces[idxPlace];
                _config.Data.YLabels.Add(sPlace);
                var lDates = new List<DateTime>(countByPlaceAndDate[sPlace].Keys);
                lDates.Sort();
                foreach (var date in lDates)
                {
                    int nRadius = (int)Math.Round(2 * Math.Sqrt(countByPlaceAndDate[sPlace][date]));
                    var lineSetCurve = new LineDataset<Point>
                    {
                        BackgroundColor = "rgba(239,209,0,0.8)",
                        BorderWidth = 1,
                        PointHoverBorderWidth = 1,
                        BorderColor = "rgba(239,209,0,0.8)",
                        PointRadius = nRadius,
                        PointHoverRadius = nRadius,
                        PointHitRadius = nRadius,
                        ShowLine = false,
                    };
                    lineSetCurve.Label = sPlace + " " + ConvInvar.ToString(countByPlaceAndDate[sPlace][date]) + " " + Localize["Individuals"];
                    lineSetCurve.Add(new Point(SD.GetSeasonizedTime(date), idxPlace));
                    if (lineSetCurve.Data.Count >= 1)
                    {
                        _config.Data.Datasets.Add(lineSetCurve);
                    }
                }
            }
        }
    }
}
