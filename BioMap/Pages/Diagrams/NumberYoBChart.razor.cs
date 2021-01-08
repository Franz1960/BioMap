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
using ChartJs.Blazor.LineChart;
using ChartJs.Blazor.BarChart;
using ChartJs.Blazor.BarChart.Axes;

namespace BioMap.Pages.Diagrams
{
  public partial class NumberYoBChart : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private LineConfig _config;
    private Chart _chartJs;

    protected override void OnInitialized() {
      base.OnInitialized();
      _config = new LineConfig {
        Options = new LineOptions {
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
          Scales = new Scales {
            XAxes = new List<CartesianAxis>
          {
            new LinearCartesianAxis
            {
              ScaleLabel = new ScaleLabel
              {
                LabelString = Localize["Time"]
              },
              GridLines = new GridLines
              {
                Display = false
              },
              Ticks = new LinearCartesianTicks
              {
                StepSize=1,
                Precision=0,
              }
            }
          },
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
      var dtProjectStart = DS.ProjectStart;
      var aaIndisByIId = DS.GetIndividuals(SD);
      _config.Data.Datasets.Clear();
      var countByYoB = new Dictionary<int,Dictionary<int,int>>(); // [yob][year]
      foreach (var idx in aaIndisByIId.Keys) {
        var elsByYears = new Dictionary<int,Element>();
        foreach (var el in aaIndisByIId[idx]) {
          int nYear = el.ElementProp.CreationTime.Year;
          int? nYoB = el.GetYearOfBirth();
          if (nYoB.HasValue) {
            if (!elsByYears.ContainsKey(nYear)) {
              elsByYears.Add(nYear,el);
            }
          }
        }
        foreach (int nYear in elsByYears.Keys) {
          var el = elsByYears[nYear];
          int nYoB = el.GetYearOfBirth().Value;
          if (!countByYoB.ContainsKey(nYoB)) {
            countByYoB.Add(nYoB,new Dictionary<int,int>());
          }
          var d = countByYoB[nYoB];
          if (!d.ContainsKey(nYear)) {
            d.Add(nYear,0);
          }
          d[nYear]++;
        }
      }
      var lYoBs = new List<int>(countByYoB.Keys);
      lYoBs.Sort();
      foreach (int nYoB in lYoBs) {
        var d = countByYoB[nYoB];
        var lYears = new List<int>(d.Keys);
        lYears.Sort();
        string sYobColor = Element.GetColorForYearOfBirth(nYoB);
        var lineSetCurve = new LineDataset<Point> {
          BackgroundColor = sYobColor,
          BorderWidth = 2,
          PointHoverBorderWidth = 0,
          BorderColor = sYobColor,
          PointRadius = 3,
          Label="YoB: "+nYoB,
        };
        {
          foreach (int nYear in lYears) {
            try {
              double cnt = d[nYear];
              lineSetCurve.Add(new Point(nYear,cnt));
            } catch { }
          }
        }
        if (lineSetCurve.Data.Count>=1) {
          _config.Data.Datasets.Add(lineSetCurve);
        }
      }
    }
  }
}