using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class PatternImage
  {
    public PatternImage() {
    }
    public string MeasureDataJson;
    public string DataImageSrc;
  }
}
