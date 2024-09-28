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
  public partial class PopulationSize : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private int VanishAfterYearsMissing {
      get => this._VanishAfterYearsMissing;
      set {
        if (value != this._VanishAfterYearsMissing) {
          this._VanishAfterYearsMissing = value;
          this.RefreshData();
        }
      }
    }
    private int _VanishAfterYearsMissing = 2;
    //
    private BarConfig _configOverTime;
    private Chart _chartJsOverTime;
    private TableFromChart _tableFromChartOverTime;
    private BarConfig _configByPlace;
    private Chart _chartJsByPlace;
    private TableFromChart _tableFromChartByPlace;
    private BarConfig _configByRecatch;
    private Chart _chartJsByRecatch;
    private TableFromChart _tableFromChartByRecatch;
    private TableFromData _tableFromDataByRecatch;
    private string[] RecatchData_ColumnHeaders;
    private string[][] RecatchData_Rows;
    //
    private string selectedTab = "OverTime";
    private void OnSelectedTabChanged(string name) {
      this.selectedTab = name;
      this.RefreshData();
    }
    //
    protected override void OnInitialized() {
      base.OnInitialized();
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
      this._configByRecatch = new BarConfig {
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
      this._tableFromChartOverTime.RefreshData();
      this._tableFromChartByPlace.RefreshData();
      this._tableFromChartByRecatch.RefreshData();
    }
    private void RefreshData() {
      if (this.selectedTab == "OverTime") {
        this._configOverTime.Data.Labels.Clear();
        this._configOverTime.Data.Datasets.Clear();
        //
        {
          DateTime? dtMin = null;
          DateTime? dtMax = null;
          {
            foreach (List<Element> aIndis in this.DS.GetIndividuals(this.SD, this.SD.Filters).Values) {
              foreach (Element el in aIndis) {
                if (!dtMin.HasValue || el.ElementProp.CreationTime < dtMin.Value) {
                  dtMin = el.ElementProp.CreationTime;
                }
                if (!dtMax.HasValue || el.ElementProp.CreationTime > dtMax.Value) {
                  dtMax = el.ElementProp.CreationTime;
                }
              }
            }
          }
          if (dtMin.HasValue && dtMax.HasValue) {
            var dsIndis = new BarDataset<int>() {
              YAxisId = "cnt",
              Label = this.Localize["Individuals"],
              BackgroundColor = this.GetColor(0),
            };
            DateTime dtStart = (dtMin.Value.Date + TimeSpan.FromDays(7 - (int)dtMin.Value.DayOfWeek));
            DateTime dtEnd = (dtMax.Value.Date + TimeSpan.FromDays(7));
            for (DateTime dt = dtStart; dt < dtEnd; dt += TimeSpan.FromDays(7)) {
              if (dt.DayOfYear >= 105 && dt.DayOfYear < 285) {
                int nIndis = this.DS.GetPopulationSize(this.SD, dt, null, this.VanishAfterYearsMissing);
                if (nIndis >= 1) {
                  dsIndis.Add(nIndis);
                  this._configOverTime.Data.Labels.Add(dt.ToString("yyyy-MM-dd"));
                }
              }
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
          int nTotalIndis = 0;
          var lPlaces = new List<string>();
          var dsIndis = new BarDataset<int>() {
            YAxisId = "cnt",
            Label = this.Localize["Individuals"],
            BackgroundColor = this.GetColor(0),
          };
          foreach (Place place in this.DS.GetPlaces(this.SD)) {
            string sAddFilter = "";
            sAddFilter = Filters.AddToWhereClause(sAddFilter, "elements.place='" + place.Name + "'");
            if (this.VanishAfterYearsMissing >= 1) {
              var dtVanish = DateTime.Now - TimeSpan.FromDays(365 * this.VanishAfterYearsMissing);
              sAddFilter = Filters.AddToWhereClause(sAddFilter, "elements.creationtime>'" + ConvInvar.ToString(dtVanish) + "'");
            }
            Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD, this.SD.Filters, sAddFilter);
            int nIndis = aaIndisByIId.Keys.Count;
            if (nIndis >= 1) {
              nTotalIndis += nIndis;
              dsIndis.Add(nIndis);
              this._configByPlace.Data.Labels.Add(place.Name);
            }
          }
          this._configByPlace.Data.Datasets.Add(dsIndis);
        }
      }
      if (this.selectedTab == "ByRecatch") {
        this._configByRecatch.Data.Labels.Clear();
        this._configByRecatch.Data.Datasets.Clear();
        //
        if (this.SD.CurrentProject.StartDate.HasValue) {
          var dsIndis = new BarDataset<int>() {
            YAxisId = "cnt",
            Label = this.Localize["Individuals"],
            BackgroundColor = this.GetColor(0),
          };
          int nYearMin = this.SD.CurrentProject.StartDate.Value.Year;
          int nYearMax = DateTime.Now.Year;
          var dictPopByPlace = new Dictionary<string, int>();
          var dictByYearAndPlaces = new Dictionary<string, Dictionary<string, string>>();
          for (int nYear = nYearMin; nYear <= nYearMax; nYear++) {
            var results = Recatch.GetResults(this.SD, nYear);
            int N_total = 0;
            foreach (string sPlace in results.Keys) {
              N_total += results[sPlace];
            }
            dsIndis.Add(N_total);
            this._configByRecatch.Data.Labels.Add(nYear.ToString());
            {
              string sYear = ConvInvar.ToString(nYear);
              if (!dictByYearAndPlaces.TryGetValue(sYear, out Dictionary<string, string> dictByPlaces)) {
                dictByPlaces = new Dictionary<string, string>();
                dictByYearAndPlaces.Add(sYear, dictByPlaces);
              }
              foreach (var sPlace in results.Keys) {
                dictByPlaces.Add(sPlace, ConvInvar.ToString(results[sPlace]));
              }
            }
          }
          this._configByRecatch.Data.Datasets.Add(dsIndis);
          // Table by places.
          {
            var lColumnHeaders = new List<string>();
            var lRows = new List<string[]>();
            lColumnHeaders.Add(this.Localize["Place"]);
            lColumnHeaders.AddRange(dictByYearAndPlaces.Keys);
            foreach (string sPlace in this.DS.GetPlaces(this.SD).Select(pl => pl.Name)) {
              var lRow = new List<string>();
              lRow.Add(sPlace);
              foreach (string sYear in dictByYearAndPlaces.Keys) {
                if (dictByYearAndPlaces.ContainsKey(sYear) && dictByYearAndPlaces[sYear].ContainsKey(sPlace)) {
                  lRow.Add(dictByYearAndPlaces[sYear][sPlace]);
                } else {
                  lRow.Add("");
                }
              }
              lRows.Add(lRow.ToArray());
            }
            this.RecatchData_ColumnHeaders = lColumnHeaders.ToArray();
            this.RecatchData_Rows = lRows.ToArray();
          }
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
