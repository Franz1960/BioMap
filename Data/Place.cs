using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class Place
  {
    public string Name;
    public double Radius = 150;
    public LatLng LatLng;
    //
    public static Place GetNearestPlace(LatLng latLng) {
      Place nearestPlace = null;
      double minDistance = double.MaxValue;
      foreach (var p in DataService.Instance.AllPlaces) {
        var d = GeoCalculator.GetDistance(p.LatLng,latLng);
        if (nearestPlace==null || d<minDistance) {
          nearestPlace=p;
          minDistance=d;
        }
      }
      return nearestPlace;
    }
  }
}
