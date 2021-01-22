using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blazor.ImageSurveyor
{
  /// <summary>
  /// MapOptions object used to define the properties that can be set on a Map.
  /// </summary>
  [JsonObject(MemberSerialization.Fields)]
  public class ImageSurveyorMeasureData
  {
    [JsonObject(MemberSerialization.Fields)]
    public class Point
    {
      public float X;
      public float Y;
    }
    public string method { get; set; }

    public object[] normalizePoints { get; set; }
    public object[] measurePoints { get; set; }

  }
}
