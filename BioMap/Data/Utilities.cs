﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
  public static class Utilities
  {
    public static void FireEvent(EventHandler ev,object sender,object e) {
      if (ev!=null) {
        foreach (Delegate d in ev.GetInvocationList()) {
          d.DynamicInvoke(new object[] { sender,e });
        }
      }
    }
    public static DateTime DateTime_from_Years(double dYears) {
      int nYears = (int)Math.Floor(dYears);
      var dt = new DateTime(nYears,1,1);
      dt+=TimeSpan.FromDays((dYears-nYears)*365);
      return dt;
    }
    public static double Years_from_DateTime(DateTime dt) {
      double dYears = dt.Year+Math.Min(365,dt.DayOfYear)/365.001;
      return dYears;
    }
    private class DelayedCaller
    {
      internal DelayedCaller(int delayMs,Action<object[]> action,params object[] oaArgs) {
        this.DelayMs=delayMs;
        this.Action=action;
        this.Timer=new Timer(delayMs);
        this.Timer.Elapsed += this.Timer_Elapsed;
      }
      private void Timer_Elapsed(object sender,ElapsedEventArgs e) {
        this.Timer.Elapsed -= this.Timer_Elapsed;
        lock (DelayedCallers) {
          DelayedCallers.Remove(this.Action);
        }
        this.Action(this.Args);
      }
      internal Action<object[]> Action;
      internal object[] Args;
      internal Timer Timer;
      internal int DelayMs;
    }
    private static readonly Dictionary<Action<object[]>,DelayedCaller> DelayedCallers = new Dictionary<Action<object[]>, DelayedCaller>();
    /// <summary>
    /// Execute an action after a given delay. If this method is called again for the same action before the action 
    /// has been executed, the delay will start over again.
    /// Typical use case: Each change on a resource must lead to a refresh action, but if several changes occur in a 
    /// small span of time the refresh action shall be executed only once.
    /// </summary>
    /// <param name="delayMs">
    /// The delay in milliseconds.
    /// </param>
    /// <param name="action">
    /// The action.
    /// </param>
    public static void CallDelayed(int delayMs,Action<object[]> action,params object[] oaArgs) {
      lock (DelayedCallers) {
        DelayedCaller dc=null;
        if (!DelayedCallers.TryGetValue(action,out dc)) {
          dc=new DelayedCaller(delayMs,action,oaArgs);
          DelayedCallers[action] = dc;
        }
        dc.Timer.Stop();
        dc.Args=oaArgs;
        dc.Timer.Start();
      }
    }
    /// <summary>
    /// The differing core parts of two strings, excluding the identical parts from left and from right. The strings may have 
    /// different lengths. Example: A='ab123xyz', B='ab19yz' --> result=['23x','9'].
    /// </summary>
    /// <param name="A">
    /// The first string.
    /// </param>
    /// <param name="B">
    /// The second string.
    /// </param>
    /// <param name="nNeigboringChars">
    /// The number of characters left and right of the differing core parts to be included in the result.
    /// </param>
    /// <returns>
    /// Array of two strings: the differing part of the first and of the second string; null if the strings are equal.
    /// </returns>
    public static string[] FindDifferingCoreParts(string A,string B,int nNeigboringChars=10) {
      if (string.IsNullOrEmpty(A)) {
        if (string.IsNullOrEmpty(B)) {
          return null;
        } else {
          return new[] { "",B };
        }
      } else if (string.IsNullOrEmpty(B)) {
        return new[] { A,"" };
      } else if (string.CompareOrdinal(A,B)==0) {
        return null;
      }
      int lenA=A.Length;
      int lenB=B.Length;
      int lenMin=Math.Min(lenA,lenB);
      int idxL=0;
      while (idxL<lenMin) {
        if (A[idxL]!=B[idxL]) {
          break;
        }
        idxL++;
      }
      if (idxL>=lenA) {
        return new[] { "",B.Substring(Math.Max(0,idxL-nNeigboringChars)) };
      } else if (idxL>=lenB) {
        return new[] { A.Substring(Math.Max(0,idxL-nNeigboringChars)),"" };
      } else {
        int idxR=0;
        while (idxR<lenMin-idxL) {
          if (A[lenA-idxR-1]!=B[lenB-idxR-1]) {
            break;
          }
          idxR++;
        }
        idxL=Math.Max(0,idxL-nNeigboringChars);
        idxR=Math.Max(0,idxR-nNeigboringChars);
        return new[] { A.Substring(idxL,lenA-idxR-idxL),B.Substring(idxL,lenB-idxR-idxL) };
      }
    }
    public static string Bash(this string cmd) {
      var escapedArgs = cmd.Replace("\"","\\\"");
      var process = new Process() {
        StartInfo = new ProcessStartInfo {
          FileName = "/bin/bash",
          Arguments = $"-c \"{escapedArgs}\"",
          RedirectStandardOutput = true,
          UseShellExecute = false,
          CreateNoWindow = true,
        }
      };
      process.Start();
      string result = process.StandardOutput.ReadToEnd();
      process.WaitForExit();
      return result;
    }
    public static float GetScale(System.Numerics.Matrix3x2 m) {
      var v0t=System.Numerics.Vector2.Transform(System.Numerics.Vector2.Zero, m);
      var v1t=System.Numerics.Vector2.Transform(System.Numerics.Vector2.One, m);
      float fScale=(v1t-v0t).Length();
      return fScale;
    }
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
