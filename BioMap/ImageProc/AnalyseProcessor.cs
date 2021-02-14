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
  public class AnalyseProcessor : IImageProcessor
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyseProcessor"/> class.
    /// </summary>
    public AnalyseProcessor() {
    }

    public class AnalyseData_t
    {
      /// <summary>
      /// The proportion of white pixels.
      /// </summary>
      public float ShareOfWhite { get; set; }
    }

    public AnalyseData_t AnalyseData = new AnalyseData_t();

    /// <inheritdoc />
    public IImageProcessor<TPixel> CreatePixelSpecificProcessor<TPixel>(Configuration configuration,Image<TPixel> source,Rectangle sourceRectangle)
        where TPixel : unmanaged, IPixel<TPixel>
        => new AnalyseProcessor<TPixel>(configuration,this,source,sourceRectangle);
  }
}
