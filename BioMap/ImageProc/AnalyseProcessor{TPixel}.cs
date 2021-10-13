// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors;

namespace BioMap.ImageProc
{
  /// <summary>
  /// Performs simple binary threshold filtering against an image.
  /// </summary>
  /// <typeparam name="TPixel">The pixel format.</typeparam>
  internal class AnalyseProcessor<TPixel> : ImageProcessor<TPixel>
      where TPixel : unmanaged, IPixel<TPixel>
  {
    private readonly AnalyseProcessor definition;

    public Data_t data = new Data_t();

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryThresholdProcessor{TPixel}"/> class.
    /// </summary>
    /// <param name="configuration">The configuration which allows altering default behaviour or extending the library.</param>
    /// <param name="definition">The <see cref="BinaryThresholdProcessor"/> defining the processor parameters.</param>
    /// <param name="source">The source <see cref="Image{TPixel}"/> for the current processor instance.</param>
    /// <param name="sourceRectangle">The source area to process for the current processor instance.</param>
    public AnalyseProcessor(Configuration configuration, AnalyseProcessor definition, Image<TPixel> source, Rectangle sourceRectangle)
        : base(configuration, source, sourceRectangle) {
      this.definition = definition;
    }

    protected override void BeforeImageApply() {
      base.BeforeImageApply();
      this.data.BlackPixelCntPerRow = new double[this.SourceRectangle.Height];
    }

    protected override void AfterImageApply() {
      base.AfterImageApply();
      int yLength = this.data.BlackPixelCntPerRow.Length;
      int yCenter = yLength / 2;
      double fBlackTotal = 0;
      double mom1Sum = 0;
      double mom2Sum = 0;
      double fPixelArea = this.SourceRectangle.Width * this.SourceRectangle.Height;
      var lVerticalProfile = new List<double>();
      for (int y = 0; y < yLength; y++) {
        int yd = y - yCenter;
        double a = this.data.BlackPixelCntPerRow[y];
        lVerticalProfile.Add(this.data.BlackPixelCntPerRow[y] / fPixelArea);
        fBlackTotal += a;
        mom1Sum += yd * a;
        mom2Sum += yd * yd * a;
      }
      this.definition.AnalyseData.VerticalProfile = lVerticalProfile.ToArray();
      this.definition.AnalyseData.ShareOfBlack = fBlackTotal / fPixelArea;
      var mom1 = mom1Sum / fBlackTotal;
      var mom2 = mom2Sum / fBlackTotal;
      this.definition.AnalyseData.VerticalCenterOfMass = mom1 / yCenter;
      this.definition.AnalyseData.VerticalStdDeviation = Math.Sqrt(mom2 - mom1 * mom1) / yCenter;
    }

    /// <inheritdoc/>
    protected override void OnFrameApply(ImageFrame<TPixel> source) {
      Rectangle sourceRectangle = this.SourceRectangle;
      Configuration configuration = this.Configuration;

      var interest = Rectangle.Intersect(sourceRectangle, source.Bounds());

      var operation = new RowOperation(interest, source, this.data);
      ParallelRowIterator.IterateRows(
          configuration,
          interest,
          in operation);
    }

    public class Data_t
    {
      public double[] BlackPixelCntPerRow;
    }

    /// <summary>
    /// A <see langword="struct"/> implementing the clone logic for <see cref="BinaryThresholdProcessor{TPixel}"/>.
    /// </summary>
    private struct RowOperation : IRowOperation
    {
      private readonly ImageFrame<TPixel> source;
      private readonly int minX;
      private readonly int maxX;

      public readonly Data_t Data;

      public RowOperation(
          Rectangle bounds,
          ImageFrame<TPixel> source,
          Data_t data) {
        this.source = source;
        this.minX = bounds.X;
        this.maxX = bounds.Right;
        this.Data = data;
      }

      /// <inheritdoc/>
      public void Invoke(int y) {
        Rgba32 rgba = default;
        Span<TPixel> row = this.source.GetPixelRowSpan(y);
        ref TPixel rowRef = ref MemoryMarshal.GetReference(row);

        {
          float fBlackPixelCnt = 0;
          for (int x = this.minX; x < this.maxX; x++) {
            ref TPixel color = ref Unsafe.Add(ref rowRef, x);
            color.ToRgba32(ref rgba);

            if (rgba == (Rgba32)Color.Black) {
              fBlackPixelCnt += 1;
            }
          }
          this.Data.BlackPixelCntPerRow[y] += fBlackPixelCnt;
        }
      }
    }
  }
}
