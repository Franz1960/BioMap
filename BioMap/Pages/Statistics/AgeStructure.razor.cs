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
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    protected override void OnInitialized() {
      base.OnInitialized();
      this._configCountByAge = new BarConfig {
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
                    Stacked = true
                },
            },
            YAxes = new List<CartesianAxis> {
                new LinearCartesianAxis {
                  ID="cnt",
                  ScaleLabel=new ScaleLabel {
                    Display=true,
                  },
                  Ticks=new LinearCartesianTicks {
                    Min=0,
                  },
                },
            }
          },
        },
      };
      this.SD.Filters.FilterChanged += this.Filters_FilterChanged;
      this.NM.LocationChanged += this.NM_LocationChanged;
      this.RefreshData();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      this._tableFromChartCountByAge.RefreshData();
    }
    private async void Filters_FilterChanged(object sender, EventArgs e) {
      this.RefreshData();
      base.InvokeAsync(this.StateHasChanged);
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      this.NM.LocationChanged -= this.NM_LocationChanged;
      this.SD.Filters.FilterChanged -= this.Filters_FilterChanged;
    }
    private void RefreshData() {
      Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD, this.SD.Filters);
      {
        var lAgeValues = new List<int>();
        var dictYearToDataset = new Dictionary<int, Dictionary<int, int>>();
        int nYearMin = this.SD.CurrentProject.StartDate.Value.Year;
        int nYearMax = DateTime.Now.Year;
        for (int nYear = nYearMin; nYear <= nYearMax; nYear++) {
          var dictCountsByAge = new Dictionary<int, int>();
          foreach (int iid in aaIndisByIId.Keys) {
            foreach (Element el in aaIndisByIId[iid]) {
              if (el.ElementProp.CreationTime.Year == nYear) {
                int nWinters = (int)el.GetWinters();
                if (!dictCountsByAge.ContainsKey(nWinters)) {
                  dictCountsByAge[nWinters] = 0;
                }
                dictCountsByAge[nWinters]++;
                break;
              }
            }
          }
          foreach (int nAge in dictCountsByAge.Keys.ToList()) {
            if (!lAgeValues.Contains(nAge)) {
              lAgeValues.Add(nAge);
            }
          }
          dictYearToDataset.Add(nYear, dictCountsByAge);
        }
        lAgeValues.Sort();
        //
        this._configCountByAge.Data.Labels.Clear();
        this._configCountByAge.Data.Datasets.Clear();
        //
        foreach (int nAgeValue in lAgeValues) {
          this._configCountByAge.Data.Labels.Add(ConvInvar.ToString(nAgeValue));
        }
        for (int nYear = nYearMin; nYear <= nYearMax; nYear++) {
          var ds = new LineDataset<int>() {
            Label = this.Localize[ConvInvar.ToString(nYear)],
            BackgroundColor = "rgba(0,0,0,0)",
            BorderWidth = 2,
            PointHoverBorderWidth = 0,
            BorderColor = this.GetColor(nYear - nYearMin),
            PointRadius = 3,
          };
          var dictCountsByAge = dictYearToDataset[nYear];
          foreach (int nAgeValue in lAgeValues) {
            if (dictCountsByAge.TryGetValue(nAgeValue, out int nCount)) {
              ds.Add(nCount);
            } else {
              ds.Add(0);
            }
          }
          this._configCountByAge.Data.Datasets.Add(ds);
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
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.DarkMagenta)),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Magenta)),
    };
  }
}
