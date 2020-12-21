﻿using System;
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
    private BarConfig _configPerMonth;
    private Chart _chartJsPerMonth;
    private BarConfig _configHeadBodyLength;
    private Chart _chartJsHeadBodyLength;
    private BarConfig _configOverTime;
    private Chart _chartJsOverTime;
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
                new BarLinearCartesianAxis
                {
                    Stacked = true
                }
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
        _configPerMonth.Data.Labels.Clear();
        _configPerMonth.Data.Datasets.Clear();
        //
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
    }
    public string GetColor(int nIndex) {
      return _Colors[nIndex % _Colors.Length];
    }
    private string[] _Colors = new string[] {
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Green),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Blue),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Cyan),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.DarkMagenta),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Magenta),
    };
  }
}