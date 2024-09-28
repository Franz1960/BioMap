using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
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
    private int progressCompletion = 0;

    protected override void OnInitialized() {
      base.OnInitialized();
      this._config = new LineConfig {
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
                                }
                            }
                        },
          },
          Tooltips = new Tooltips {
            Mode = InteractionMode.Nearest,
          }
        },
      };
      this.SD.Filters.FilterChanged += (sender, ev) => {
        this.RefreshData();
        base.InvokeAsync(this.StateHasChanged);
      };
      this.RefreshData();
    }
    private void CheckBoxShowVintageBoundaries_CheckedChanged(ChangeEventArgs e) {
      this.SD.SizeTimeChartShowVintageBoundaries = bool.Parse(e.Value.ToString());
      this.RefreshData();
      base.InvokeAsync(this.StateHasChanged);
    }
    private void GrowingCurveMode_SelectedValueChanged(string e) {
      this.SD.SizeTimeChartGrowingCurveMode = e;
      this.RefreshData();
      base.InvokeAsync(this.StateHasChanged);
    }
    private async Task OnSaveYoBClick() {
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      await Task.Run(() => {
        try {
          Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD, this.SD.Filters, null, true);
          int idx = 0;
          foreach (int iid in aaIndisByIId.Keys) {
            idx++;
            try {
              this.progressCompletion = ((idx + 1) * 100) / aaIndisByIId.Count;
              this.InvokeAsync(() => this.StateHasChanged());
              // Wachstumskurve.
              var growthFunc = GrowthFunc.FromCatches(aaIndisByIId[iid]);
              if (growthFunc != null) {
                foreach (Element el in aaIndisByIId[iid].ToArray()) {
                  try {
                    el.ElementProp.IndivData.DateOfBirth = growthFunc.DateOfBirth;
                    this.DS.WriteElement(this.SD, el);
                  } catch { }
                }
              }
            } catch { }
          }
        } finally {
          this.InvokeAsync(() => { this.progressModalRef.Hide(); this.StateHasChanged(); });
        }
      });
      this.RefreshData();
      await base.InvokeAsync(this.StateHasChanged);
    }
    private void RefreshData() {
      DateTime dtProjectStart = this.SD.CurrentProject.StartDate.Value;
      Dictionary<int, List<Element>> aaIndisByIId = this.DS.GetIndividuals(this.SD, this.SD.Filters, null, true);
      this._config.Data.Datasets.Clear();
      // Ideale Wachstumskurven hinzufügen.
      if (this.SD.SizeTimeChartShowVintageBoundaries) {
        for (int nYoB = 2012; nYoB <= DateTime.Now.Year; nYoB++) {
          string sYobColor = Element.GetColorForYearOfBirth(nYoB);
          var lineSet = new LineDataset<Point> {
            BackgroundColor = ColorUtil.FromDrawingColor(System.Drawing.Color.White),
            BorderWidth = 4,
            BorderDash = new[] { 10, 5 },
            PointHoverBorderWidth = 4,
            BorderColor = sYobColor,
            PointRadius = 0,
            CubicInterpolationMode = CubicInterpolationMode.Monotone,
            Fill = FillingMode.Disabled,
            //ShowLine = true,
          };
          var fg = new GrowthFunc() {
            DateOfBirth = new DateTime(nYoB, 1, 1) + TimeSpan.FromDays(GrowthFunc.SeasonStartDay - GrowthFunc.MaxAddDaysInFirstSeason),
          };
          for (DateTime dt = dtProjectStart; dt < new DateTime((DateTime.Now - TimeSpan.FromDays(100)).Year, 11, 1); dt += TimeSpan.FromDays(7)) {
            try {
              double l = fg.GetSize(dt);
              if (l > 10) {
                lineSet.Add(new Point(this.SD.GetSeasonizedTime(dt), l));
              }
            } catch { }
          }
          if (lineSet.Data.Count >= 2) {
            this._config.Data.Datasets.Add(lineSet);
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
      foreach (int idx in aaIndisByIId.Keys) {
        try {
          if (string.CompareOrdinal(this.SD.SizeTimeChartGrowingCurveMode, "GrowingCurve") == 0) {
            // Wachstumskurve.
            bool bIncludeSinglePoints = true;
            DateTime? dtFittedYearOfBirth = null;
            if (aaIndisByIId[idx].Count >= (bIncludeSinglePoints ? 1 : 2)) {
              DateTime? dtDateOfBirth = null;
              var ldaPoints = new List<double[]>();
              foreach (Element el in aaIndisByIId[idx]) {
                try {
                  double l = el.ElementProp.IndivData.MeasuredData.HeadBodyLength;
                  if (l != 0) {
                    double t = Utilities.Years_from_DateTime(el.ElementProp.CreationTime);
                    ldaPoints.Add(new double[] { t, l });
                    dtDateOfBirth = el.ElementProp.IndivData?.DateOfBirth;
                  }
                } catch { }
              }
              if (ldaPoints.Count >= (bIncludeSinglePoints ? 1 : 2)) {
                var growthFunc = new GrowthFunc() {
                  DateOfBirth = dtDateOfBirth.Value,
                };
                string sYobColor = Element.GetColorForYearOfBirth(growthFunc.DateOfBirth.Year);
                var lineSetCurve = new LineDataset<Point> {
                  BackgroundColor = sYobColor,
                  BorderWidth = 2,
                  PointHoverBorderWidth = 0,
                  BorderColor = sYobColor,
                  PointRadius = 0,
                  Fill = FillingMode.Disabled,
                  CubicInterpolationMode = CubicInterpolationMode.Monotone,
                  Label = "YoB: " + dtFittedYearOfBirth?.ToString("yyyy-MM-dd"),
                };
                {
                  DateTime dtB = aaIndisByIId[idx][0].ElementProp.CreationTime;
                  if (bIncludeSinglePoints) {
                    dtB = (dtProjectStart < growthFunc.DateOfBirth) ? growthFunc.DateOfBirth : dtProjectStart;
                  }
                  DateTime dtE = aaIndisByIId[idx][aaIndisByIId[idx].Count - 1].ElementProp.CreationTime;
                  TimeSpan tsT = (dtE - TimeSpan.FromSeconds(30)) - dtB;
                  int nSteps = Math.Max(1, (int)Math.Ceiling(tsT / TimeSpan.FromDays(7)));
                  var tsDelta = new TimeSpan(tsT.Ticks / nSteps);
                  for (DateTime dt = dtB; dt <= dtE; dt += tsDelta) {
                    try {
                      double l = growthFunc.GetSize(dt);
                      if (l > 10) {
                        lineSetCurve.Add(new Point(this.SD.GetSeasonizedTime(dt), l));
                      }
                    } catch { }
                  }
                }
                if (lineSetCurve.Data.Count >= 2) {
                  this._config.Data.Datasets.Add(lineSetCurve);
                  //_config.Options.Legend.Display=true;
                }
              }
            }
            // Datenpunkte.
            {
              string sYobColor = Element.GetColorForYearOfBirth(dtFittedYearOfBirth.HasValue ? dtFittedYearOfBirth.Value.Year : aaIndisByIId[idx][0].ElementProp.IndivData?.DateOfBirth.Year);
              var lineSetPoints = new LineDataset<Point> {
                BackgroundColor = sYobColor,
                BorderWidth = 0,
                PointHoverBorderWidth = 4,
                BorderColor = sYobColor,
                PointRadius = 3,
                Fill = FillingMode.Disabled,
                ShowLine = false,
              };
              foreach (Element el in aaIndisByIId[idx]) {
                try {
                  double l = el.ElementProp.IndivData.MeasuredData.HeadBodyLength;
                  if (l > 0) {
                    lineSetPoints.Add(new Point(this.SD.GetSeasonizedTime(el.ElementProp.CreationTime), l));
                  }
                } catch { }
              }
              if (lineSetPoints.Data.Count >= 1) {
                this._config.Data.Datasets.Add(lineSetPoints);
              }
            }
          } else {
            // Interpolation durch Datenpunkte.
            string sYobColor = "rgba(100,100,100,0.5)";
            var lineSet = new LineDataset<Point> {
              BackgroundColor = sYobColor,
              BorderWidth = 2,
              PointHoverBorderWidth = 4,
              BorderColor = sYobColor,
              PointRadius = 3,
              Fill = FillingMode.Disabled,
              CubicInterpolationMode = CubicInterpolationMode.Monotone,
              //ShowLine = true,
            };
            if (string.CompareOrdinal(this.SD.SizeTimeChartGrowingCurveMode, "-") == 0) {
              lineSet.ShowLine = false;
            } else if (string.CompareOrdinal(this.SD.SizeTimeChartGrowingCurveMode, "Linear") == 0) {
              lineSet.LineTension = 0;
            }
            foreach (Element el in aaIndisByIId[idx]) {
              try {
                double l = el.ElementProp.IndivData.MeasuredData.HeadBodyLength;
                if (l != 0) {
                  lineSet.Add(new Point(this.SD.GetSeasonizedTime(el.ElementProp.CreationTime), l));
                }
              } catch { }
            }
            if (lineSet.Data.Count >= 1) {
              this._config.Data.Datasets.Add(lineSet);
            }
          }
        } catch {
        }
      }
    }
  }
}
