using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using System.Linq;
using System.Numerics;

namespace BioMap.ImageProc
{
  /// <summary>
  /// Image operations.
  /// </summary>
  public class ImageOperations
  {
    /// <summary>
    /// Crop a arbitrarily oriented rectangle out of an image and resize to a given size.
    /// The rectangle may be inside the source image or not. If not, the parts of the cropped image 
    /// which are outside the source image are fille with gray.
    /// </summary>
    /// <param name="imgSrc">The source image.</param>
    /// <param name="mTransform">The transformation which transforms the source rectangle to the resulting image.</param>
    /// <param name="szTargetImg">The target image size.</param>
    /// <returns>The target image or null if the operation is not possible (e.g. tranform matrix is not invertible).</returns>
    public static Image TransformAndCropOutOfImage(Image imgSrc,System.Numerics.Matrix3x2 mTransform,Size szTargetImg) {
      if (System.Numerics.Matrix3x2.Invert(mTransform, out var mInvers)) {
        var ptaSrcImgVertices = new[] {
          Point.Transform(new Point(0,0),mInvers),
          Point.Transform(new Point(0,szTargetImg.Width),mInvers),
          Point.Transform(new Point(szTargetImg.Height,szTargetImg.Width),mInvers),
          Point.Transform(new Point(szTargetImg.Height,0),mInvers),
        };
        var rectSrc = new Rectangle(
          ptaSrcImgVertices.Min(pt => pt.X),
          ptaSrcImgVertices.Min(pt => pt.Y),
          ptaSrcImgVertices.Max(pt => pt.X),
          ptaSrcImgVertices.Max(pt => pt.Y));
        if (rectSrc.X>=0 && rectSrc.Y>=0 && rectSrc.Right<=imgSrc.Width && rectSrc.Top<=imgSrc.Height) {
          // Source rectangle is completely inside source image --> translate, crop, transform, crop.
          var mTranslate = System.Numerics.Matrix3x2.CreateTranslation(-rectSrc.X,-rectSrc.Y);
          var mTranslateInv = System.Numerics.Matrix3x2.CreateTranslation(rectSrc.X,rectSrc.Y);
          return imgSrc.Clone(x => x.Transform(rectSrc,mTransform,szTargetImg,KnownResamplers.Bicubic));
        } else {
          // Source rectangle is not completely inside source image --> transform and paint.
          var atb = new AffineTransformBuilder();
          atb.AppendMatrix(mTransform);
          imgSrc.Mutate(x => x.Transform(atb));
          var imgCropped = new Image<Rgb24>(szTargetImg.Width,szTargetImg.Height,Color.Gray);
          imgCropped.Mutate(x => x.DrawImage(imgSrc,1f));
          return imgCropped;
        }
      }
      return null;
    }
  }
}
