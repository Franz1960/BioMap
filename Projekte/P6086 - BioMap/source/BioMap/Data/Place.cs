using System.Collections.Generic;
using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class Place
  {
    public Place() {
      this.TraitValues=new List<int>(new int[Places.Traits.Length]);
    }
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
    public readonly List<int> TraitValues = new List<int>();
  }
}
