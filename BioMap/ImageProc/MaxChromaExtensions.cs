
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap.ImageProc
{
  /// <summary>
  /// Defines extension methods to apply binary thresholding on an <see cref="Image"/>
  /// using Mutate/Clone.
  /// </summary>
  public static class MaxChromaExtensions
  {
    /// <summary>
    /// Applies binarization to the image splitting the pixels at the given threshold with
    /// Luminance as color component to be compared to threshold.
    /// </summary>
    /// <param name="source">The image this method extends.</param>
    /// <param name="threshold">The threshold to apply binarization of the image. Must be between 0 and 1.</param>
    /// <param name="hueIntervals">Permissible hue value intervals as array of 2D vectors. X is lower, Y upper interval bounding. Range is circular 0..360.</param>
    /// <returns>The <see cref="IImageProcessingContext"/> to allow chaining of operations.</returns>
    public static IImageProcessingContext MaxChroma(this IImageProcessingContext source, float threshold, Vector2[] hueIntervals = null) =>
        source.ApplyProcessor(new MaxChromaProcessor(threshold, hueIntervals));

  }
}
