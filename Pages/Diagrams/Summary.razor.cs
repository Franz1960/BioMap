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
  public partial class Summary : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private int ProjectYearBegin;
    private int ProjectYearEnd;
    private class DataByYears_t
    {
      public string Name;
      public string AllYears;
      public string[] ByYear;
    }
    private BarConfig _config;
    private Chart _chartJs;
    private readonly List<DataByYears_t> IndividualData = new List<DataByYears_t>();
    private readonly List<DataByYears_t> OtherData = new List<DataByYears_t>();
    protected override void OnInitialized() {
      base.OnInitialized();
      ProjectYearBegin=DS.ProjectStart.Year;
      ProjectYearEnd=Math.Max(2024,DateTime.Now.Year);
      _config = new BarConfig {
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
      //
      _config.Data.Datasets.Clear();
      for (int year = ProjectYearBegin;year<=ProjectYearEnd;year++) {
        _config.Data.Labels.Add(ConvInvar.ToString(year));
      }
      //
      var aaIndisByIId = DS.GetIndividuals();
      int nIndex = 0;
      foreach (var indiSpec in new[] {
      new Tuple<string,Func<List<Element>,int,bool>,int>(Localize["Known individuals"],(ea,year)=>ea.Any(el=>el.ElementProp.CreationTime.Year<year) && ea[ea.Count-1].ElementProp.CreationTime.Year>=year,1),
      new Tuple<string,Func<List<Element>,int,bool>,int>(Localize["New individuals"]+" 1+ "+Localize["Hibernations"],(ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()>=1,1),
      new Tuple<string,Func<List<Element>,int,bool>,int>(Localize["New individuals"]+" 0 "+Localize["Hibernations"],(ea,year)=>ea[0].ElementProp.CreationTime.Year==year && ea[0].GetWinters()<1,1),
      new Tuple<string,Func<List<Element>,int,bool>,int>(Localize["Missed individuals"]+" 1+ "+Localize["Hibernations"],(ea,year)=>ea[ea.Count-1].ElementProp.CreationTime.Year==year-1 && ea[ea.Count-1].GetWinters()>=1,-1),
      new Tuple<string,Func<List<Element>,int,bool>,int>(Localize["Missed individuals"]+" 0 "+Localize["Hibernations"],(ea,year)=>ea[ea.Count-1].ElementProp.CreationTime.Year==year-1 && ea[ea.Count-1].GetWinters()<1,-1),
    }) {
        var ds = new BarDataset<int>() { Label=indiSpec.Item1,BackgroundColor=this.GetColor(nIndex) };
        for (int year = ProjectYearBegin;year<=ProjectYearEnd;year++) {
          int nCount = 0;
          foreach (var ea in aaIndisByIId.Values) {
            if (indiSpec.Item2(ea,year)) {
              nCount++;
            }
          }
          ds.Add(nCount*indiSpec.Item3);
        }
        _config.Data.Datasets.Add(ds);
        nIndex++;
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