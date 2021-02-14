// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
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
  /// <typeparam name="TPixel">The pixel format.</typeparam>
  internal class MaxChromaProcessor<TPixel> : ImageProcessor<TPixel>
      where TPixel : unmanaged, IPixel<TPixel>
  {
    private readonly MaxChromaProcessor definition;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryThresholdProcessor{TPixel}"/> class.
    /// </summary>
    /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
    /// <param name="definition">The <see cref="BinaryThresholdProcessor"/> defining the processor parameters.</param>
    /// <param name="source">The source <see cref="Image{TPixel}"/> for the current processor instance.</param>
    /// <param name="sourceRectangle">The source area to process for the current processor instance.</param>
    public MaxChromaProcessor(Configuration configuration,MaxChromaProcessor definition,Image<TPixel> source,Rectangle sourceRectangle)
        : base(configuration,source,sourceRectangle) {
      this.definition = definition;
    }

    /// <inheritdoc/>
    protected override void OnFrameApply(ImageFrame<TPixel> source) {
      byte threshold = (byte)MathF.Round(this.definition.Threshold * 255F);
      TPixel upper = this.definition.UpperColor.ToPixel<TPixel>();
      TPixel lower = this.definition.LowerColor.ToPixel<TPixel>();

      Rectangle sourceRectangle = this.SourceRectangle;
      Configuration configuration = this.Configuration;

      var interest = Rectangle.Intersect(sourceRectangle,source.Bounds());
      bool isAlphaOnly = typeof(TPixel) == typeof(A8);

      var operation = new RowOperation(interest,source,upper,lower,threshold,this.definition.HueIntervals);
      ParallelRowIterator.IterateRows(
          configuration,
          interest,
          in operation);
    }

    /// <summary>
    /// A <see langword="struct"/> implementing the clone logic for <see cref="BinaryThresholdProcessor{TPixel}"/>.
    /// </summary>
    private readonly struct RowOperation : IRowOperation
    {
      private readonly ImageFrame<TPixel> source;
      private readonly TPixel upper;
      private readonly TPixel lower;
      private readonly byte threshold;
      private readonly int minX;
      private readonly int maxX;
      private readonly Vector2[] hueIntervals;
      private readonly ColorSpaceConverter colorSpaceConverter;

      public RowOperation(
          Rectangle bounds,
          ImageFrame<TPixel> source,
          TPixel upper,
          TPixel lower,
          byte threshold,
          Vector2[] hueIntervals) {
        this.source = source;
        this.upper = upper;
        this.lower = lower;
        this.threshold = threshold;
        this.minX = bounds.X;
        this.maxX = bounds.Right;
        this.hueIntervals = hueIntervals;
        this.colorSpaceConverter = new ColorSpaceConverter();
      }

      /// <inheritdoc/>
      public void Invoke(int y) {
        Rgba32 rgba = default;
        Span<TPixel> row = this.source.GetPixelRowSpan(y);
        ref TPixel rowRef = ref MemoryMarshal.GetReference(row);

        if (this.hueIntervals==null) {
          float fThreshold = this.threshold / 2F;
          for (int x = this.minX;x < this.maxX;x++) {
            ref TPixel color = ref Unsafe.Add(ref rowRef,x);
            color.ToRgba32(ref rgba);

            // Calculate YCbCr value and compare to threshold.
            var yCbCr = this.colorSpaceConverter.ToYCbCr(rgba);
            if (MathF.Max(MathF.Abs(yCbCr.Cb - YCbCr.Achromatic.Cb),MathF.Abs(yCbCr.Cr - YCbCr.Achromatic.Cr)) >= fThreshold) {
              color = this.upper;
            } else {
              color = this.lower;
            }
          }
        } else {
          float fThreshold = this.threshold / 2F;
          for (int x = this.minX;x < this.maxX;x++) {
            ref TPixel color = ref Unsafe.Add(ref rowRef,x);
            color.ToRgba32(ref rgba);

            // Calculate HSL hue value and compare to intervals.
            bool bHueOk = false;
            var hue = this.colorSpaceConverter.ToHsl(rgba).H;
            foreach (var iv in this.hueIntervals) {
              if (iv.Y < iv.X) {
                if (hue >= iv.X || hue <= iv.Y) {
                  bHueOk=true;
                  break;
                }
              } else {
                if (hue >= iv.X && hue <= iv.Y) {
                  bHueOk = true;
                  break;
                }
              }
            }
            if (bHueOk) {
              // Calculate YCbCr value and compare to threshold.
              var yCbCr = this.colorSpaceConverter.ToYCbCr(rgba);
              if (MathF.Max(MathF.Abs(yCbCr.Cb - YCbCr.Achromatic.Cb),MathF.Abs(yCbCr.Cr - YCbCr.Achromatic.Cr)) >= fThreshold) {
                color = this.upper;
              } else {
                color = this.lower;
              }
            } else {
              color = this.lower;
            }
          }
        }
      }
    }
  }
}
