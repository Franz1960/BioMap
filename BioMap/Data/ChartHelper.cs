using System;
using System.Collections.Generic;
using System.Linq;

namespace BioMap
{
  public class ChartHelper
  {
    public static string GetColor(int nIndex) {
      return _Colors[nIndex % _Colors.Length];
    }
    private static string[] _Colors = new string[] {
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Brown),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Blue),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Green),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Red),
      ChartJs.Blazor.Util.ColorUtil.FromDrawingColor(System.Drawing.Color.Yellow),
    };
  }
}
