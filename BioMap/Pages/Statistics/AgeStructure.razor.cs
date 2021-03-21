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
using ChartJs.Blazor.Util;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.Common.Handlers;
using ChartJs.Blazor.Common.Time;
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using BioMap.Shared;

namespace BioMap.Pages.Statistics
{
    public partial class AgeStructure : ComponentBase
    {
        [Inject]
        protected DataService DS { get; set; }
        [Inject]
        protected SessionData SD { get; set; }
        //
        private BarConfig _configCountByAge;
        private Chart _chartJsCountByAge;
        private TableFromChart _tableFromChartCountByAge;
        //
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
                }
            }
        }
        private int _Year = (DateTime.Now - TimeSpan.FromDays(100)).Year;
        //
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _configCountByAge = new BarConfig
            {
                Options = new BarOptions
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
                        Display = true,
                    },
                    Scales = new BarScales
                    {
                        XAxes = new List<CartesianAxis> {
                new BarCategoryAxis {
                    Stacked = true
                },
            },
                        YAxes = new List<CartesianAxis> {
                new BarLinearCartesianAxis {
                  ID="cnt",
                  ScaleLabel=new ScaleLabel {
                    Display=true,
                  },
                  Stacked = true,
                  Ticks=new LinearCartesianTicks {
                    Min=0,
                  },
                },
            }
                    },
                },
            };
            SD.Filters.FilterChanged += (sender, ev) =>
            {
                RefreshData();
                base.InvokeAsync(this.StateHasChanged);
            };
            RefreshData();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
            }
            _tableFromChartCountByAge.RefreshData();
        }
        private void RefreshData()
        {
            var aaIndisByIId = DS.GetIndividuals(SD, SD.Filters);
            {
                _configCountByAge.Data.Labels.Clear();
                _configCountByAge.Data.Datasets.Clear();
                //
                {
                    var dictCountsByAge = new Dictionary<int, int>();
                    foreach (var iid in aaIndisByIId.Keys)
                    {
                        foreach (var el in aaIndisByIId[iid])
                        {
                            if (el.ElementProp.CreationTime.Year == Year)
                            {
                                int nWinters = (int)el.GetWinters();
                                if (!dictCountsByAge.ContainsKey(nWinters))
                                {
                                    dictCountsByAge[nWinters] = 0;
                                }
                                dictCountsByAge[nWinters]++;
                                break;
                            }
                        }
                    }
                    var lAgeValues = dictCountsByAge.Keys.ToList();
                    lAgeValues.Sort();
                    var ds = new BarDataset<int>()
                    {
                        Label = ConvInvar.ToString(Year),
                        BackgroundColor = this.GetColor(0),
                    };
                    foreach (var nAgeValue in lAgeValues)
                    {
                        _configCountByAge.Data.Labels.Add(ConvInvar.ToString(nAgeValue));
                        ds.Add(dictCountsByAge[nAgeValue]);
                    }
                    _configCountByAge.Data.Datasets.Add(ds);
                }
            }
        }
        public string GetColor(int nIndex)
        {
            return _Colors[nIndex % _Colors.Length];
        }
        private string[] _Colors = new string[] {
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Green)),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Blue)),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Cyan)),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.DarkMagenta)),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Magenta)),
    };
    }
}
