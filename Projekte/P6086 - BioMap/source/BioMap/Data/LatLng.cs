using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class LatLng
  {
    public double lat;
    public double lng;
  }
}
