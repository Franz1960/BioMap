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
    private int ProjectYearBegin;
    private int ProjectYearEnd;
    private BarConfig _configByPlace;
    private Chart _chartJsByPlace;
    private TableFromChart _tableFromChartByPlace;
    private BarConfig _configOverTime;
    private Chart _chartJsOverTime;
    private TableFromChart _tableFromChartOverTime;
    private LineConfig _configByPlaceAndTime;
    private Chart _chartJsByPlaceAndTime;
    //
    private string selectedTab = "OverTime";
    private void OnSelectedTabChanged(string name) {
      this.selectedTab = name;
      this.RefreshData();
    }
    //
    protected override void OnInitialized() {
      base.OnInitialized();
      this.ProjectYearBegin = this.SD.CurrentProject.StartDate.Value.Year;
      this.ProjectYearEnd = Math.Max(this.ProjectYearBegin + 1, (DateTime.Now - TimeSpan.FromDays(90)).Year);
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
                new BarLinearCartesianAxis {
                  ID="reprate",
                  ScaleLabel=new ScaleLabel {
                    LabelString=this.Localize["Reproduction rate"],
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
              new BarLinearCartesianAxis {
                ID="reprate",
                ScaleLabel=new ScaleLabel {
                  LabelString=this.Localize["Reproduction rate"],
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
      this._configByPlaceAndTime = new LineConfig {
        Options = new LineOptions {
          Animation = new Animation {
            Duration = 0,
          },
          Title = new OptionsTitle {
            Text = "XXX",
            Display = false,
          },
          Legend = new Legend {
            Display = false,
          },
          Scales = new Scales {
            XAxes = new List<CartesianAxis>
                  {
                            new LinearCartesianAxis
                            {
                                ScaleLabel = new ScaleLabel
                                {
                                    LabelString = this.Localize["Time"]
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
        for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
          this._configOverTime.Data.Labels.Add(ConvInvar.ToString(year));
        }
        {
          Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD, this.SD.Filters);
          var hatchlings = new List<Element>();
          foreach (List<Element> lCatches in aaIndisByIId.Values) {
            Element elCatch = lCatches[0];
            if (elCatch.GetWinters() == 0) {
              hatchlings.Add(elCatch);
            }
          }
          {
            var dsAdults = new BarDataset<int>() {
              YAxisId = "cnt",
              Label = this.Localize["Population size"],
              BackgroundColor = this.GetColor(0),
            };
            var dsHatchlings = new BarDataset<int>() {
              YAxisId = "cnt",
              Label = this.Localize["Hatchlings"],
              BackgroundColor = this.GetColor(2),
            };
            var dsReproductionRate = new BarDataset<double>() {
              YAxisId = "reprate",
              Label = this.Localize["Reproduction rate"],
              BackgroundColor = this.GetColor(3),
            };
            for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
              var dt = new DateTime(year, 12, 31);
              int nAdults = this.DS.GetPopulationSize(this.SD, dt, 2, 2);
              dsAdults.Add(nAdults);
              int nHatchlings = hatchlings.Where(el => el.ElementProp.CreationTime.Year == year).Count();
              dsHatchlings.Add(nHatchlings);
              double fReproductionRate = (nAdults < 1) ? 0 : ((double)nHatchlings) / nAdults;
              dsReproductionRate.Add(fReproductionRate);
            }
            this._configOverTime.Data.Datasets.Add(dsAdults);
            this._configOverTime.Data.Datasets.Add(dsHatchlings);
            this._configOverTime.Data.Datasets.Add(dsReproductionRate);
          }
        }
      }
      if (this.selectedTab == "ByPlace") {
        this._configByPlace.Data.Labels.Clear();
        this._configByPlace.Data.Datasets.Clear();
        //
        {
          int nTotalAdults = 0;
          int nTotalHatchlings = 0;
          var lPlaces = new List<string>();
          var dsAdults = new BarDataset<int>() {
            YAxisId = "cnt",
            Label = this.Localize["Adults"],
            BackgroundColor = this.GetColor(0),
          };
          var dsHatchlings = new BarDataset<int>() {
            YAxisId = "cnt",
            Label = this.Localize["Hatchlings"],
            BackgroundColor = this.GetColor(2),
          };
          var dsReproductionRate = new BarDataset<double>() {
            YAxisId = "reprate",
            Label = this.Localize["Reproduction rate"],
            BackgroundColor = this.GetColor(3),
          };
          foreach (Place place in this.DS.GetPlaces(this.SD)) {
            string sAddFilter = "";
            sAddFilter = Filters.AddToWhereClause(sAddFilter, "elements.place='" + place.Name + "'");
            Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD, this.SD.Filters, sAddFilter);
            var funcGetIndiCnt = new Func<Func<Element, bool>, int>((cond) => {
              int nResult = 0;
              foreach (List<Element> aIndis in aaIndisByIId.Values) {
                foreach (Element el in aIndis) {
                  if (cond(el)) {
                    nResult++;
                    break;
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
              this._configByPlace.Data.Labels.Add(place.Name);
            }
          }
          this._configByPlace.Data.Datasets.Add(dsAdults);
          this._configByPlace.Data.Datasets.Add(dsHatchlings);
          this._configByPlace.Data.Datasets.Add(dsReproductionRate);
        }
      }
      if (this.selectedTab == "ByPlaceAndTime") {
        DateTime dtProjectStart = this.SD.CurrentProject.StartDate.Value;
        var lElements = new List<Element>();
        lElements.AddRange(this.DS.GetElements(
          this.SD,
          this.SD.Filters,
          Filters.AddToWhereClause(WhereClauses.Is_Individuum, "indivdata.winters=0"),
          "elements.creationtime ASC",
          true));
        lElements.AddRange(this.DS.GetElements(
          this.SD,
          this.SD.Filters,
          Filters.AddToWhereClause(WhereClauses.Is_LivingBeing, "elements.lbsciname='Bombina variegata'"),
          "elements.creationtime ASC",
          true));
        this._configByPlaceAndTime.Data.Datasets.Clear();
        var countByPlaceAndDate = new Dictionary<string, Dictionary<DateTime, int>>();
        foreach (Element el in lElements) {
          string sPlace = el.GetPlaceName();
          DateTime date = el.ElementProp.CreationTime.Date;
          int nSize = 0;
          if (el.HasIndivData()) {
            nSize = 1;
          } else if (el.Classification.IsLivingBeing() && el.Classification.LivingBeing.Stadium == ElementClassification.Stadium.Juveniles) {
            nSize = el.Classification.LivingBeing.Count;
          }
          if (nSize > 0) {
            if (!countByPlaceAndDate.TryGetValue(sPlace, out Dictionary<DateTime, int> countByDate)) {
              countByDate = new Dictionary<DateTime, int>();
              countByPlaceAndDate.Add(sPlace, countByDate);
            }
            if (!countByDate.ContainsKey(date)) {
              countByDate[date] = 0;
            }
            countByDate[date] += nSize;
          }
        }
        var lPlaces = new List<string>(countByPlaceAndDate.Keys);
        lPlaces.Sort();
        this._configByPlaceAndTime.Data.YLabels.Clear();
        for (int idxPlace = 0; idxPlace < lPlaces.Count; idxPlace++) {
          string sPlace = lPlaces[idxPlace];
          this._configByPlaceAndTime.Data.YLabels.Add(sPlace);
          var lDates = new List<DateTime>(countByPlaceAndDate[sPlace].Keys);
          lDates.Sort();
          foreach (DateTime date in lDates) {
            int nRadius = (int)Math.Round(2 * Math.Sqrt(countByPlaceAndDate[sPlace][date]));
            var lineSetCurve = new LineDataset<Point> {
              BackgroundColor = "rgba(239,209,0,0.8)",
              BorderWidth = 1,
              PointHoverBorderWidth = 1,
              BorderColor = "rgba(239,209,0,0.8)",
              PointRadius = nRadius,
              PointHoverRadius = nRadius,
              PointHitRadius = nRadius,
              ShowLine = false,
            };
            lineSetCurve.Label = sPlace + " " + ConvInvar.ToString(countByPlaceAndDate[sPlace][date]) + " " + this.Localize["Individuals"];
            lineSetCurve.Add(new Point(this.SD.GetSeasonizedTime(date), idxPlace));
            if (lineSetCurve.Data.Count >= 1) {
              this._configByPlaceAndTime.Data.Datasets.Add(lineSetCurve);
            }
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
