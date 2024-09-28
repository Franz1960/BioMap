using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Blazor.ImageSurveyor
{
  public class RoundedCurve
  {
    public static Vector2[] GetPolyLineFromRoundedCurve(
      Vector2[] points,
      float? Radius = null,
      float MaxDeviation = 3.0f) {
      //
      var vaPolyLine = new List<Vector2>();
      //
      if (points.Length >= 2) {
        float fRadius = Radius ?? (points.Last() - points.First()).Length() / 3;
        vaPolyLine.Add(points[0]);
        for (int iPoint = 0; iPoint < points.Length; iPoint++) {
          Vector2 ptf0 = points[(iPoint == 0) ? (points.Length - 1) : (iPoint - 1)];
          Vector2 ptf1 = points[iPoint];
          Vector2 ptf2 = points[(iPoint == points.Length - 1) ? 0 : (iPoint + 1)];
          float r = fRadius;
          //
          if (r == 0 || iPoint == 0 || iPoint == points.Length - 1) {
            // Es ist ein scharfes Eck.
            if (iPoint != 0) {
              vaPolyLine.Add(ptf1);
            }
          } else {
            // Es ist ein abgerundetes Eck.
            double fAlpha1 = Math.Atan2(ptf1.Y - ptf0.Y, ptf1.X - ptf0.X);
            double fAlpha2 = Math.Atan2(ptf2.Y - ptf1.Y, ptf2.X - ptf1.X);
            double f2Beta = fAlpha1 + Math.PI - fAlpha2;
            while (f2Beta > 2 * Math.PI) {
              f2Beta -= 2 * Math.PI;
            }
            while (f2Beta <= 0) {
              f2Beta += 2 * Math.PI;
            }
            if (f2Beta > 0) {
              double fBeta = 0.5 * f2Beta;
              int nSign = (f2Beta < Math.PI) ? 1 : -1;
              double fA = Math.Abs(r / Math.Tan(fBeta));
              var ptfT0 = new Vector2(ptf1.X - (float)(fA * Math.Cos(fAlpha1)), ptf1.Y - (float)(fA * Math.Sin(fAlpha1)));
              var ptfT1 = new Vector2(ptf1.X + (float)(fA * Math.Cos(fAlpha2)), ptf1.Y + (float)(fA * Math.Sin(fAlpha2)));
              double fC = nSign * Math.Sqrt((fA * fA) + (r * r));
              double fGamma = fAlpha1 - fBeta;
              var ptfM = new Vector2(ptf1.X - (float)(fC * Math.Cos(fGamma)), ptf1.Y - (float)(fC * Math.Sin(fGamma)));
              float fStartAngle = (float)(fAlpha1 - (nSign * 0.5 * Math.PI));
              float fSweepAngle = (float)(Math.PI - f2Beta);
              //
              if (iPoint != 0 && vaPolyLine.Count == 0) {
                vaPolyLine.Add(ptfT0);
              }
              {
                var angleStepRad = Math.Acos(r / (r + MaxDeviation));
                int nVertices = 1 + (int)Math.Ceiling(Math.Abs(fSweepAngle) / angleStepRad);
                for (int i = 0; i <= nVertices; i++) {
                  float phi = fStartAngle + ((fSweepAngle * i) / nVertices);
                  var vB = new Vector2(
                    (float)(ptfM.X + (r * Math.Cos(phi))),
                    (float)(ptfM.Y + (r * Math.Sin(phi))));
                  vaPolyLine.Add(vB);
                }
              }
            }
          }
        }
      }
      return vaPolyLine.ToArray();
    }
    public static float GetPolyLineLength(Vector2[] polyLine) {
      float fLengthSum = 0;
      for (int i = 1; i < polyLine.Length; i++) {
        fLengthSum += (polyLine[i] - polyLine[i - 1]).Length();
      }
      return fLengthSum;
    }
  }
}
