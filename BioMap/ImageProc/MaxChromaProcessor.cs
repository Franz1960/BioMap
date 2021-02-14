using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;
using System.Numerics;

namespace BioMap.ImageProc
{
  /// <summary>
  /// Performs simple binary threshold filtering against an image.
  /// </summary>
  public class MaxChromaProcessor : IImageProcessor
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxChromaProcessor"/> class.
    /// </summary>
    /// <param name="threshold">The threshold to split the image. Must be between 0 and 1.</param>
    /// <param name="hueIntervals">Permissible hue value intervals as array of 2D vectors. X is lower, Y upper interval bounding. Range is circular 0..360.</param>
    public MaxChromaProcessor(float threshold,Vector2[] hueIntervals = null)
        : this(threshold,Color.White,Color.Black,hueIntervals) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxChromaProcessor"/> class.
    /// </summary>
    /// <param name="threshold">The threshold to split the image. Must be between 0 and 1.</param>
    /// <param name="upperColor">The color to use for pixels that are above the threshold.</param>
    /// <param name="lowerColor">The color to use for pixels that are below the threshold.</param>
    /// <param name="hueIntervals">Permissible hue value intervals as array of 2D vectors. X is lower, Y upper interval bounding. Range is circular 0..360.</param>
    public MaxChromaProcessor(float threshold,Color upperColor,Color lowerColor,Vector2[] hueIntervals = null) {
      this.Threshold = threshold;
      this.UpperColor = upperColor;
      this.LowerColor = lowerColor;
      this.HueIntervals = hueIntervals;
    }

    /// <summary>
    /// Gets the threshold value.
    /// </summary>
    public float Threshold { get; }

    /// <summary>
    /// Gets the color to use for pixels that are above the threshold.
    /// </summary>
    public Color UpperColor { get; }

    /// <summary>
    /// Gets the color to use for pixels that fall below the threshold.
    /// </summary>
    public Color LowerColor { get; }

    /// Permissible hue value intervals as array of 2D vectors. X is lower, Y upper interval bounding. Range is circular 0..360.
    public Vector2[] HueIntervals { get; }

    /// <inheritdoc />
    public IImageProcessor<TPixel> CreatePixelSpecificProcessor<TPixel>(Configuration configuration,Image<TPixel> source,Rectangle sourceRectangle)
        where TPixel : unmanaged, IPixel<TPixel>
        => new MaxChromaProcessor<TPixel>(configuration,this,source,sourceRectangle);
  }
}
