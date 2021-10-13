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
  public partial class Reproduction : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private BarConfig _configByPlace;
    private Chart _chartJsByPlace;
    private TableFromChart _tableFromChartByPlace;
    //
    private string selectedTab = "ByPlace";
    private void OnSelectedTabChanged(string name) {
      selectedTab = name;
    }
    //
    protected override void OnInitialized() {
      base.OnInitialized();
      _configByPlace = new BarConfig {
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
                new BarLinearCartesianAxis {
                  ID="reprate",
                  ScaleLabel=new ScaleLabel {
                    LabelString=Localize["Reproduction rate"],
                    Display=true,
                  },
                  Stacked = false,
                  Position = Position.Right,
                  Ticks=new LinearCartesianTicks {
                    Min=0,
                  },
                },
            }
          },
        },
      };
      SD.Filters.FilterChanged += (sender, ev) => {
        RefreshData();
        this.StateHasChanged();
      };
      RefreshData();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      _tableFromChartByPlace.RefreshData();
    }
    private void RefreshData() {
      {
        _configByPlace.Data.Labels.Clear();
        _configByPlace.Data.Datasets.Clear();
        //
        {
          int nTotalAdults = 0;
          int nTotalHatchlings = 0;
          var lPlaces = new List<string>();
          var dsAdults = new BarDataset<int>() {
            YAxisId = "cnt",
            Label = Localize["Adults"],
            BackgroundColor = this.GetColor(0),
          };
          var dsHatchlings = new BarDataset<int>() {
            YAxisId = "cnt",
            Label = Localize["Hatchlings"],
            BackgroundColor = this.GetColor(2),
          };
          var dsReproductionRate = new BarDataset<double>() {
            YAxisId = "reprate",
            Label = Localize["Reproduction rate"],
            BackgroundColor = this.GetColor(3),
          };
          foreach (var place in DS.GetPlaces(SD)) {
            string sAddFilter = "";
            sAddFilter = Filters.AddToWhereClause(sAddFilter, "elements.place='" + place.Name + "'");
            var aaIndisByIId = DS.GetIndividuals(SD, SD.Filters, sAddFilter);
            var funcGetIndiCnt = new Func<Func<Element, bool>, int>((cond) => {
              int nResult = 0;
              foreach (var aIndis in aaIndisByIId.Values) {
                foreach (var el in aIndis) {
                  if (cond(el)) {
                    nResult++;
                  }
                }
              }
              return nResult;
            });
            int nAdults = funcGetIndiCnt((el) => (el.GetWinters() >= 2));
            int nHatchlings = funcGetIndiCnt((el) => (el.GetWinters() == 0));
            if (nAdults >= 1 && nHatchlings >= 1) {
              nTotalAdults += nAdults;
              nTotalHatchlings += nHatchlings;
              dsAdults.Add(nAdults);
              dsHatchlings.Add(nHatchlings);
              dsReproductionRate.Add(((double)nHatchlings) / nAdults);
              _configByPlace.Data.Labels.Add(place.Name);
            }
          }
          _configByPlace.Data.Datasets.Add(dsAdults);
          _configByPlace.Data.Datasets.Add(dsHatchlings);
          _configByPlace.Data.Datasets.Add(dsReproductionRate);
        }
      }
    }
    public string GetColor(int nIndex) {
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
