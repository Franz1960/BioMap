
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Numerics;

namespace BioMap.ImageProc
{
  /// <summary>
  /// Defines extension methods to apply binary thresholding on an <see cref="Image"/>
  /// using Mutate/Clone.
  /// </summary>
  public static class SafeCropExtensions
  {
    /// <summary>
    /// Applies binarization to the image splitting the pixels at the given threshold with
    /// Luminance as color component to be compared to threshold.
    /// </summary>
    /// <param name="source">The image this method extends.</param>
    /// <param name="threshold">The threshold to apply binarization of the image. Must be between 0 and 1.</param>
    /// <param name="hueIntervals">Permissible hue value intervals as array of 2D vectors. X is lower, Y upper interval bounding. Range is circular 0..360.</param>
    /// <returns>The <see cref="IImageProcessingContext"/> to allow chaining of operations.</returns>
    public static IImageProcessingContext SafeCrop(this IImageProcessingContext source,int width,int height) {
      var sz = source.GetCurrentSize();
      if (width > sz.Width || height > sz.Height) {
        source.Resize(new ResizeOptions {
          Size = new Size(Math.Max(width, sz.Width), Math.Max(height, sz.Height)),
          TargetRectangle = new Rectangle(0, 0, sz.Width, sz.Height),
        });
      }
      return source.Crop(width,height);
    }

  }
}
