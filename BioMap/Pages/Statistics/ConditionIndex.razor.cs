using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioMap.Shared;
using ChartJs.Blazor;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.Common.Handlers;
using ChartJs.Blazor.Common.Time;
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.Util;
using GoogleMapsComponents;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Statistics
{
  public partial class ConditionIndex : ComponentBase
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
    private string selectedTab = "OverTime";
    private void OnSelectedTabChanged(string name) {
      this.selectedTab = name;
      this.RefreshData();
    }
    //
    protected override void OnInitialized() {
      base.OnInitialized();
      this._configByPlace = new BarConfig {
        Options = new BarOptions {
          Animation = new Animation {
            Duration = 0,
          },
          Title = new OptionsTitle {
            Text = "XXX",
            Display = false,
          },
          Legend = new Legend {
            Display = true,
          },
          Scales = new BarScales {
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
      this._configOverTime = new BarConfig {
        Options = new BarOptions {
          Animation = new Animation {
            Duration = 0,
          },
          Title = new OptionsTitle {
            Text = "XXX",
            Display = false,
          },
          Legend = new Legend {
            Display = true,
          },
          Scales = new BarScales {
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
      this.SD.Filters.FilterChanged += (sender, ev) => {
        this.RefreshData();
        this.StateHasChanged();
      };
      this.RefreshData();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      this._tableFromChartByPlace.RefreshData();
      this._tableFromChartOverTime.RefreshData();
    }
    private void RefreshData() {
      if (this.selectedTab == "OverTime") {
        this._configOverTime.Data.Labels.Clear();
        this._configOverTime.Data.Datasets.Clear();
        //
        {
          Element[] aCatches = this.DS.GetCatches(this.SD, this.SD.Filters).Where(el => el.GetConditionIndex(this.SD).HasValue).ToArray();
          DateTime? dtMin = null;
          DateTime? dtMax = null;
          {
            foreach (Element el in aCatches) {
              if (!dtMin.HasValue || el.ElementProp.CreationTime < dtMin.Value) {
                dtMin = el.ElementProp.CreationTime;
              }
              if (!dtMax.HasValue || el.ElementProp.CreationTime > dtMax.Value) {
                dtMax = el.ElementProp.CreationTime;
              }
            }
          }
          if (dtMin.HasValue && dtMax.HasValue) {
            var dsIndis = new BarDataset<double>() {
              YAxisId = "cnt",
              Label = this.Localize["Condition index"],
              BackgroundColor = this.GetColor(0),
            };
            int nYearStart = dtMin.Value.Year;
            int nYearEnd = dtMax.Value.Year;
            for (int nYear = nYearStart; nYear <= nYearEnd; nYear++) {
              var filteredCatches = aCatches.Where(el => el.ElementProp.CreationTime.Year == nYear);
              double dConditionIndex = 0;
              if (filteredCatches.Count() >= 1) {
                dConditionIndex = filteredCatches.Average(el => el.GetConditionIndex(this.SD).Value);
              }
              dsIndis.Add(dConditionIndex);
              this._configOverTime.Data.Labels.Add(nYear.ToString("0000") + " (N=" + filteredCatches.Count() + ")");
            }
            this._configOverTime.Data.Datasets.Add(dsIndis);
          }
        }
      }
      if (this.selectedTab == "ByPlace") {
        this._configByPlace.Data.Labels.Clear();
        this._configByPlace.Data.Datasets.Clear();
        //
        {
          Element[] aCatches = this.DS.GetCatches(this.SD, this.SD.Filters).Where(el => el.GetConditionIndex(this.SD).HasValue).ToArray();
          var dsIndis = new BarDataset<double>() {
            YAxisId = "cnt",
            Label = this.Localize["Condition index"],
            BackgroundColor = this.GetColor(0),
          };
          foreach (Place place in this.DS.GetPlaces(this.SD)) {
            var filteredCatches = aCatches.Where(el => el.GetPlaceName() == place.Name);
            double dConditionIndex = 0;
            if (filteredCatches.Count() >= 1) {
              dConditionIndex = filteredCatches.Average(el => el.GetConditionIndex(this.SD).Value);
            }
            dsIndis.Add(dConditionIndex);
            this._configByPlace.Data.Labels.Add(place.Name + " (N=" + filteredCatches.Count() + ")");
          }
          this._configByPlace.Data.Datasets.Add(dsIndis);
        }
      }
    }
    public string GetColor(int nIndex) {
      return this._Colors[nIndex % this._Colors.Length];
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
