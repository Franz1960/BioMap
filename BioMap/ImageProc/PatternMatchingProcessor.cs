// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using SixLabors;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;

namespace BioMap.ImageProc
{
  /// <summary>
  /// Combines two images together by blending the pixels.
  /// </summary>
  public class PatternMatchingProcessor : IImageProcessor
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="PatternMatchingProcessor"/> class.
    /// </summary>
    /// <param name="image">The image to blend.</param>
    /// <param name="location">The location to draw the blended image.</param>
    /// <param name="colorBlendingMode">The blending mode to use when drawing the image.</param>
    /// <param name="alphaCompositionMode">The Alpha blending mode to use when drawing the image.</param>
    /// <param name="opacity">The opacity of the image to blend.</param>
    public PatternMatchingProcessor(
        Image image,
        Point location,
        float opacity) {
      this.Image = image;
      this.Location = location;
      this.Opacity = opacity;
    }

    /// <summary>
    /// Gets the image to blend.
    /// </summary>
    public Image Image { get; }

    /// <summary>
    /// Gets the location to draw the blended image.
    /// </summary>
    public Point Location { get; }

    /// <summary>
    /// Gets the opacity of the image to blend.
    /// </summary>
    public float Opacity { get; }

    /// <inheritdoc />
    public IImageProcessor<TPixelBg> CreatePixelSpecificProcessor<TPixelBg>(Configuration configuration, Image<TPixelBg> source, Rectangle sourceRectangle)
        where TPixelBg : unmanaged, IPixel<TPixelBg> {
      var visitor = new ProcessorFactoryVisitor<TPixelBg>(configuration, this, source, sourceRectangle);
      this.Image.AcceptVisitor(visitor);
      return visitor.Result;
    }

    private class ProcessorFactoryVisitor<TPixelBg> : IImageVisitor
        where TPixelBg : unmanaged, IPixel<TPixelBg>
    {
      private readonly Configuration configuration;
      private readonly PatternMatchingProcessor definition;
      private readonly Image<TPixelBg> source;
      private readonly Rectangle sourceRectangle;

      public ProcessorFactoryVisitor(Configuration configuration, PatternMatchingProcessor definition, Image<TPixelBg> source, Rectangle sourceRectangle) {
        this.configuration = configuration;
        this.definition = definition;
        this.source = source;
        this.sourceRectangle = sourceRectangle;
      }

      public IImageProcessor<TPixelBg> Result { get; private set; }

      public void Visit<TPixelFg>(Image<TPixelFg> image)
          where TPixelFg : unmanaged, IPixel<TPixelFg> {
        this.Result = new PatternMatchingProcessor<TPixelBg, TPixelFg>(
            this.configuration,
            image,
            this.source,
            this.sourceRectangle,
            this.definition.Location,
            this.definition.Opacity);
      }
    }
  }
}
