using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Blazor.ImageSurveyor
{
  /// <summary>
  /// MapOptions object used to define the properties that can be set on a Map.
  /// </summary>
  [JsonObject(MemberSerialization.Fields)]
  public class ImageSurveyorMeasureData
  {
    public static readonly ImageSurveyorMeasureData Empty = new ImageSurveyorMeasureData() { normalizer = new ImageSurveyorNormalizer("") };
    public ImageSurveyorNormalizer normalizer;
    public Vector2[] normalizePoints;
    public Vector2[] measurePoints;
    public float GetScaleToNormalize() {
      var mp = this.measurePoints;
      float fScale = (mp[3] - mp[2]).Length() / (mp[1] - mp[0]).Length();
      return fScale;
    }
    public float GetHeadBodyLengthPx(bool bRaw) {
      Vector2[] spineCurvePoints = this.GetSpineCurvePoints(bRaw);
      Vector2[] spinePolyLine = RoundedCurve.GetPolyLineFromRoundedCurve(spineCurvePoints);
      float fResult = RoundedCurve.GetPolyLineLength(spinePolyLine);
      return fResult;
    }
    /// <summary>
    /// Height of pattern image in pixels.
    /// </summary>
    public int PatternHeightPx => 300;
    /// <summary>
    /// Relative width of pattern image with reference to head-body-length.
    /// </summary>
    public float PatternRelWidth => this.normalizer.PatternRelWidth;
    /// <summary>
    /// Relative height of pattern image with reference to head-body-length.
    /// </summary>
    public float PatternRelHeight => this.normalizer.PatternRelHeight;
    /// <summary>
    /// Threshold value for pattern generation by MaxChroma algorithm.
    /// </summary>
    public float Threshold = 0.10f;
    /// <summary>
    /// Mode of binarisation: 0=invalid, 1=Luminance, 2=Saturation, 3=MaxChroma.
    /// </summary>
    public int BinaryThresholdMode = 3;
    //
    public Vector2[] GetSpineCurvePoints(bool bRaw) {
      var lPoints = new List<Vector2>();
      if (this.measurePoints.Length >= 4) {
        lPoints.Add(this.measurePoints[bRaw ? 0 : 2]);
        for (int i = 4; i < this.measurePoints.Length; i += 2) {
          lPoints.Add(this.measurePoints[i + (bRaw ? 0 : 1)]);
        }
        lPoints.Add(this.measurePoints[bRaw ? 1 : 3]);
      }
      return lPoints.ToArray();
    }
    public Matrix3x2 GetNormalizeMatrix() {
      var mResult = Matrix3x2.Identity;
      if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakInPetriDish") == 0) {
        try {
          var md = this;
          mResult = Matrix3x2.Identity;
          if (md.normalizePoints != null && md.normalizePoints.Length >= 3) {
            var circle = GetCircleFrom3Points(
              md.normalizePoints[0].X,
              md.normalizePoints[0].Y,
              md.normalizePoints[1].X,
              md.normalizePoints[1].Y,
              md.normalizePoints[2].X,
              md.normalizePoints[2].Y);
            float fScale = circle.Radius / (float)((md.normalizer.NormalizeReference / 2) / md.normalizer.NormalizePixelSize);
            var ptBoxCenter = new Vector2 {
              X = (md.measurePoints[0].X + md.measurePoints[1].X) / 2,
              Y = (md.measurePoints[0].Y + md.measurePoints[1].Y) / 2,
            };
            float phi = MathF.Atan2(md.measurePoints[0].Y - md.measurePoints[1].Y, md.measurePoints[0].X - md.measurePoints[1].X);
            //
            mResult =
              Matrix3x2.CreateTranslation(-ptBoxCenter.X, -ptBoxCenter.Y) *
              Matrix3x2.CreateRotation(-MathF.PI / 2 - phi) *
              Matrix3x2.CreateScale(1 / fScale) *
              Matrix3x2.CreateTranslation(md.normalizer.NormalizedWidthPx / 2, md.normalizer.NormalizedHeightPx / 2);
          }
        } catch { }
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakIn50mmCuvette") == 0) {
        try {
          var md = this;
          float fNormWidthPixels = (
              new Vector2(md.normalizePoints[1].X, md.normalizePoints[1].Y)
              -
              new Vector2(md.normalizePoints[0].X, md.normalizePoints[0].Y)
              ).Length();
          float fScale = fNormWidthPixels / (float)((md.normalizer.NormalizeReference) / md.normalizer.NormalizePixelSize);
          var ptBoxCenter = new Vector2 {
            X = (md.measurePoints[0].X + md.measurePoints[1].X) / 2,
            Y = (md.measurePoints[0].Y + md.measurePoints[1].Y) / 2,
          };
          float phi = MathF.Atan2(md.measurePoints[0].Y - md.measurePoints[1].Y, md.measurePoints[0].X - md.measurePoints[1].X);
          //
          mResult =
            Matrix3x2.CreateTranslation(-ptBoxCenter.X, -ptBoxCenter.Y) *
            Matrix3x2.CreateRotation(-MathF.PI / 2 - phi) *
            Matrix3x2.CreateScale(1 / fScale) *
            Matrix3x2.CreateTranslation(md.normalizer.NormalizedWidthPx / 2, md.normalizer.NormalizedHeightPx / 2);
        } catch { }
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "CropRectangle") == 0) {
        try {
          var md = this;
          //
          mResult =
            Matrix3x2.CreateTranslation(-md.normalizePoints[0].X, -md.normalizePoints[0].Y);
        } catch { }
      }
      return mResult;
    }
    public Matrix3x2 GetPatternMatrix(int nPatternHeight) {
      var md = this;
      var mResult = this.GetNormalizeMatrix();
      (int nWidth, int nHeight) = md.GetPatternSize(nPatternHeight);
      if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakInPetriDish") == 0) {
        try {
          var vCenter = (md.measurePoints[2] + md.measurePoints[3]) / 2;
          var fLength = (md.measurePoints[2] - md.measurePoints[3]).Length();
          float fScale = fLength * this.PatternRelHeight / nPatternHeight;
          float phi = MathF.Atan2(md.measurePoints[2].Y - md.measurePoints[3].Y, md.measurePoints[2].X - md.measurePoints[3].X);
          //
          mResult =
            mResult *
            Matrix3x2.CreateTranslation(-vCenter) *
            Matrix3x2.CreateRotation(-MathF.PI / 2 - phi) *
            Matrix3x2.CreateScale(1 / fScale) *
            Matrix3x2.CreateTranslation(nWidth / 2, nHeight / 2);
        } catch { }
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakIn50mmCuvette") == 0) {
        try {
          var vCenter = (md.measurePoints[2] + md.measurePoints[3]) / 2;
          var fLength = (md.measurePoints[2] - md.measurePoints[3]).Length();
          float fScale = fLength * this.PatternRelHeight / nPatternHeight;
          float phi = MathF.Atan2(md.measurePoints[2].Y - md.measurePoints[3].Y, md.measurePoints[2].X - md.measurePoints[3].X);
          //
          mResult =
            mResult *
            Matrix3x2.CreateTranslation(-vCenter) *
            Matrix3x2.CreateRotation(-MathF.PI / 2 - phi) *
            Matrix3x2.CreateScale(1 / fScale) *
            Matrix3x2.CreateTranslation(nWidth / 2, nHeight / 2);
        } catch { }
      }
      return mResult;
    }
    public (int, int) GetNormalizedSize() {
      if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakInPetriDish") == 0) {
        return (this.normalizer.NormalizedWidthPx, this.normalizer.NormalizedHeightPx);
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakIn50mmCuvette") == 0) {
        return (this.normalizer.NormalizedWidthPx, this.normalizer.NormalizedHeightPx);
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "CropRectangle") == 0) {
        return ((int)(this.normalizePoints[1].X - this.normalizePoints[0].X), (int)(this.normalizePoints[1].Y - this.normalizePoints[0].Y));
      }
      return (0, 0);
    }
    public (int, int) GetPatternSize(int nPatternHeight) {
      float fAspectRatio = ((float)this.normalizer.NormalizedWidthPx) / this.normalizer.NormalizedHeightPx;
      if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakInPetriDish") == 0) {
        return ((int)(fAspectRatio * (this.PatternRelWidth * nPatternHeight) / this.PatternRelHeight), nPatternHeight);
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakIn50mmCuvette") == 0) {
        return ((int)(fAspectRatio * (this.PatternRelWidth * nPatternHeight) / this.PatternRelHeight), nPatternHeight);
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "CropRectangle") == 0) {
        return ((int)(this.normalizePoints[1].X - this.normalizePoints[0].X), (int)(this.normalizePoints[1].Y - this.normalizePoints[0].Y));
      }
      return (0, 0);
    }
    public static float calcDistance(Vector2 p1, Vector2 p2) {
      float dx = p2.X - p1.X;
      float dy = p2.Y - p1.Y;
      return (float)Math.Sqrt(dx * dx + dy * dy);
    }
    public struct Circle
    {
      public Vector2 Center;
      public float Radius;
    }
    public static Circle GetCircleFrom3Points(float x1, float y1, float x2, float y2, float x3, float y3) {
      // Sort points.
      if (Math.Abs(y2 - y3) <= Math.Abs(y1 - y2)) {
        float fTemp = y3;
        y3 = y1;
        y1 = fTemp;
        fTemp = x3;
        x3 = x1;
        x1 = fTemp;
      }
      float ms2 = (x3 - x2) / (y2 - y3);
      float f1;
      if (y1 == y2) {
        f1 = x1 + x2;
      } else {
        float ms1 = (x2 - x1) / (y1 - y2);
        float msD = ms1 - ms2;
        f1 = ((x1 + x2) * ms1 - (x2 + x3) * ms2 + (y3 - y1)) / msD;
      }
      float xCenter = f1 / 2;
      float yCenter = ((f1 - x2 - x3) * ms2 + (y2 + y3)) / 2;
      float radius = (float)Math.Sqrt((xCenter - x1) * (xCenter - x1) + (yCenter - y1) * (yCenter - y1));
      return new Circle {
        Center = new Vector2 { X = xCenter, Y = yCenter },
        Radius = radius,
      };
    }
  }
}
