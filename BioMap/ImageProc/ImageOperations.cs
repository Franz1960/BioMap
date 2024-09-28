using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Blazor.ImageSurveyor;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;

namespace BioMap.ImageProc
{
  public static class Extensions
  {
    public static Vector2 GetUnitVector(this Vector2 v) {
      float l = v.Length();
      if (l == 0f) {
        return Vector2.UnitX;
      } else {
        return v / l;
      }
    }
  }
  /// <summary>
  /// Image operations.
  /// </summary>
  public class ImageOperations
  {
    /// <summary>
    /// Crop an arbitrarily oriented rectangle out of an image and resize to a given size.
    /// The rectangle may be inside the source image or not. If not, the parts of the cropped image 
    /// which are outside the source image are fille with gray.
    /// </summary>
    /// <param name="imgSrc">The source image.</param>
    /// <param name="mTransform">The transformation which transforms the source rectangle to the resulting image.</param>
    /// <param name="szTargetImg">The target image size.</param>
    /// <returns>The target image or null if the operation is not possible (e.g. tranform matrix is not invertible).</returns>
    public static Image TransformAndCropOutOfImage(Image imgSrc, Matrix3x2 mTransform, Size szTargetImg) {
      if (Matrix3x2.Invert(mTransform, out Matrix3x2 mInvers)) {
        Point[] ptaSrcImgVertices = new[] {
          Point.Transform(new Point(0,0),mInvers),
          Point.Transform(new Point(0,szTargetImg.Width),mInvers),
          Point.Transform(new Point(szTargetImg.Height,szTargetImg.Width),mInvers),
          Point.Transform(new Point(szTargetImg.Height,0),mInvers),
        };
        var rectSrc = new Rectangle(
          ptaSrcImgVertices.Min(pt => pt.X),
          ptaSrcImgVertices.Min(pt => pt.Y),
          ptaSrcImgVertices.Max(pt => pt.X) - ptaSrcImgVertices.Min(pt => pt.X),
          ptaSrcImgVertices.Max(pt => pt.Y) - ptaSrcImgVertices.Min(pt => pt.Y));
        if (rectSrc.X >= 0 && rectSrc.Y >= 0 && rectSrc.Right <= imgSrc.Width && rectSrc.Top <= imgSrc.Height) {
          // Source rectangle is completely inside source image --> translate, crop, transform, crop.
          return imgSrc.Clone(x => x.Transform(rectSrc, mTransform, szTargetImg, KnownResamplers.Bicubic));
        } else {
          // Source rectangle is not completely inside source image --> transform and paint.
          var atb = new AffineTransformBuilder();
          atb.AppendMatrix(mTransform);
          imgSrc.Mutate(x => x.Transform(atb));
          var imgCropped = new Image<Rgb24>(szTargetImg.Width, szTargetImg.Height, Color.Gray);
          imgCropped.Mutate(x => x.DrawImage(imgSrc, 1f));
          return imgCropped;
        }
      }
      return null;
    }
    private static (Vector2, Vector2) GetLRPoints(Vector2 ptSpine, Vector2 vDir, float fWidth) {
      var vRot = Vector2.Transform(vDir, mRot90);
      Vector2 ptL = ptSpine - fWidth * vRot;
      Vector2 ptR = ptSpine + fWidth * vRot;
      return (ptL, ptR);
    }
    private static readonly Matrix3x2 mRot90 = Matrix3x2.CreateRotation(MathF.PI / 2);
    public static void DrawQuadToRect(Image imgSrc, Image imgDst, Vector2[] vaSrc, Vector2[] vaDst) {
      Vector2 vLM = (vaSrc[0] + vaSrc[3]) / 2;
      Vector2 vRM = (vaSrc[1] + vaSrc[2]) / 2;
      Vector2 vMM = (vLM + vRM) / 2;
      float phi = MathF.Atan2(vRM.Y - vLM.Y, vRM.X - vLM.X);
      var mSrcTranslate1 = Matrix3x2.CreateTranslation(-vMM);
      var mSrcRot = Matrix3x2.CreateRotation((-MathF.PI) - phi);
      Matrix3x2 mSrc = mSrcTranslate1 * mSrcRot;
      Vector2[] vaSrcTransformed = new[] {
          Vector2.Transform(vaSrc[0], mSrc),
          Vector2.Transform(vaSrc[1], mSrc),
          Vector2.Transform(vaSrc[2], mSrc),
          Vector2.Transform(vaSrc[3], mSrc),
        };
      var rectSrc = new Rectangle(
        (int)vaSrc.Min(pt => pt.X),
        (int)vaSrc.Min(pt => pt.Y),
        (int)(vaSrc.Max(pt => pt.X) - vaSrc.Min(pt => pt.X)),
        (int)(vaSrc.Max(pt => pt.Y) - vaSrc.Min(pt => pt.Y)));
      var rectSrcTransformed = new Rectangle(
        (int)vaSrcTransformed.Min(pt => pt.X),
        (int)vaSrcTransformed.Min(pt => pt.Y),
        (int)(vaSrcTransformed.Max(pt => pt.X) - vaSrcTransformed.Min(pt => pt.X)),
        (int)(vaSrcTransformed.Max(pt => pt.Y) - vaSrcTransformed.Min(pt => pt.Y)));
      var mSrcTranslate2 = Matrix3x2.CreateTranslation(-rectSrcTransformed.X, -rectSrcTransformed.Y);
      Vector2 vTargetDiag = (vaDst[1] - vaDst[0]);
      var szTargetRect = new Size((int)vTargetDiag.X, (int)vTargetDiag.Y);
      Matrix3x2 mTransform =
        mSrc *
        mSrcTranslate2 *
        Matrix3x2.CreateScale(vTargetDiag.X / rectSrcTransformed.Width, vTargetDiag.Y / rectSrcTransformed.Height);
      using (Image imgCropped = imgSrc.Clone(x => x.Transform(rectSrc, mTransform, szTargetRect, KnownResamplers.Bicubic))) {
        imgDst.Mutate(x => x.DrawImage(imgCropped, new Point((int)vaDst[0].X, (int)vaDst[0].Y), 1f));
      }
    }
    /// <summary>
    /// Crop an arbitrarily oriented curved spinal area out of an image and resize to a given size.
    /// The area may be inside the source image or not. If not, the parts of the cropped image 
    /// which are outside the source image are fille with gray.
    /// </summary>
    /// <param name="imgSrc">The source image.</param>
    /// <param name="szTargetImg">The target image size.</param>
    /// <returns>The target image or null if the operation is not possible (e.g. tranform matrix is not invertible).</returns>
    public static Image TransformCurvedSpineAndCropOutOfImage(Image imgSrc, Vector2[] spine, float fPatternRelWidth, float fPatternRelHeight, Size szTargetImg, Action<IEnumerable<Vector2[]>> setPolyLines = null) {
      float fSpineLength = RoundedCurve.GetPolyLineLength(spine);
      float fEmptyLength = (1 - fPatternRelHeight) / 2 * fSpineLength;
      float fPatternHalfWidth = fSpineLength * fPatternRelWidth / 2;
      var spineL = new List<Vector2>();
      var spineR = new List<Vector2>();
      var lRawBoxes = new List<Vector2[]>();
      for (int i = 0; i < spine.Length; i++) {
        Vector2 ptL, ptR;
        if (i == 0) {
          (ptL, ptR) = GetLRPoints(spine[i] + (fEmptyLength * (spine[i + 1] - spine[i]).GetUnitVector()), (spine[i + 1] - spine[i]).GetUnitVector(), fPatternHalfWidth);
        } else if (i == spine.Length - 1) {
          (ptL, ptR) = GetLRPoints(spine[i] - (fEmptyLength * (spine[i] - spine[i - 1]).GetUnitVector()), (spine[i] - spine[i - 1]).GetUnitVector(), fPatternHalfWidth);
        } else {
          (Vector2 ptL0, Vector2 ptR0) = GetLRPoints(spine[i], (spine[i] - spine[i - 1]).GetUnitVector(), fPatternHalfWidth);
          (Vector2 ptL1, Vector2 ptR1) = GetLRPoints(spine[i], (spine[i + 1] - spine[i]).GetUnitVector(), fPatternHalfWidth);
          ptL = (ptL0 + ptL1) / 2;
          ptR = (ptR0 + ptR1) / 2;
        }
        if (i >= 1) {
          lRawBoxes.Add(new[] { spineL.Last(), spineR.Last(), ptR, ptL, spineL.Last() });
        }
        spineL.Add(ptL);
        spineR.Add(ptR);
      }
      setPolyLines?.Invoke(lRawBoxes.ToArray());
      var spineTL = new List<Vector2>();
      var spineTR = new List<Vector2>();
      for (int i = 0; i < spine.Length; i++) {
        float yL = RoundedCurve.GetPolyLineLength(spineL.GetRange(0, i + 1).ToArray());
        float yR = RoundedCurve.GetPolyLineLength(spineR.GetRange(0, i + 1).ToArray());
        float yT = (yL + yR) / 2 * szTargetImg.Height / (fSpineLength * fPatternRelHeight);
        spineTL.Add(new Vector2(0, yT));
        spineTR.Add(new Vector2(szTargetImg.Width, yT));
      }
      var imgCropped = new Image<Rgb24>(szTargetImg.Width, szTargetImg.Height, Color.Gray);
#if true
      for (int iRect = 1; iRect < spine.Length; iRect++) {
        try {
          DrawQuadToRect(
            imgSrc,
            imgCropped,
            new[] { spineL[iRect - 1], spineR[iRect - 1], spineR[iRect], spineL[iRect] },
            new[] { spineTL[iRect - 1], new Vector2(spineTR[iRect].X, spineTR[iRect].Y + 1) });
        } catch { }
      }
#else
      // https://math.stackexchange.com/questions/169176/2d-transformation-matrix-to-make-a-trapezoid-out-of-a-rectangle
      for (int iRect = 1; iRect < spine.Length; iRect++) {
        try {
          float fScaleX = spineTR[iRect - 1].X - spineTL[iRect - 1].X;
          float fScaleY = spineTL[iRect].Y - spineTL[iRect - 1].Y;
          Vector2 v0 = spineL[iRect - 1];
          Vector2 vU = spineR[iRect - 1] - v0;
          Vector2 vV = spineL[iRect] - v0;
          Vector2 vW = spineR[iRect] - v0;
          var u = new Vector2(vU.X / fScaleX, vU.Y / fScaleY);
          var v = new Vector2(vV.X / fScaleX, vV.Y / fScaleY);
          var w = new Vector2(vW.X / fScaleX, vW.Y / fScaleY);
          var mInvers = new Matrix3x2(u.X, u.Y, v.X, v.Y, w.X, w.Y);
          if (Matrix3x2.Invert(mInvers, out Matrix3x2 mTransform)) {
            mTransform = Matrix3x2.CreateTranslation(-spineR[iRect - 1]) * mTransform;
            Vector2[] ptaSrcImgVertices = new[] {
              spineL[iRect - 1],
              spineL[iRect],
              spineR[iRect],
              spineR[iRect - 1],
            };
            var rectSrc = new Rectangle(
              (int)ptaSrcImgVertices.Min(pt => pt.X),
              (int)ptaSrcImgVertices.Min(pt => pt.Y),
              (int)(ptaSrcImgVertices.Max(pt => pt.X) - ptaSrcImgVertices.Min(pt => pt.X)),
              (int)(ptaSrcImgVertices.Max(pt => pt.Y) - ptaSrcImgVertices.Min(pt => pt.Y)));
            using (Image imgTransformed = imgSrc.Clone(x => x.Transform(rectSrc, mTransform, szTargetImg, KnownResamplers.Bicubic))) {
              imgCropped.Mutate(x => x.DrawImage(imgTransformed, new Point((int)spineTL[iRect - 1].X, (int)spineTL[iRect - 1].Y), 1f));
            }
          }
        } catch { }
      }
#endif
      return imgCropped;
    }
    // Extensions.
  }
}
