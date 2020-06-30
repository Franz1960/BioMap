using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class Place
  {
    public string Name;
    public double Radius = 150;
    public LatLng LatLng;
  }
}
