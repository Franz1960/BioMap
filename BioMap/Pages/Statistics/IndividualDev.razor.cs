using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChartJs.Blazor;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.Common.Handlers;
using ChartJs.Blazor.Common.Time;
using GoogleMapsComponents;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Statistics
{
  public partial class IndividualDev : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private int ProjectYearBegin;
    private int ProjectYearEnd;
    private int CurrentDayOfYear;
    private BarConfig _config;
    private Chart _chartJs;
    protected override void OnInitialized() {
      base.OnInitialized();
      this.ProjectYearBegin = this.SD.CurrentProject.StartDate.Value.Year;
      this.ProjectYearEnd = Math.Max(this.ProjectYearBegin + 1, (DateTime.Now - TimeSpan.FromDays(90)).Year);
      this.CurrentDayOfYear = DateTime.Now.DayOfYear;
      this._config = new BarConfig {
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
      this.SD.Filters.FilterChanged += (sender, ev) => {
        this.RefreshData();
        base.InvokeAsync(this.StateHasChanged);
      };
      this.RefreshData();
    }
    private void RefreshData() {
      //
      this._config.Data.Datasets.Clear();
      this._config.Data.Labels.Clear();
      for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
        this._config.Data.Labels.Add(ConvInvar.ToString(year));
      }
      //
      Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD);
      int nIndex = 0;
      Tuple<string, Func<List<Element>, int, bool>, int>[] aIndiSpecs;
      if (this.SD.IndividualDevLimitToCurrentTimeOfYear) {
        int nCurrentDayOfYear = this.CurrentDayOfYear;
        aIndiSpecs = new Tuple<string, Func<List<Element>, int, bool>, int>[] {
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["Known individuals"],(ea,year)=>ea.Any(el=>el.ElementProp.CreationTime.Year<year) && ea[^1].ElementProp.CreationTime.Year==year && ea[^1].ElementProp.CreationTime.DayOfYear<=nCurrentDayOfYear,1),
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["New individuals"]+" 1+ "+this.Localize["Hibernations"],(ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].ElementProp.CreationTime.DayOfYear<=nCurrentDayOfYear && ea[0].GetWinters()>=1,1),
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["New individuals"]+" 0 "+this.Localize["Hibernations"],(ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].ElementProp.CreationTime.DayOfYear<=nCurrentDayOfYear && ea[0].GetWinters()<1,1),
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["Missed individuals"]+" 1+ "+this.Localize["Hibernations"],(ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()>=1,-1),
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["Missed individuals"]+" 0 "+this.Localize["Hibernations"],(ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()<1,-1),
        };
      } else {
        aIndiSpecs = new Tuple<string, Func<List<Element>, int, bool>, int>[] {
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["Known individuals"],(ea,year)=>ea.Any(el=>el.ElementProp.CreationTime.Year<year) && ea[^1].ElementProp.CreationTime.Year>=year,1),
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["New individuals"]+" 1+ "+this.Localize["Hibernations"],(ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()>=1,1),
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["New individuals"]+" 0 "+this.Localize["Hibernations"],(ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()<1,1),
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["Missed individuals"]+" 1+ "+this.Localize["Hibernations"],(ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()>=1,-1),
          new Tuple<string,Func<List<Element>,int,bool>,int>(this.Localize["Missed individuals"]+" 0 "+this.Localize["Hibernations"],(ea,year)=>ea[^1].ElementProp.CreationTime.Year==year-1 && ea[^1].GetWinters()<1,-1),
        };
      }
      foreach (Tuple<string, Func<List<Element>, int, bool>, int> indiSpec in aIndiSpecs) {
        var ds = new BarDataset<int>() { Label = indiSpec.Item1, BackgroundColor = this.GetColor(nIndex) };
        for (int year = this.ProjectYearBegin; year <= this.ProjectYearEnd; year++) {
          int nCount = 0;
          foreach (int nIId in aaIndisByIId.Keys) {
            var ea = aaIndisByIId[nIId].Where(el => el.ElementProp.CreationTime.Year <= year).ToList();
            if (ea.Count() > 0) {
              if (indiSpec.Item2(ea, year)) {
                nCount++;
              }
            }
          }
          ds.Add(nCount * indiSpec.Item3);
        }
        this._config.Data.Datasets.Add(ds);
        nIndex++;
      }
    }
    public string GetColor(int nIndex) {
      return this._Colors[nIndex % this._Colors.Length];
    }
    private readonly string[] _Colors = new string[] {
        ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Green),
        ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Blue),
        ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Cyan),
        ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.DarkMagenta),
        ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Magenta),
    };
    private void CheckBoxLimitToCurrentTimeOfYear_CheckedChanged(ChangeEventArgs e) {
      this.SD.IndividualDevLimitToCurrentTimeOfYear = bool.Parse(e.Value.ToString());
      this.RefreshData();
      base.InvokeAsync(this.StateHasChanged);
    }
  }
}
