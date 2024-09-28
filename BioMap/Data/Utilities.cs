using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Timers;
using BioMap.ImageProc;
using Blazor.ImageSurveyor;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Drawing;

namespace BioMap
{
  public static class Utilities
  {
    public static void FireEvent(EventHandler ev, object sender, object e) {
      if (ev != null) {
        foreach (Delegate d in ev.GetInvocationList()) {
          d.DynamicInvoke(new object[] { sender, e });
        }
      }
    }
    public static DateTime DateTime_from_Years(double dYears) {
      int nYears = (int)Math.Floor(dYears);
      var dt = new DateTime(nYears, 1, 1);
      dt += TimeSpan.FromDays((dYears - nYears) * 365);
      return dt;
    }
    /// <summary>
    /// Get array of 3 Rational values containing degrees, minutes and seconds for Exif GPS values from a double GPS value.
    /// </summary>
    /// <param name="dValue"></param>
    /// <returns></returns>
    public static Rational[] LatLngRational_from_double(double dValue) {
      double deg = (int)dValue;
      double min = (int)((dValue - deg) * 60);
      double sec = ((dValue - deg - (min / 60)) * 3600);
      return new Rational[] { new Rational(deg), new Rational(min), new Rational(sec) };
    }
    public static double Years_from_DateTime(DateTime dt) {
      double dYears = dt.Year + Math.Min(365, dt.DayOfYear) / 365.001;
      return dYears;
    }
    public static double CalcTraitScore(double traitValue, double traitValueRef, double normValue) {
      double normedDelta = (traitValue - traitValueRef) / normValue;
      return 1 - Math.Min(2, Math.Abs(normedDelta));
      //return 1.0 / (1.0 + (normedDelta * normedDelta));
    }
    public static string GetPatternImgSource(Element el, DataService ds, SessionData sd, Action<IEnumerable<Vector2[]>> setPolyLines = null) {
      string sPatternImgSrc = "";
      var analyseYellowShare = new AnalyseProcessor();
      var analyseEntropy = new AnalyseProcessor();
      string sMeasureDataJson = JsonConvert.SerializeObject(el.MeasureData);
      PatternImage pi = ds.GetPatternImage(sd, el.ElementName);
      if (pi != null && string.CompareOrdinal(pi.MeasureDataJson, sMeasureDataJson) == 0) {
        setPolyLines?.Invoke(null);
        return pi.DataImageSrc;
      }
      string sSrcFile = ds.GetFilePathForImage(sd.CurrentUser.Project, el.ElementName, true);
      if (System.IO.File.Exists(sSrcFile)) {
        using (var imgSrc = Image.Load(sSrcFile)) {
          imgSrc.Mutate(x => x.AutoOrient());
          Blazor.ImageSurveyor.ImageSurveyorMeasureData md = el.MeasureData;
          if (md != null) {
            (int nWidth, int nHeight) = md.GetPatternSize(md.PatternHeightPx);
            float fPatternRelWidth = (md.normalizer.NormalizedWidthPx * md.PatternRelWidth) / (md.normalizer.NormalizedHeightPx);
            Image imgCropped;
            Vector2[] spineCurvePoints = md.GetSpineCurvePoints(true);
            Vector2[] spine = RoundedCurve.GetPolyLineFromRoundedCurve(spineCurvePoints, (spineCurvePoints.Last() - spineCurvePoints.First()).Length() / 3);
            imgCropped = ImageOperations.TransformCurvedSpineAndCropOutOfImage(imgSrc, spine, fPatternRelWidth, md.PatternRelHeight, new Size(nWidth, nHeight), setPolyLines);
            try {
              if (md.BinaryThresholdMode == 1) {
                imgCropped.Mutate(x => x.BinaryThreshold(md.Threshold, BinaryThresholdMode.Luminance));
              } else if (md.BinaryThresholdMode == 2) {
                imgCropped.Mutate(x => x.BinaryThreshold(md.Threshold, BinaryThresholdMode.Saturation));
              } else {
                imgCropped.Mutate(x => x.MaxChroma(md.Threshold, new[] { new System.Numerics.Vector2(1, 100) }));
              }
              imgCropped.Mutate(x => x.ApplyProcessor(analyseYellowShare));
              Image imgEdges = imgCropped.Clone(x => x.DetectEdges());
              imgEdges.Mutate(x => x.ApplyProcessor(analyseEntropy));
              var bs = new System.IO.MemoryStream();
              imgCropped.SaveAsJpeg(bs);
              sPatternImgSrc = "data:image/png;base64," + Convert.ToBase64String(bs.ToArray());
              if (el.ElementProp?.IndivData != null) {
                el.ElementProp.IndivData.MeasuredData.ShareOfBlack = (float)analyseYellowShare.AnalyseData.ShareOfBlack;
                el.ElementProp.IndivData.MeasuredData.CenterOfMass = (float)(analyseYellowShare.AnalyseData.VerticalCenterOfMass);
                el.ElementProp.IndivData.MeasuredData.StdDeviation = (float)(analyseYellowShare.AnalyseData.VerticalStdDeviation);
                el.ElementProp.IndivData.MeasuredData.Entropy = (float)(1 - analyseEntropy.AnalyseData.ShareOfBlack);
              }
              ds.WritePatternImage(sd, el.ElementName, new PatternImage() { MeasureDataJson = sMeasureDataJson, DataImageSrc = sPatternImgSrc });
            } finally {
              imgCropped.Dispose();
            }
          }
        }
      } else {
        sSrcFile = ds.GetFilePathForImage(sd.CurrentUser.Project, el.ElementName, false);
        if (System.IO.File.Exists(sSrcFile)) {
          using (var imgSrc = Image.Load(sSrcFile)) {
            imgSrc.Mutate(x => x.AutoOrient());
            ImageSurveyorMeasureData md = el.MeasureData;
            (int nWidth, int nHeight) = md.GetPatternSize(300);
            Matrix3x2 mPattern = md.GetPatternMatrix(nHeight);
            using (Image imgCropped = ImageOperations.TransformAndCropOutOfImage(imgSrc, mPattern, new Size(nWidth, nHeight))) {
              if (md.BinaryThresholdMode == 1) {
                imgCropped.Mutate(x => x.BinaryThreshold(md.Threshold, BinaryThresholdMode.Luminance));
              } else if (md.BinaryThresholdMode == 2) {
                imgCropped.Mutate(x => x.BinaryThreshold(md.Threshold, BinaryThresholdMode.Saturation));
              } else {
                imgCropped.Mutate(x => x.MaxChroma(md.Threshold, new[] { new System.Numerics.Vector2(1, 100) }));
              }
              imgCropped.Mutate(x => x.ApplyProcessor(analyseYellowShare));
              Image imgEdges = imgCropped.Clone(x => x.DetectEdges());
              imgEdges.Mutate(x => x.ApplyProcessor(analyseEntropy));
              var bs = new System.IO.MemoryStream();
              imgCropped.SaveAsJpeg(bs);
              sPatternImgSrc = "data:image/png;base64," + Convert.ToBase64String(bs.ToArray());
              if (el.ElementProp?.IndivData != null) {
                el.ElementProp.IndivData.MeasuredData.ShareOfBlack = (float)analyseYellowShare.AnalyseData.ShareOfBlack;
                el.ElementProp.IndivData.MeasuredData.CenterOfMass = (float)(analyseYellowShare.AnalyseData.VerticalCenterOfMass);
                el.ElementProp.IndivData.MeasuredData.StdDeviation = (float)(analyseYellowShare.AnalyseData.VerticalStdDeviation);
                el.ElementProp.IndivData.MeasuredData.Entropy = (float)(1 - analyseEntropy.AnalyseData.ShareOfBlack);
              }
            }
          }
        }
      }
      return sPatternImgSrc;
    }
    public static string GetPatternMatchingImgSource(Element elA, Element elB, SessionData sd) {
      string sPatternImgSrc = "";
      string sPISrcA = GetPatternImgSource(elA, sd.DS, sd);
      string sPISrcB = GetPatternImgSource(elB, sd.DS, sd);
      var analyseYellowShare = new AnalyseProcessor();
      using (var imgA = Image.Load(Convert.FromBase64String(sPISrcA.Substring(22)))) {
        using (var imgB = Image.Load(Convert.FromBase64String(sPISrcB.Substring(22)))) {
          if (imgA != null && imgB != null && imgA.Width == imgB.Width && imgA.Height == imgB.Height) {
            using (var imgR = new Image<SixLabors.ImageSharp.PixelFormats.L8>(imgA.Width, imgA.Height)) {
              imgA.Mutate(x => x.ApplyProcessor(new PatternMatchingProcessor(imgB, Point.Empty, 1f)));
              var bs = new System.IO.MemoryStream();
              imgA.SaveAsJpeg(bs);
              sPatternImgSrc = "data:image/png;base64," + Convert.ToBase64String(bs.ToArray());
            }
          }
        }
      }
      return sPatternImgSrc;
    }
    /// <summary>
    /// Calculate similarity of image patterns in the range from -1 (no match) to +1 (full match).
    /// </summary>
    /// <param name="elA"></param>
    /// <param name="elB"></param>
    /// <param name="sd"></param>
    /// <returns></returns>
    public static double GetPatternMatching(Element elA, Element elB, SessionData sd) {
      double dSimilarity = 0;
      string sPISrcA = GetPatternImgSource(elA, sd.DS, sd);
      string sPISrcB = GetPatternImgSource(elB, sd.DS, sd);
      var analyseYellowShare = new AnalyseProcessor();
      using (var imgA = Image.Load(Convert.FromBase64String(sPISrcA.Substring(22)))) {
        using (var imgB = Image.Load(Convert.FromBase64String(sPISrcB.Substring(22)))) {
          if (imgA != null && imgB != null && imgA.Width == imgB.Width && imgA.Height == imgB.Height) {
            imgA.Mutate(x => x.ApplyProcessor(new PatternMatchingProcessor(imgB, Point.Empty, 1f)));
            imgA.Mutate(x => x.ApplyProcessor(analyseYellowShare));
            double dShareOfBlack = analyseYellowShare.AnalyseData.ShareOfBlack;
            dSimilarity = 2 * dShareOfBlack - 1;
          }
        }
      }
      return dSimilarity;
    }
    private class DelayedCaller
    {
      internal DelayedCaller(int delayMs, Action<object[]> action, params object[] oaArgs) {
        this.DelayMs = delayMs;
        this.Action = action;
        this.Timer = new Timer(delayMs);
        this.Timer.Elapsed += this.Timer_Elapsed;
      }
      private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
        this.Timer.Elapsed -= this.Timer_Elapsed;
        lock (DelayedCallers) {
          DelayedCallers.Remove(this.Action);
        }
        this.Action(this.Args);
      }
      internal Action<object[]> Action;
      internal object[] Args;
      internal Timer Timer;
      internal int DelayMs;
    }
    private static readonly Dictionary<Action<object[]>, DelayedCaller> DelayedCallers = new Dictionary<Action<object[]>, DelayedCaller>();
    /// <summary>
    /// Execute an action after a given delay. If this method is called again for the same action before the action 
    /// has been executed, the delay will start over again.
    /// Typical use case: Each change on a resource must lead to a refresh action, but if several changes occur in a 
    /// small span of time the refresh action shall be executed only once.
    /// </summary>
    /// <param name="delayMs">
    /// The delay in milliseconds.
    /// </param>
    /// <param name="action">
    /// The action.
    /// </param>
    public static void CallDelayed(int delayMs, Action<object[]> action, params object[] oaArgs) {
      lock (DelayedCallers) {
        if (!DelayedCallers.TryGetValue(action, out DelayedCaller dc)) {
          dc = new DelayedCaller(delayMs, action, oaArgs);
          DelayedCallers[action] = dc;
        }
        dc.Timer.Stop();
        dc.Args = oaArgs;
        dc.Timer.Start();
      }
    }
    /// <summary>
    /// The differing core parts of two strings, excluding the identical parts from left and from right. The strings may have 
    /// different lengths. Example: A='ab123xyz', B='ab19yz' --> result=['23x','9'].
    /// </summary>
    /// <param name="A">
    /// The first string.
    /// </param>
    /// <param name="B">
    /// The second string.
    /// </param>
    /// <param name="nNeigboringChars">
    /// The number of characters left and right of the differing core parts to be included in the result.
    /// </param>
    /// <returns>
    /// Array of two strings: the differing part of the first and of the second string; null if the strings are equal.
    /// </returns>
    public static string[] FindDifferingCoreParts(string A, string B, int nNeigboringChars = 10) {
      if (string.IsNullOrEmpty(A)) {
        if (string.IsNullOrEmpty(B)) {
          return null;
        } else {
          return new[] { "", B };
        }
      } else if (string.IsNullOrEmpty(B)) {
        return new[] { A, "" };
      } else if (string.CompareOrdinal(A, B) == 0) {
        return null;
      }
      int lenA = A.Length;
      int lenB = B.Length;
      int lenMin = Math.Min(lenA, lenB);
      int idxL = 0;
      while (idxL < lenMin) {
        if (A[idxL] != B[idxL]) {
          break;
        }
        idxL++;
      }
      if (idxL >= lenA) {
        return new[] { "", B.Substring(Math.Max(0, idxL - nNeigboringChars)) };
      } else if (idxL >= lenB) {
        return new[] { A.Substring(Math.Max(0, idxL - nNeigboringChars)), "" };
      } else {
        int idxR = 0;
        while (idxR < lenMin - idxL) {
          if (A[lenA - idxR - 1] != B[lenB - idxR - 1]) {
            break;
          }
          idxR++;
        }
        idxL = Math.Max(0, idxL - nNeigboringChars);
        idxR = Math.Max(0, idxR - nNeigboringChars);
        return new[] { A.Substring(idxL, lenA - idxR - idxL), B.Substring(idxL, lenB - idxR - idxL) };
      }
    }
    public static string Bash(this string cmd) {
      string escapedArgs = cmd.Replace("\"", "\\\"");
      var process = new Process() {
        StartInfo = new ProcessStartInfo {
          FileName = "/bin/bash",
          Arguments = $"-c \"{escapedArgs}\"",
          RedirectStandardOutput = true,
          UseShellExecute = false,
          CreateNoWindow = true,
        }
      };
      process.Start();
      string result = process.StandardOutput.ReadToEnd();
      process.WaitForExit();
      return result;
    }
    public static float GetScale(System.Numerics.Matrix3x2 m) {
      var v0t = System.Numerics.Vector2.Transform(System.Numerics.Vector2.Zero, m);
      var v1t = System.Numerics.Vector2.Transform(System.Numerics.Vector2.One, m);
      float fScale = (v1t - v0t).Length();
      return fScale;
    }
  }
}
