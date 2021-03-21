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
    public partial class PopulationSize : ComponentBase
    {
        [Inject]
        protected DataService DS { get; set; }
        [Inject]
        protected SessionData SD { get; set; }
        //
        private BarConfig _configByPlace;
        private Chart _chartJsByPlace;
        private TableFromChart _tableFromChartByPlace;
        private BarConfig _configOverTime;
        private Chart _chartJsOverTime;
        private TableFromChart _tableFromChartOverTime;
        //
        private string selectedTab = "ByPlace";
        private void OnSelectedTabChanged(string name)
        {
            selectedTab = name;
        }
        //
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _configByPlace = new BarConfig
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
                    Stacked = false,
                },
            },
                        YAxes = new List<CartesianAxis> {
              new BarLinearCartesianAxis {
                ID="cnt",
                ScaleLabel=new ScaleLabel {
                  Display=true,
                },
                Stacked = false,
                Ticks=new LinearCartesianTicks {
                  Min=0,
                },
              },
            }
                    },
                },
            };
            _configOverTime = new BarConfig
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
                    Stacked = false,
                },
            },
                        YAxes = new List<CartesianAxis> {
              new BarLinearCartesianAxis {
                ID="cnt",
                ScaleLabel=new ScaleLabel {
                  Display=true,
                },
                Stacked = false,
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
                this.StateHasChanged();
            };
            RefreshData();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
            }
            _tableFromChartByPlace.RefreshData();
            _tableFromChartOverTime.RefreshData();
        }
        private void RefreshData()
        {
            {
                _configByPlace.Data.Labels.Clear();
                _configByPlace.Data.Datasets.Clear();
                //
                {
                    int nTotalIndis = 0;
                    var lPlaces = new List<string>();
                    var dsIndis = new BarDataset<int>()
                    {
                        YAxisId = "cnt",
                        Label = Localize["Individuals"],
                        BackgroundColor = this.GetColor(0),
                    };
                    foreach (var place in DS.GetPlaces(SD))
                    {
                        string sAddFilter = "";
                        sAddFilter = Filters.AddToWhereClause(sAddFilter, "elements.place='" + place.Name + "'");
                        var aaIndisByIId = DS.GetIndividuals(SD, SD.Filters, sAddFilter);
                        int nIndis = aaIndisByIId.Keys.Count;
                        if (nIndis >= 1)
                        {
                            nTotalIndis += nIndis;
                            dsIndis.Add(nIndis);
                            _configByPlace.Data.Labels.Add(place.Name);
                        }
                    }
                    _configByPlace.Data.Datasets.Add(dsIndis);
                }
            }
            {
                _configOverTime.Data.Labels.Clear();
                _configOverTime.Data.Datasets.Clear();
                //
                {
                    DateTime? dtMin = null;
                    DateTime? dtMax = null;
                    {
                        foreach (var aIndis in DS.GetIndividuals(SD, SD.Filters).Values)
                        {
                            foreach (var el in aIndis)
                            {
                                if (!dtMin.HasValue || el.ElementProp.CreationTime < dtMin.Value)
                                {
                                    dtMin = el.ElementProp.CreationTime;
                                }
                                if (!dtMax.HasValue || el.ElementProp.CreationTime > dtMax.Value)
                                {
                                    dtMax = el.ElementProp.CreationTime;
                                }
                            }
                        }
                    }
                    if (dtMin.HasValue && dtMax.HasValue)
                    {
                        var dsIndis = new BarDataset<int>()
                        {
                            YAxisId = "cnt",
                            Label = Localize["Individuals"],
                            BackgroundColor = this.GetColor(0),
                        };
                        var dtStart = (dtMin.Value.Date + TimeSpan.FromDays(7 - (int)dtMin.Value.DayOfWeek));
                        var dtEnd = (dtMax.Value.Date + TimeSpan.FromDays(7));
                        for (var dt = dtStart; dt < dtEnd; dt += TimeSpan.FromDays(7))
                        {
                            if (dt.DayOfYear >= 105 && dt.DayOfYear < 285)
                            {
                                string sAddFilter = "";
                                sAddFilter = Filters.AddToWhereClause(sAddFilter, "elements.creationtime<'" + ConvInvar.ToString(dt) + "'");
                                var aaIndisByIId = DS.GetIndividuals(SD, SD.Filters, sAddFilter);
                                int nIndis = aaIndisByIId.Keys.Count;
                                if (nIndis >= 1)
                                {
                                    dsIndis.Add(nIndis);
                                    _configOverTime.Data.Labels.Add(dt.ToString("yyyy-MM-dd"));
                                }
                            }
                        }
                        _configOverTime.Data.Datasets.Add(dsIndis);
                    }
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
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Pink)),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Magenta)),
    };
    }
}
