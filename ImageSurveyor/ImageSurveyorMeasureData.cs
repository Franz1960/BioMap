using System;
using System.Collections.Generic;
using System.Linq;
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
    public ImageSurveyorNormalizer normalizer;
    public System.Numerics.Vector2[] normalizePoints;
    public System.Numerics.Vector2[] measurePoints;
    public float PatternRelWidth { get => 0.30f; }
    public float PatternRelHeight { get => 0.80f; }
    //
    public System.Numerics.Matrix3x2 GetNormalizeMatrix() {
      var mResult = System.Numerics.Matrix3x2.Identity;
      if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakInPetriDish") == 0) {
        try {
          var md = this;
          mResult = System.Numerics.Matrix3x2.Identity;
          if (md.normalizePoints != null && md.normalizePoints.Length >= 3) {
            var circle = GetCircleFrom3Points(
              md.normalizePoints[0].X,
              md.normalizePoints[0].Y,
              md.normalizePoints[1].X,
              md.normalizePoints[1].Y,
              md.normalizePoints[2].X,
              md.normalizePoints[2].Y);
            float fScale = circle.Radius / (float)((md.normalizer.NormalizeReference / 2) / md.normalizer.NormalizePixelSize);
            var ptBoxCenter = new System.Numerics.Vector2 {
              X = (md.measurePoints[0].X + md.measurePoints[1].X) / 2,
              Y = (md.measurePoints[0].Y + md.measurePoints[1].Y) / 2,
            };
            float phi = MathF.Atan2(md.measurePoints[0].Y - md.measurePoints[1].Y, md.measurePoints[0].X - md.measurePoints[1].X);
            //
            mResult =
              System.Numerics.Matrix3x2.CreateTranslation(-ptBoxCenter.X, -ptBoxCenter.Y) *
              System.Numerics.Matrix3x2.CreateRotation(-MathF.PI / 2 - phi) *
              System.Numerics.Matrix3x2.CreateScale(1 / fScale) *
              System.Numerics.Matrix3x2.CreateTranslation(md.normalizer.NormalizedWidthPx / 2, md.normalizer.NormalizedHeightPx / 2);
          }
        } catch { }
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakIn50mmCuvette") == 0) {
        try {
          var md = this;
          float fNormWidthPixels = (
              new System.Numerics.Vector2(md.normalizePoints[1].X, md.normalizePoints[1].Y)
              -
              new System.Numerics.Vector2(md.normalizePoints[0].X, md.normalizePoints[0].Y)
              ).Length();
          float fScale = fNormWidthPixels / (float)((md.normalizer.NormalizeReference) / md.normalizer.NormalizePixelSize);
          var ptBoxCenter = new System.Numerics.Vector2 {
            X = (md.measurePoints[0].X + md.measurePoints[1].X) / 2,
            Y = (md.measurePoints[0].Y + md.measurePoints[1].Y) / 2,
          };
          float phi = MathF.Atan2(md.measurePoints[0].Y - md.measurePoints[1].Y, md.measurePoints[0].X - md.measurePoints[1].X);
          //
          mResult =
            System.Numerics.Matrix3x2.CreateTranslation(-ptBoxCenter.X, -ptBoxCenter.Y) *
            System.Numerics.Matrix3x2.CreateRotation(-MathF.PI / 2 - phi) *
            System.Numerics.Matrix3x2.CreateScale(1 / fScale) *
            System.Numerics.Matrix3x2.CreateTranslation(md.normalizer.NormalizedWidthPx / 2, md.normalizer.NormalizedHeightPx / 2);
        } catch { }
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "CropRectangle") == 0) {
        try {
          var md = this;
          //
          mResult =
            System.Numerics.Matrix3x2.CreateTranslation(-md.normalizePoints[0].X, -md.normalizePoints[0].Y);
        } catch { }
      }
      return mResult;
    }
    public System.Numerics.Matrix3x2 GetPatternMatrix(int nPatternHeight) {
      var md = this;
      var mResult = this.GetNormalizeMatrix();
      (int nWidth, int nHeight) = md.GetPatternSize(nPatternHeight);
      if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakInPetriDish") == 0) {
        try {
          var vCenter = (md.measurePoints[2] + md.measurePoints[3]) / 2;
          var fLength = (md.measurePoints[2] - md.measurePoints[3]).Length();
          float fScale = (fLength * this.PatternRelHeight) / (nPatternHeight);
          var ptBoxCenter = new System.Numerics.Vector2 {
            X = (md.measurePoints[2].X + md.measurePoints[3].X) / 2,
            Y = (md.measurePoints[2].Y + md.measurePoints[3].Y) / 2,
          };
          float phi = MathF.Atan2(md.measurePoints[2].Y - md.measurePoints[3].Y, md.measurePoints[2].X - md.measurePoints[3].X);
          //
          mResult =
            mResult *
            System.Numerics.Matrix3x2.CreateTranslation(-ptBoxCenter.X, -ptBoxCenter.Y) *
            System.Numerics.Matrix3x2.CreateRotation(-MathF.PI / 2 - phi) *
            System.Numerics.Matrix3x2.CreateScale(1 / fScale) *
            System.Numerics.Matrix3x2.CreateTranslation(nWidth / 2, nHeight / 2);
        } catch { }
      } else if (string.CompareOrdinal(this.normalizer.NormalizeMethod, "HeadToCloakIn50mmCuvette") == 0) {
        try {
          var vCenter = (md.measurePoints[2] + md.measurePoints[3]) / 2;
          var fLength = (md.measurePoints[2] - md.measurePoints[3]).Length();
          float fScale = (fLength * this.PatternRelHeight) / (nPatternHeight);
          var ptBoxCenter = new System.Numerics.Vector2 {
            X = (md.measurePoints[2].X + md.measurePoints[3].X) / 2,
            Y = (md.measurePoints[2].Y + md.measurePoints[3].Y) / 2,
          };
          float phi = MathF.Atan2(md.measurePoints[2].Y - md.measurePoints[3].Y, md.measurePoints[2].X - md.measurePoints[3].X);
          //
          mResult =
            mResult *
            System.Numerics.Matrix3x2.CreateTranslation(-ptBoxCenter.X, -ptBoxCenter.Y) *
            System.Numerics.Matrix3x2.CreateRotation(-MathF.PI / 2 - phi) *
            System.Numerics.Matrix3x2.CreateScale(1 / fScale) *
            System.Numerics.Matrix3x2.CreateTranslation(nWidth / 2, nHeight / 2);
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
    public static float calcDistance(System.Numerics.Vector2 p1, System.Numerics.Vector2 p2) {
      float dx = p2.X - p1.X;
      float dy = p2.Y - p1.Y;
      return (float)Math.Sqrt(dx * dx + dy * dy);
    }
    public struct Circle
    {
      public System.Numerics.Vector2 Center;
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
        Center = new System.Numerics.Vector2 { X = xCenter, Y = yCenter },
        Radius = radius,
      };
    }
  }
}
