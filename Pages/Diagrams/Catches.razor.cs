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

namespace BioMap.Pages.Diagrams
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
    private BarConfig _configPerMonth;
    private Chart _chartJsPerMonth;
    private BarConfig _configHeadBodyLength;
    private Chart _chartJsHeadBodyLength;
    private BarConfig _configGenderRatio;
    private Chart _chartJsGenderRatio;
    private BarLinearCartesianAxis _yAxisGenderRatioCnt;
    //
    private string selectedTab = "OverTime";

    private void OnSelectedTabChanged(string name) {
      selectedTab = name;
    }
    //
    protected override void OnInitialized() {
      base.OnInitialized();
      _configOverTime = new BarConfig {
        Options = new BarOptions {
          Animation=new Animation {
            Duration=0,
          },
          Title = new OptionsTitle {
            Text="XXX",
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
                  Stacked = true
                },
                new LinearCartesianAxis {
                  ID="tavg",
                  ScaleLabel=new ScaleLabel {
                    LabelString="°C",
                    Display=true,
                  },
                },
                new LinearCartesianAxis {
                  ID="prcp",
                  ScaleLabel=new ScaleLabel {
                    LabelString="mm",
                    Display=true,
                  },
                },
            }
          },
        },
      };
      _configPerMonth = new BarConfig {
        Options = new BarOptions {
          Animation=new Animation {
            Duration=0,
          },
          Title = new OptionsTitle {
            Text="XXX",
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
                    Stacked = true
                }
            },
            YAxes = new List<CartesianAxis>
          {
                new BarLinearCartesianAxis {
                  ID="cnt",
                  ScaleLabel=new ScaleLabel {
                    Display=true,
                  },
                  Stacked = true
                },
                new LinearCartesianAxis {
                  ID="tavg",
                  ScaleLabel=new ScaleLabel {
                    LabelString="°C",
                    Display=true,
                  },
                },
                new LinearCartesianAxis {
                  ID="prcp",
                  ScaleLabel=new ScaleLabel {
                    LabelString="mm",
                    Display=true,
                  },
                },
            }
          },
        },
      };
      _configHeadBodyLength = new BarConfig {
        Options = new BarOptions {
          Animation=new Animation {
            Duration=0,
          },
          Title = new OptionsTitle {
            Text="XXX",
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
                    Stacked = true
                }
            },
            YAxes = new List<CartesianAxis>
        {
                new BarLinearCartesianAxis
                {
                    Stacked = true
                }
            }
          },
        },
      };
      _yAxisGenderRatioCnt = new BarLinearCartesianAxis {
        ID="cnt",
        ScaleLabel=new ScaleLabel {
          Display=true,
        },
        Stacked = true
      };
      _configGenderRatio = new BarConfig {
        Options = new BarOptions {
          Animation=new Animation {
            Duration=0,
          },
          Title = new OptionsTitle {
            Text="XXX",
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
                _yAxisGenderRatioCnt,
                new LinearCartesianAxis {
                  ID="ratio",
                  ScaleLabel=new ScaleLabel {
                    LabelString="%",
                    Display=true,
                  },
                  Ticks= new LinearCartesianTicks {
                    Min=0,
                    Max=100,
                  },
                },
            }
          },
        },
      };
      SD.Filters.FilterChanged+=(sender,ev) => {
        RefreshData();
        base.InvokeAsync(StateHasChanged);
      };
      RefreshData();
    }
    private void RefreshData() {
      var aaIndisByIId = DS.GetIndividuals(SD.Filters);
      {
        _configOverTime.Data.Labels.Clear();
        _configOverTime.Data.Datasets.Clear();
        //
        {
          int nIndex = 0;
          foreach (var indiSpec in new[] {
            new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 2+",(el)=>el.GetWinters()>=2),
            new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 1",(el)=>el.GetWinters()==1),
            new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 0",(el)=>el.GetWinters()==0),
          }) {
            var dictCatches = new Dictionary<string,int>();
            foreach (var ea in aaIndisByIId.Values) {
              foreach (var el in ea) {
                if (indiSpec.Item2(el)) {
                  string sKey = el.GetIsoDateTime().Substring(2,5);
                  if (!dictCatches.ContainsKey(sKey)) {
                    dictCatches[sKey]=0;
                  }
                  dictCatches[sKey]++;
                }
              }
            }
            var ds = new BarDataset<int>() {
              YAxisId="cnt",
              Label=indiSpec.Item1,
              BackgroundColor=this.GetColor(nIndex),
            };
            var lDates = new List<string>(dictCatches.Keys);
            lDates.Sort();
            for (int idx=0;idx<lDates.Count;idx++) {
              string sKey=lDates[idx];
              if (nIndex==0) {
                _configOverTime.Data.Labels.Add(sKey);
              }
              ds.Add(dictCatches[sKey]);
            }
            _configOverTime.Data.Datasets.Add(ds);
            nIndex++;
          }
        }
        {
          var csvContent = (new CsvHelper()).ReadCsv("MeteoStat Donaustauf.csv");
          if (csvContent.Rows.Length>=1) {
            var dsTemperature = new LineDataset<double>() {
              Label=Localize["Temperature"],
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Red",
              PointRadius = 3,
              YAxisId="tavg",
            };
            var dsPrecipitation = new LineDataset<double>() {
              Label=Localize["Precipitation"],
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Blue",
              PointRadius = 3,
              YAxisId="prcp",
            };
            int nColIdx_Temperature=Array.IndexOf(csvContent.Headers,"tavg")-1;
            int nColIdx_Precipitation=Array.IndexOf(csvContent.Headers,"prcp")-1;
            foreach (var sLabel in _configOverTime.Data.Labels) {
              int nYear=2000+ConvInvar.ToInt(sLabel.Substring(0,2));
              int nMonth=ConvInvar.ToInt(sLabel.Substring(3,2));
              int nCnt=0;
              double dSum_Temperature=0;
              double dSum_Precipitation=0;
              foreach (var row in csvContent.Rows) {
                if (row.DateTime.Year==nYear && row.DateTime.Month==nMonth) {
                  nCnt++;
                  dSum_Temperature+=row.Columns[nColIdx_Temperature];
                  dSum_Precipitation+=row.Columns[nColIdx_Precipitation];
                }
              }
              dsTemperature.Add(dSum_Temperature/nCnt);
              dsPrecipitation.Add(dSum_Precipitation);
            }
            _configOverTime.Data.Datasets.Insert(0,dsPrecipitation);
            _configOverTime.Data.Datasets.Insert(0,dsTemperature);
          }
        }
      }
      {
        _configPerMonth.Data.Labels.Clear();
        _configPerMonth.Data.Datasets.Clear();
        //
        {
          int nIndex = 0;
          foreach (var indiSpec in new[] {
            new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 2+",(el)=>el.GetWinters()>=2),
            new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 1",(el)=>el.GetWinters()==1),
            new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 0",(el)=>el.GetWinters()==0),
          }) {
            var aCatchCountsPerMonth = new int[12];
            foreach (var ea in aaIndisByIId.Values) {
              foreach (var el in ea) {
                if (indiSpec.Item2(el)) {
                  aCatchCountsPerMonth[el.ElementProp.CreationTime.Month-1]++;
                }
              }
            }
            var ds = new BarDataset<int>() {
              Label=indiSpec.Item1,
              BackgroundColor=this.GetColor(nIndex),
            };
            for (int month = 4;month<=10;month++) {
              if (nIndex==0) {
                _configPerMonth.Data.Labels.Add(month.ToString("00"));
              }
              ds.Add(aCatchCountsPerMonth[month-1]);
            }
            _configPerMonth.Data.Datasets.Add(ds);
            nIndex++;
          }
        }
        {
          var csvContent = (new CsvHelper()).ReadCsv("MeteoStat Donaustauf.csv");
          if (csvContent.Rows.Length>=1) {
            var dsTemperature = new LineDataset<double>() {
              Label=Localize["Temperature"],
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Red",
              PointRadius = 3,
              YAxisId="tavg",
            };
            var dsPrecipitation = new LineDataset<double>() {
              Label=Localize["Precipitation"],
              BackgroundColor = "rgba(0,0,0,0)",
              BorderWidth = 2,
              PointHoverBorderWidth = 0,
              BorderColor = "Blue",
              PointRadius = 3,
              YAxisId="prcp",
            };
            int nColIdx_Temperature=Array.IndexOf(csvContent.Headers,"tavg")-1;
            int nColIdx_Precipitation=Array.IndexOf(csvContent.Headers,"prcp")-1;
            foreach (var sLabel in _configPerMonth.Data.Labels) {
              int nMonth=ConvInvar.ToInt(sLabel);
              int nCnt=0;
              double dSum_Temperature=0;
              double dSum_Precipitation=0;
              foreach (var row in csvContent.Rows) {
                if (row.DateTime.Month==nMonth) {
                  nCnt++;
                  dSum_Temperature+=row.Columns[nColIdx_Temperature];
                  dSum_Precipitation+=row.Columns[nColIdx_Precipitation];
                }
              }
              dsTemperature.Add(dSum_Temperature/nCnt);
              dsPrecipitation.Add(dSum_Precipitation);
            }
            _configPerMonth.Data.Datasets.Insert(0,dsPrecipitation);
            _configPerMonth.Data.Datasets.Insert(0,dsTemperature);
          }
        }
      }
      {
        _configHeadBodyLength.Data.Labels.Clear();
        _configHeadBodyLength.Data.Datasets.Clear();
        //
        int nIndex = 0;
        foreach (var indiSpec in new[] {
          new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 2+",(el)=>el.GetWinters()>=2),
          new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 1",(el)=>el.GetWinters()==1),
          new Tuple<string,Func<Element,bool>>(Localize["Hibernations"]+": 0",(el)=>el.GetWinters()==0),
        }) {
          var aCatchCountsHeadBodyLength = new int[30];
          foreach (var ea in aaIndisByIId.Values) {
            foreach (var el in ea) {
              if (indiSpec.Item2(el)) {
                aCatchCountsHeadBodyLength[Math.Min(aCatchCountsHeadBodyLength.Length-1,(int)Math.Floor(el.GetHeadBodyLengthMm()/2))]++;
              }
            }
          }
          var ds = new BarDataset<int>() {
            Label=indiSpec.Item1,
            BackgroundColor=this.GetColor(nIndex),
          };
          for (int idx = 6;idx<aCatchCountsHeadBodyLength.Length;idx++) {
            if (nIndex==0) {
              _configHeadBodyLength.Data.Labels.Add((idx*2).ToString());
            }
            ds.Add(aCatchCountsHeadBodyLength[idx]);
          }
          _configHeadBodyLength.Data.Datasets.Add(ds);
          nIndex++;
        }
      }
      {
        _configGenderRatio.Data.Labels.Clear();
        _configGenderRatio.Data.Datasets.Clear();
        //
        {
          var aFemaleCount = new int[12];
          var aMaleCount = new int[12];
          foreach (var ea in aaIndisByIId.Values) {
            foreach (var el in ea) {
              string sGender=el.GetGender();
              if (sGender.StartsWith("f")) {
                aFemaleCount[el.ElementProp.CreationTime.Month-1]++;
              } else if (sGender.StartsWith("m")) {
                aMaleCount[el.ElementProp.CreationTime.Month-1]++;
              }
            }
          }
          var dsFemale = new BarDataset<int>() {
            Label=Localize["Female"],
            BackgroundColor=ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Red)),
          };
          var dsMale = new BarDataset<int>() {
            Label=Localize["Male"],
            BackgroundColor=ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Blue)),
          };
          var dsRatio = new LineDataset<double>() {
            Label=Localize["Male"]+" %",
            BackgroundColor = "rgba(0,0,0,0)",
            BorderWidth = 2,
            PointHoverBorderWidth = 0,
            BorderColor = "Green",
            PointRadius = 3,
            YAxisId="ratio",
          };
          for (int month = 4;month<=10;month++) {
            _configGenderRatio.Data.Labels.Add(month.ToString("00"));
            dsFemale.Add(-aFemaleCount[month-1]);
            dsMale.Add(aMaleCount[month-1]);
            int nSum=aFemaleCount[month-1]+aMaleCount[month-1];
            dsRatio.Add(nSum<10 ? 50 : ((aMaleCount[month-1]*100.0)/nSum));
          }
          var nMaxCnt = Math.Max(-dsFemale.Min(),dsMale.Max());
          _yAxisGenderRatioCnt.Ticks = new LinearCartesianTicks {
            Min=-nMaxCnt,
            Max=nMaxCnt,
          };
          _configGenderRatio.Data.Datasets.Add(dsFemale);
          _configGenderRatio.Data.Datasets.Add(dsMale);
          _configGenderRatio.Data.Datasets.Add(dsRatio);
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
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.DarkMagenta)),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.FromArgb(200,System.Drawing.Color.Magenta)),
    };
  }
}