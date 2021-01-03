using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Blazorise;
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
  public partial class SizeTimeChart : ComponentBase
  {
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private LineConfig _config;
    private Chart _chartJs;
    private Modal progressModalRef;
    private int progressCompletion=0;

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
            Display = false,
          },
          Scales = new Scales {
            XAxes = new List<CartesianAxis>
            {
            new TimeAxis
            {
              ScaleLabel = new ScaleLabel
              {
                LabelString = Localize["Time"]
              },
              Time = new TimeOptions
              {
                Unit = TimeMeasurement.Month,
                Round = TimeMeasurement.Day,
                TooltipFormat = "DD.MM.YYYY",
              },
            }
          },
          },
          Tooltips=new Tooltips {
            Mode=InteractionMode.Nearest,
          }
        },
      };
      SD.Filters.FilterChanged+=(sender,ev) => {
        RefreshData();
        base.InvokeAsync(StateHasChanged);
      };
      RefreshData();
    }
    private void CheckBoxShowVintageBoundaries_CheckedChanged(ChangeEventArgs e) {
      SD.SizeTimeChartShowVintageBoundaries=bool.Parse(e.Value.ToString());
      RefreshData();
      base.InvokeAsync(StateHasChanged);
    }
    private void GrowingCurveMode_SelectedValueChanged(string e) {
      SD.SizeTimeChartGrowingCurveMode=e;
      RefreshData();
      base.InvokeAsync(StateHasChanged);
    }
    private async Task OnSaveYoBClick() {
      progressCompletion=0;
      progressModalRef.Show();
      await Task.Run(()=>{
        try {
          var aaIndisByIId = DS.GetIndividuals(SD,SD.Filters);
          foreach (var idx in aaIndisByIId.Keys) {
            try {
              progressCompletion=((idx+1)*100)/aaIndisByIId.Count;
              this.InvokeAsync(()=>{ StateHasChanged(); });
              // Wachstumskurve.
              DateTime? dtFittedYearOfBirth = null;
              var lsf = new LeastSquareFit();
              if (aaIndisByIId[idx].Count>=1) {
                DateTime? dtDateOfBirth = null;
                var ldaPoints = new List<double[]>();
                foreach (var el in aaIndisByIId[idx]) {
                  try {
                    var l = el.ElementProp.IndivData.MeasuredData.HeadBodyLength;
                    if (l!=0) {
                      double t = Utilities.Years_from_DateTime(el.ElementProp.CreationTime);
                      ldaPoints.Add(new double[] { t,l });
                      dtDateOfBirth=el.ElementProp.IndivData?.DateOfBirth;
                    }
                  } catch { }
                }
                if (ldaPoints.Count>=1) {
                  var dtFirstPoint = Utilities.DateTime_from_Years(ldaPoints[0][0]);
                  double? dMinYear = null;
                  var dFirstLength = ldaPoints[0][1];
                  var dtEarliestHatchTimeInFirstYear = new DateTime(dtFirstPoint.Year,1,1)+TimeSpan.FromDays(GrowthFunc.SeasonStartDay-GrowthFunc.MaxAddDaysInFirstSeason);
                  double dMaxLengthFirstYear = GrowthFunc.GetSizeForNetGrowingTime(((double)(dtFirstPoint-dtEarliestHatchTimeInFirstYear).Days)/GrowthFunc.SeasonLengthDays);
                  if (dFirstLength<dMaxLengthFirstYear) {
                    dMinYear = Utilities.Years_from_DateTime(new DateTime(dtFirstPoint.Year,1,1)+TimeSpan.FromDays(GrowthFunc.SeasonStartDay-GrowthFunc.MaxAddDaysInFirstSeason));
                  }
                  if (!dMinYear.HasValue) {
                    dMinYear=dtFirstPoint.Year-9;
                  }
                  double dMaxYear = Utilities.Years_from_DateTime(dtFirstPoint);
                  lsf.Optimize(
                    new double[][] { new double[] { dMinYear.Value,dMaxYear } },
                    ldaPoints.ToArray(),
                    (daParams,daaPoints) => {
                      double dyTimeOfBirth = daParams[0];
                      var fg = new GrowthFunc() {
                        DateOfBirth=Utilities.DateTime_from_Years(dyTimeOfBirth),
                      };
                      //System.Diagnostics.Debug.Write(fg.DateOfBirth.ToString()+": ");
                      double dDevSum = 0;
                        for (int i = 0;i<daaPoints.Length;i++) {
                          double dyTime = daaPoints[i][0];
                          double lReal = daaPoints[i][1];
                          double lCalc = fg.GetSize(dyTime);
                          double dDev = lReal-lCalc;
                          dDevSum+=(dDev*dDev);
                        //System.Diagnostics.Debug.Write(" Dev="+ConvInvar.ToDecimalString(dDev,5));
                      }
                      //System.Diagnostics.Debug.WriteLine(" DevSum="+ConvInvar.ToDecimalString(dDevSum,5));
                      return dDevSum;
                    },
                    -0.02,
                    0.0001,
                    out double[] daBestParams,
                    LeastSquareFit.Method.Directed);
                  dtFittedYearOfBirth = Utilities.DateTime_from_Years(daBestParams[0]);
                  {
                    foreach (var el in aaIndisByIId[idx].ToArray()) {
                      try {
                        el.ElementProp.IndivData.DateOfBirth=dtFittedYearOfBirth.Value;
                        DS.WriteElement(el);
                      } catch { }
                    }
                  }
                  dtDateOfBirth=dtFittedYearOfBirth.Value;
                }
              }
            } catch { }
          }
        } finally {
          this.InvokeAsync(()=>{  progressModalRef.Hide(); StateHasChanged(); });
        }
      });
      RefreshData();
      await base.InvokeAsync(StateHasChanged);
    }
    private void RefreshData() {
      var dtProjectStart = DS.ProjectStart;
      var aaIndisByIId = DS.GetIndividuals(SD,SD.Filters);
      _config.Data.Datasets.Clear();
      // Ideale Wachstumskurven hinzufügen.
      if (SD.SizeTimeChartShowVintageBoundaries) {
        for (int nYoB = 2012;nYoB<2021;nYoB++) {
          string sYobColor = Element.GetColorForYearOfBirth(nYoB);
          var lineSet = new LineDataset<TimePoint> {
            BackgroundColor = ColorUtil.FromDrawingColor(System.Drawing.Color.White),
            BorderWidth = 4,
            BorderDash= new[] { 10,5 },
            PointHoverBorderWidth = 4,
            BorderColor = sYobColor,
            PointRadius = 0,
            CubicInterpolationMode = CubicInterpolationMode.Monotone,
            Fill=FillingMode.Disabled,
            //ShowLine = true,
          };
          var fg = new GrowthFunc() {
            DateOfBirth=new DateTime(nYoB,1,1)+TimeSpan.FromDays(GrowthFunc.SeasonStartDay-GrowthFunc.MaxAddDaysInFirstSeason),
          };
          for (var dt = dtProjectStart;dt<new DateTime((DateTime.Now-TimeSpan.FromDays(100)).Year,11,1);dt+=TimeSpan.FromDays(7)) {
            try {
              var l = fg.GetSize(dt);
              if (l > 10) {
                lineSet.Add(new TimePoint(dt,l));
              }
            } catch { }
          }
          if (lineSet.Data.Count>=2) {
            _config.Data.Datasets.Add(lineSet);
          }
        }
#if trueX
// Kurvenschar hinzufügen.
for (var dtB = new DateTime(2012,1,1);dtB.Year<2021;dtB+=TimeSpan.FromDays(7)) {
string sYobColor = Element.GetColorForYearOfBirth(dtB.Year);
var lineSet = new LineDataset<TimePoint> {
  BackgroundColor = sYobColor,
  BorderWidth = 2,
  PointHoverBorderWidth = 4,
  BorderColor = sYobColor,
  PointRadius = 0,
  CubicInterpolationMode = CubicInterpolationMode.Monotone,
  Fill=FillingMode.Disabled,
  //ShowLine = true,
};
var fg = new GrowthFunc() {
  DateOfBirth=dtB,
};
//for (var dt = dtProjectStart;dt<new DateTime(2020,11,1);dt+=TimeSpan.FromDays(7)) {
for (var dt = new DateTime(2015,11,1);dt<new DateTime(2020,11,1);dt+=TimeSpan.FromDays(7)) {
  try {
    var l = fg.GetSize(dt);
    if (l > 10) {
      lineSet.Add(new TimePoint(dt,l));
    }
  } catch { }
}
if (lineSet.Data.Count>=2) {
  _config.Data.Datasets.Add(lineSet);
}
}
#endif
      }
      // Wachstumskurven der Individuen hinzufügen.
      foreach (var idx in aaIndisByIId.Keys) {
        try {
          if (string.CompareOrdinal(SD.SizeTimeChartGrowingCurveMode,"GrowingCurve")==0) {
            // Wachstumskurve.
            bool bIncludeSinglePoints = true;
            DateTime? dtFittedYearOfBirth = null;
            var lsf = new LeastSquareFit();
            if (aaIndisByIId[idx].Count>=(bIncludeSinglePoints ? 1 : 2)) {
              DateTime? dtDateOfBirth = null;
              var ldaPoints = new List<double[]>();
              foreach (var el in aaIndisByIId[idx]) {
                try {
                  var l = el.ElementProp.IndivData.MeasuredData.HeadBodyLength;
                  if (l!=0) {
                    double t = Utilities.Years_from_DateTime(el.ElementProp.CreationTime);
                    ldaPoints.Add(new double[] { t,l });
                    dtDateOfBirth=el.ElementProp.IndivData?.DateOfBirth;
                  }
                } catch { }
              }
              if (ldaPoints.Count>=(bIncludeSinglePoints ? 1 : 2)) {
                var growthFunc = new GrowthFunc() {
                  DateOfBirth=dtDateOfBirth.Value,
                };
                string sYobColor = Element.GetColorForYearOfBirth(growthFunc.DateOfBirth.Year);
                var lineSetCurve = new LineDataset<TimePoint> {
                  BackgroundColor = sYobColor,
                  BorderWidth = 2,
                  PointHoverBorderWidth = 0,
                  BorderColor = sYobColor,
                  PointRadius = 0,
                  Fill=FillingMode.Disabled,
                  CubicInterpolationMode = CubicInterpolationMode.Monotone,
                  Label="YoB: "+dtFittedYearOfBirth?.ToString("yyyy-MM-dd"),
                };
                {
                  var dtB = aaIndisByIId[idx][0].ElementProp.CreationTime;
                  if (bIncludeSinglePoints) {
                    dtB=(dtProjectStart<growthFunc.DateOfBirth) ? growthFunc.DateOfBirth : dtProjectStart;
                  }
                  var dtE = aaIndisByIId[idx][aaIndisByIId[idx].Count-1].ElementProp.CreationTime;
                  var tsT = (dtE-TimeSpan.FromSeconds(30))-dtB;
                  int nSteps = Math.Max(1,(int)Math.Ceiling(tsT/TimeSpan.FromDays(7)));
                  var tsDelta = new TimeSpan(tsT.Ticks/nSteps);
                  for (var dt = dtB;dt<=dtE;dt+=tsDelta) {
                    try {
                      var l = growthFunc.GetSize(dt);
                      if (l > 10) {
                        lineSetCurve.Add(new TimePoint(dt,l));
                      }
                    } catch { }
                  }
                }
                if (lineSetCurve.Data.Count>=2) {
                  _config.Data.Datasets.Add(lineSetCurve);
                  //_config.Options.Legend.Display=true;
                }
              }
            }
            // Datenpunkte.
            {
              string sYobColor = Element.GetColorForYearOfBirth(dtFittedYearOfBirth.HasValue ? dtFittedYearOfBirth.Value.Year : aaIndisByIId[idx][0].ElementProp.IndivData?.DateOfBirth.Year);
              var lineSetPoints = new LineDataset<TimePoint> {
                BackgroundColor = sYobColor,
                BorderWidth = 0,
                PointHoverBorderWidth = 4,
                BorderColor = sYobColor,
                PointRadius = 3,
                Fill=FillingMode.Disabled,
                ShowLine = false,
              };
              foreach (var el in aaIndisByIId[idx]) {
                try {
                  var l = el.ElementProp.IndivData.MeasuredData.HeadBodyLength;
                  if (l>0) {
                    lineSetPoints.Add(new TimePoint(el.ElementProp.CreationTime,l));
                  }
                } catch { }
              }
              if (lineSetPoints.Data.Count>=1) {
                _config.Data.Datasets.Add(lineSetPoints);
              }
            }
          } else {
            // Interpolation durch Datenpunkte.
            string sYobColor = "rgba(100,100,100,0.5)";
            var lineSet = new LineDataset<TimePoint> {
              BackgroundColor = sYobColor,
              BorderWidth = 2,
              PointHoverBorderWidth = 4,
              BorderColor = sYobColor,
              PointRadius = 3,
              Fill=FillingMode.Disabled,
              CubicInterpolationMode = CubicInterpolationMode.Monotone,
              //ShowLine = true,
            };
            if (string.CompareOrdinal(SD.SizeTimeChartGrowingCurveMode,"-")==0) {
              lineSet.ShowLine=false;
            } else if (string.CompareOrdinal(SD.SizeTimeChartGrowingCurveMode,"Linear")==0) {
              lineSet.LineTension=0;
            }
            foreach (var el in aaIndisByIId[idx]) {
              try {
                var l = el.ElementProp.IndivData.MeasuredData.HeadBodyLength;
                if (l!=0) {
                  lineSet.Add(new TimePoint(el.ElementProp.CreationTime,l));
                }
              } catch { }
            }
            if (lineSet.Data.Count>=1) {
              _config.Data.Datasets.Add(lineSet);
            }
          }
        } catch {
        }
      }
    }
  }
}