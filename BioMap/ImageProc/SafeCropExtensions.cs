
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Numerics;

namespace BioMap.ImageProc
{
  /// <summary>
  /// Defines extension methods to apply safe cropping on an <see cref="Image"/>
  /// using Mutate/Clone. The base Crop operation throws an exception if a cropped dimension is bigger 
  /// than the image dimension.
  /// </summary>
  public static class SafeCropExtensions
  {
    /// <summary>
    /// Crops an image to the given width and height. If either is bigger than the image dimension, the space is filled with black.
    /// </summary>
    /// <param name="source">The image this method extends.</param>
    /// <param name="width">The width of the cropped image.</param>
    /// <param name="height">The height of the cropped image.</param>
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
