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
  public partial class Catches : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private BarConfig _configOverTime;
    private Chart _chartJsOverTime;
    private TableFromChart _tableFromChartOverTime;
    private BarConfig _configPerMonth;
    private Chart _chartJsPerMonth;
    private TableFromChart _tableFromChartPerMonth;
    private BarConfig _configHeadBodyLength;
    private Chart _chartJsHeadBodyLength;
    private TableFromChart _tableFromChartHeadBodyLength;
    private BarConfig _configGenderRatio;
    private Chart _chartJsGenderRatio;
    private TableFromChart _tableFromChartGenderRatio;
    private BarLinearCartesianAxis _yAxisGenderRatioCnt;
    private BarConfig _configGenderRatioPerAnno;
    private Chart _chartJsGenderRatioPerAnno;
    private TableFromChart _tableFromChartGenderRatioPerAnno;
    private BarLinearCartesianAxis _yAxisGenderRatioPerAnnoCnt;
    private BarConfig _configMigrationDistances;
    private Chart _chartJsMigrationDistances;
    private TableFromChart _tableFromChartMigrationDistances;
    //
    private string selectedTab = "OverTime";
    //
    private bool RelativeMigrationCounts {
      get => this._RelativeMigrationCounts;
      set {
        if (value != this._RelativeMigrationCounts) {
          this._RelativeMigrationCounts = value;
          this.RefreshData();
          base.InvokeAsync(this.StateHasChanged);
        }
      }
    }
    private bool _RelativeMigrationCounts = false;
    //
    private void OnSelectedTabChanged(string name) {
      this.selectedTab = name;
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
                new LinearCartesianAxis {
                  ID="tavg",
                  ScaleLabel=new ScaleLabel {
                    LabelString="°C",
                    Display=true,
                  },
                  Position = Position.Right,
                },
                new LinearCartesianAxis {
                  ID="prcp",
                  ScaleLabel=new ScaleLabel {
                    LabelString="mm",
                    Display=true,
                  },
                  Position = Position.Right,
                },
            }
          },
        },
      };
      this._configPerMonth = new BarConfig {
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
            XAxes = new List<CartesianAxis>
    {
                new BarCategoryAxis
                {
                    Stacked = true,
                }
            },
            YAxes = new List<CartesianAxis>
    {
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
                new LinearCartesianAxis {
                  ID="tavg",
                  ScaleLabel=new ScaleLabel {
                    LabelString="°C",
                    Display=true,
                  },
                  Position = Position.Right,
                },
                new LinearCartesianAxis {
                  ID="prcp",
                  ScaleLabel=new ScaleLabel {
                    LabelString="mm",
                    Display=true,
                  },
                  Position = Position.Right,
                },
            }
          },
        },
      };
      this._configHeadBodyLength = new BarConfig {
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
            XAxes = new List<CartesianAxis>
      {
              new BarCategoryAxis
              {
                  Stacked = true,
              },
            },
            YAxes = new List<CartesianAxis>
      {
              new BarLinearCartesianAxis
              {
                Stacked = true,
                Ticks=new LinearCartesianTicks {
                  Min=0,
                },
              },
            }
          },
        },
      };
      this._yAxisGenderRatioCnt = new BarLinearCartesianAxis {
        ID = "cnt",
        ScaleLabel = new ScaleLabel {
          Display = true,
        },
        Stacked = true
      };
      this._configGenderRatio = new BarConfig {
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
                this._yAxisGenderRatioCnt,
                new LinearCartesianAxis {
                  ID="ratio",
                  ScaleLabel=new ScaleLabel {
                    LabelString="%",
                    Display=true,
                  },
                  Position = Position.Right,
                  Ticks= new LinearCartesianTicks {
                    Min=0,
                    Max=100,
                  },
                },
            }
          },
        },
      };
      this._yAxisGenderRatioPerAnnoCnt = new BarLinearCartesianAxis {
        ID = "cnt",
        ScaleLabel = new ScaleLabel {
          Display = true,
        },
        Stacked = true
      };
      this._configGenderRatioPerAnno = new BarConfig {
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
                this._yAxisGenderRatioPerAnnoCnt,
                new LinearCartesianAxis {
                  ID="ratio",
                  ScaleLabel=new ScaleLabel {
                    LabelString="%",
                    Display=true,
                  },
                  Position = Position.Right,
                  Ticks= new LinearCartesianTicks {
                    Min=0,
                    Max=100,
                  },
                },
            }
          },
        },
      };
      this._configMigrationDistances = new BarConfig {
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
            XAxes = new List<CartesianAxis>
      {
              new BarCategoryAxis
              {
                Stacked = true,
              },
            },
            YAxes = new List<CartesianAxis>
      {
              new BarLinearCartesianAxis
              {
                Stacked = true,
                Ticks=new LinearCartesianTicks {
                  Min=0,
                  Max = null,
                },
                ScaleLabel = new ScaleLabel {
                  Display = false,
                },
              },
            },
          },
        },
      };
      this.SD.Filters.FilterChanged += (sender, ev) => {
        this.RefreshData();
        base.InvokeAsync(this.StateHasChanged);
      };
      this.RefreshData();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      this._tableFromChartOverTime.RefreshData();
      this._tableFromChartPerMonth.RefreshData();
      this._tableFromChartHeadBodyLength.RefreshData();
      this._tableFromChartGenderRatio.RefreshData();
      this._tableFromChartGenderRatioPerAnno.RefreshData();
      this._tableFromChartMigrationDistances.RefreshData();
    }
    private void RefreshData() {
      Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD, this.SD.Filters);
      {
        this._configOverTime.Data.Labels.Clear();
        this._configOverTime.Data.Datasets.Clear();
        //
        {
          int nIndex = 0;
          Tuple<string, Func<Element, bool>>[] indiSpecs = new[] {
            new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 2+",(el)=>el.GetWinters()>=2),
            new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 1",(el)=>el.GetWinters()==1),
            new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 0",(el)=>el.GetWinters()==0),
          };
          var lColLabels = new List<string>();
          foreach (Tuple<string, Func<Element, bool>> indiSpec in indiSpecs) {
            foreach (List<Element> ea in aaIndisByIId.Values) {
              foreach (Element el in ea) {
                if (indiSpec.Item2(el)) {
                  string sKey = el.GetIsoDateTime().Substring(0, 7);
                  if (!lColLabels.Contains(sKey)) {
                    lColLabels.Add(sKey);
                  }
                }
              }
            }
          }
          lColLabels.Sort();
          foreach (string sColLabel in lColLabels) {
            this._configOverTime.Data.Labels.Add(sColLabel);
          }
          foreach (Tuple<string, Func<Element, bool>> indiSpec in indiSpecs) {
            int[] cntCatches = new int[lColLabels.Count];
            foreach (List<Element> ea in aaIndisByIId.Values) {
              foreach (Element el in ea) {
                if (indiSpec.Item2(el)) {
                  string sKey = el.GetIsoDateTime().Substring(0, 7);
                  cntCatches[lColLabels.IndexOf(sKey)]++;
                }
              }
            }
            var ds = new BarDataset<int>() {
              YAxisId = "cnt",
              Label = indiSpec.Item1,
              BackgroundColor = this.GetColor(nIndex),
            };
            ds.AddRange(cntCatches);
            this._configOverTime.Data.Datasets.Add(ds);
            nIndex++;
          }
        }
        {
          CsvHelper.CsvContent csvContent = (new CsvHelper()).ReadCsv(this.SD, "MeteoStat Donaustauf.csv");
          if (csvContent.Rows.Length >= 1) {
            var dsTemperature = new LineDataset<double>() {
              Label = this.Localize["Temperature"],
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Red",
              PointRadius = 3,
              YAxisId = "tavg",
            };
            var dsPrecipitation = new LineDataset<double>() {
              Label = this.Localize["Precipitation"],
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Blue",
              PointRadius = 3,
              YAxisId = "prcp",
            };
            int nColIdx_Temperature = Array.IndexOf(csvContent.Headers, "tavg") - 1;
            int nColIdx_Precipitation = Array.IndexOf(csvContent.Headers, "prcp") - 1;
            foreach (string sLabel in this._configOverTime.Data.Labels) {
              int nYear = ConvInvar.ToInt(sLabel.Substring(0, 4));
              int nMonth = ConvInvar.ToInt(sLabel.Substring(5, 2));
              int nCnt = 0;
              double dSum_Temperature = 0;
              double dSum_Precipitation = 0;
              foreach (CsvHelper.CsvContent.Row row in csvContent.Rows) {
                if (row.DateTime.Year == nYear && row.DateTime.Month == nMonth) {
                  nCnt++;
                  dSum_Temperature += row.Columns[nColIdx_Temperature];
                  dSum_Precipitation += row.Columns[nColIdx_Precipitation];
                }
              }
              dsTemperature.Add(dSum_Temperature / nCnt);
              dsPrecipitation.Add(dSum_Precipitation);
            }
            this._configOverTime.Data.Datasets.Insert(0, dsPrecipitation);
            this._configOverTime.Data.Datasets.Insert(0, dsTemperature);
          }
        }
      }
      {
        this._configPerMonth.Data.Labels.Clear();
        this._configPerMonth.Data.Datasets.Clear();
        //
        {
          int nIndex = 0;
          foreach (Tuple<string, Func<Element, bool>> indiSpec in new[] {
            new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 2+",(el)=>el.GetWinters()>=2),
            new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 1",(el)=>el.GetWinters()==1),
            new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 0",(el)=>el.GetWinters()==0),
          }) {
            int[] aCatchCountsPerMonth = new int[12];
            foreach (List<Element> ea in aaIndisByIId.Values) {
              foreach (Element el in ea) {
                if (indiSpec.Item2(el)) {
                  aCatchCountsPerMonth[el.ElementProp.CreationTime.Month - 1]++;
                }
              }
            }
            var ds = new BarDataset<int>() {
              Label = indiSpec.Item1,
              BackgroundColor = this.GetColor(nIndex),
            };
            for (int month = 4; month <= 10; month++) {
              if (nIndex == 0) {
                this._configPerMonth.Data.Labels.Add(month.ToString("00"));
              }
              ds.Add(aCatchCountsPerMonth[month - 1]);
            }
            this._configPerMonth.Data.Datasets.Add(ds);
            nIndex++;
          }
        }
        {
          CsvHelper.CsvContent csvContent = (new CsvHelper()).ReadCsv(this.SD, "MeteoStat Donaustauf.csv");
          if (csvContent.Rows.Length >= 1) {
            var dsTemperature = new LineDataset<double>() {
              Label = this.Localize["Temperature"],
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Red",
              PointRadius = 3,
              YAxisId = "tavg",
            };
            var dsPrecipitation = new LineDataset<double>() {
              Label = this.Localize["Precipitation"],
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Blue",
              PointRadius = 3,
              YAxisId = "prcp",
            };
            int nColIdx_Temperature = Array.IndexOf(csvContent.Headers, "tavg") - 1;
            int nColIdx_Precipitation = Array.IndexOf(csvContent.Headers, "prcp") - 1;
            foreach (string sLabel in this._configPerMonth.Data.Labels) {
              int nMonth = ConvInvar.ToInt(sLabel);
              int nCnt = 0;
              double dSum_Temperature = 0;
              double dSum_Precipitation = 0;
              foreach (CsvHelper.CsvContent.Row row in csvContent.Rows) {
                if (row.DateTime.Month == nMonth) {
                  nCnt++;
                  dSum_Temperature += row.Columns[nColIdx_Temperature];
                  dSum_Precipitation += row.Columns[nColIdx_Precipitation];
                }
              }
              dsTemperature.Add(dSum_Temperature / nCnt);
              dsPrecipitation.Add(dSum_Precipitation);
            }
            this._configPerMonth.Data.Datasets.Insert(0, dsPrecipitation);
            this._configPerMonth.Data.Datasets.Insert(0, dsTemperature);
          }
        }
      }
      {
        this._configHeadBodyLength.Data.Labels.Clear();
        this._configHeadBodyLength.Data.Datasets.Clear();
        //
        int nIndex = 0;
        foreach (Tuple<string, Func<Element, bool>> indiSpec in new[] {
                    new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 2+",(el)=>el.GetWinters()>=2),
                    new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 1",(el)=>el.GetWinters()==1),
                    new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 0",(el)=>el.GetWinters()==0),
                }) {
          int[] aCatchCountsHeadBodyLength = new int[(int)Math.Ceiling(this.SD.CurrentProject.MaxHeadBodyLength / 2)];
          foreach (List<Element> ea in aaIndisByIId.Values) {
            foreach (Element el in ea) {
              if (indiSpec.Item2(el)) {
                aCatchCountsHeadBodyLength[Math.Min(aCatchCountsHeadBodyLength.Length - 1, (int)Math.Floor(el.GetHeadBodyLengthMm() / 2))]++;
              }
            }
          }
          var ds = new BarDataset<int>() {
            Label = indiSpec.Item1,
            BackgroundColor = this.GetColor(nIndex),
          };
          for (int idx = (int)Math.Floor(this.SD.CurrentProject.MinHeadBodyLength / 2); idx < aCatchCountsHeadBodyLength.Length; idx++) {
            if (nIndex == 0) {
              this._configHeadBodyLength.Data.Labels.Add((idx * 2).ToString());
            }
            ds.Add(aCatchCountsHeadBodyLength[idx]);
          }
          this._configHeadBodyLength.Data.Datasets.Add(ds);
          nIndex++;
        }
      }
      {
        this._configGenderRatio.Data.Labels.Clear();
        this._configGenderRatio.Data.Datasets.Clear();
        //
        {
          int[] aFemaleCount = new int[12];
          int[] aMaleCount = new int[12];
          foreach (List<Element> ea in aaIndisByIId.Values) {
            foreach (Element el in ea) {
              string sGender = el.Gender;
              if (el.GetWinters() >= 1) {
                if (sGender.StartsWith("f")) {
                  aFemaleCount[el.ElementProp.CreationTime.Month - 1]++;
                } else if (sGender.StartsWith("m")) {
                  aMaleCount[el.ElementProp.CreationTime.Month - 1]++;
                }
              }
            }
          }
          var dsFemale = new BarDataset<int>() {
            Label = this.Localize["Female"],
            BackgroundColor = ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200, System.Drawing.Color.Red)),
          };
          var dsMale = new BarDataset<int>() {
            Label = this.Localize["Male"],
            BackgroundColor = ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200, System.Drawing.Color.Blue)),
          };
          var dsRatio = new LineDataset<double>() {
            Label = this.Localize["Male"] + " %",
            BackgroundColor = "rgba(0,0,0,0)",
            BorderWidth = 2,
            PointHoverBorderWidth = 0,
            BorderColor = "Green",
            PointRadius = 3,
            YAxisId = "ratio",
          };
          for (int month = 4; month <= 10; month++) {
            this._configGenderRatio.Data.Labels.Add(month.ToString("00"));
            dsFemale.Add(-aFemaleCount[month - 1]);
            dsMale.Add(aMaleCount[month - 1]);
            int nSum = aFemaleCount[month - 1] + aMaleCount[month - 1];
            dsRatio.Add(nSum < 10 ? 50 : ((aMaleCount[month - 1] * 100.0) / nSum));
          }
          int nMaxCnt = Math.Max(-dsFemale.Min(), dsMale.Max());
          this._yAxisGenderRatioCnt.Ticks = new LinearCartesianTicks {
            Min = -nMaxCnt,
            Max = nMaxCnt,
          };
          this._configGenderRatio.Data.Datasets.Add(dsFemale);
          this._configGenderRatio.Data.Datasets.Add(dsMale);
          this._configGenderRatio.Data.Datasets.Add(dsRatio);
        }
      }
      {
        this._configGenderRatioPerAnno.Data.Labels.Clear();
        this._configGenderRatioPerAnno.Data.Datasets.Clear();
        //
        {
          var dictFemaleCount = new Dictionary<int, int>();
          var dictMaleCount = new Dictionary<int, int>();
          foreach (List<Element> ea in aaIndisByIId.Values) {
            foreach (Element el in ea) {
              string sGender = el.Gender;
              if (el.GetWinters() >= 1) {
                if (sGender.StartsWith("f")) {
                  if (!dictFemaleCount.ContainsKey(el.ElementProp.CreationTime.Year)) {
                    dictFemaleCount[el.ElementProp.CreationTime.Year] = 0;
                  }
                  dictFemaleCount[el.ElementProp.CreationTime.Year]++;
                } else if (sGender.StartsWith("m")) {
                  if (!dictMaleCount.ContainsKey(el.ElementProp.CreationTime.Year)) {
                    dictMaleCount[el.ElementProp.CreationTime.Year] = 0;
                  }
                  dictMaleCount[el.ElementProp.CreationTime.Year]++;
                }
              }
            }
          }
          if (dictFemaleCount.Count >= 2 && dictMaleCount.Count >= 2) {
            int nYearBegin = Math.Min(dictFemaleCount.Keys.Min(), dictMaleCount.Keys.Min());
            int nYearEnd = Math.Max(dictFemaleCount.Keys.Max(), dictMaleCount.Keys.Max());
            var dsFemale = new BarDataset<int>() {
              Label = this.Localize["Female"],
              BackgroundColor = ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200, System.Drawing.Color.Red)),
            };
            var dsMale = new BarDataset<int>() {
              Label = this.Localize["Male"],
              BackgroundColor = ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200, System.Drawing.Color.Blue)),
            };
            var dsRatio = new LineDataset<double>() {
              Label = this.Localize["Male"] + " %",
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Green",
              PointRadius = 3,
              YAxisId = "ratio",
            };
            for (int year = nYearBegin; year <= nYearEnd; year++) {
              this._configGenderRatioPerAnno.Data.Labels.Add(year.ToString("0000"));
              int nFemaleCount = dictFemaleCount.GetValueOrDefault(year, 0);
              int nMaleCount = dictMaleCount.GetValueOrDefault(year, 0);
              dsFemale.Add(-nFemaleCount);
              dsMale.Add(nMaleCount);
              int nSum = nFemaleCount + nMaleCount;
              dsRatio.Add(nSum < 10 ? 50 : ((nMaleCount * 100.0) / nSum));
            }
            int nMaxCnt = Math.Max(-dsFemale.Min(), dsMale.Max());
            this._yAxisGenderRatioPerAnnoCnt.Ticks = new LinearCartesianTicks {
              Min = -nMaxCnt,
              Max = nMaxCnt,
            };
            this._configGenderRatioPerAnno.Data.Datasets.Add(dsFemale);
            this._configGenderRatioPerAnno.Data.Datasets.Add(dsMale);
            this._configGenderRatioPerAnno.Data.Datasets.Add(dsRatio);
          }
        }
      }
      {
        this._configMigrationDistances.Data.Labels.Clear();
        this._configMigrationDistances.Data.Datasets.Clear();
        //
        int nIndex = 0;
        int nTotalCnt = aaIndisByIId.Values.Where(ea => ea.Count >= 2).Sum(ea => ea.Count - 1);
        double[] aDistanceClasses = new double[] { 0, 20, 50, 100, 200, 500, 1000, 2000, 5000 };
        var funcGetDistanceClass = new Func<double, int>((dist) => {
          for (int idx = aDistanceClasses.Length - 1; idx >= 0; idx--) {
            if (dist >= aDistanceClasses[idx]) {
              return idx;
            }
          }
          return 0;
        });
        foreach (Tuple<string, Func<Element, bool>> indiSpec in new[] {
          new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 2+",(el)=>el.GetWinters()>=2),
          new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 1",(el)=>el.GetWinters()==1),
          new Tuple<string,Func<Element,bool>>(this.Localize["Hibernations"]+": 0",(el)=>el.GetWinters()==0),
        }) {
          int[] countsPerDistanceClass = new int[aDistanceClasses.Length];
          foreach (List<Element> ea in aaIndisByIId.Values) {
            for (int idx = 1; idx < ea.Count; idx++) {
              Element el = ea[idx];
              if (indiSpec.Item2(el)) {
                double dDistance = GeoCalculator.GetDistance(ea[idx - 1].ElementProp.MarkerInfo.position, el.ElementProp.MarkerInfo.position);
                int idxDistanceClass = funcGetDistanceClass(dDistance);
                countsPerDistanceClass[idxDistanceClass]++;
              }
            }
          }
          var ds = new BarDataset<int>() {
            Label = indiSpec.Item1,
            BackgroundColor = this.GetColor(nIndex),
          };
          for (int idxKey = 0; idxKey < aDistanceClasses.Length; idxKey++) {
            if (nIndex == 0) {
              this._configMigrationDistances.Data.Labels.Add(aDistanceClasses[idxKey] + (idxKey < aDistanceClasses.Length - 1 ? ("-" + aDistanceClasses[idxKey + 1]) : "- \u221E"));
            }
            if (this.RelativeMigrationCounts) {
              ds.Add((countsPerDistanceClass[idxKey] * 1000) / nTotalCnt);
            } else {
              ds.Add(countsPerDistanceClass[idxKey]);
            }
          }
          this._configMigrationDistances.Data.Datasets.Add(ds);
          nIndex++;
        }
        if (this.RelativeMigrationCounts) {
          this._configMigrationDistances.Options.Scales.YAxes = new List<CartesianAxis>
          {
            new BarLinearCartesianAxis
            {
              Stacked = true,
              Ticks=new LinearCartesianTicks {
                Min=0,
                Max = 1000,
              },
              ScaleLabel = new ScaleLabel {
                Display = true,
                LabelString = "‰",
              },
            },
          };
        } else {
          this._configMigrationDistances.Options.Scales.YAxes = new List<CartesianAxis>
          {
            new BarLinearCartesianAxis
            {
              Stacked = true,
              Ticks=new LinearCartesianTicks {
                Min=0,
                Max = null,
              },
              ScaleLabel = new ScaleLabel {
                Display = false,
              },
            },
          };
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
