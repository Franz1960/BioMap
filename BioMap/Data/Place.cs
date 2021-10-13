using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class Place : ICloneable
  {
    public Place() {
      this.TraitValues = new List<int>(new int[Places.Traits.Length]);
    }
    public string Name;
    public double Radius = 150;
    public LatLng LatLng;
    public int MonitoringIntervalWeeks = 4;
    //
    public static Place GetNearestPlace(SessionData sd, LatLng latLng, float fDistanceTolerance = 0.20f) {
      Place nearestPlace = null;
      double minDistance = double.MaxValue;
      foreach (var p in DataService.Instance.GetPlaces(sd)) {
        var d = GeoCalculator.GetDistance(p.LatLng, latLng);
        if (nearestPlace == null || d < minDistance) {
          if (d <= p.Radius * (1 + fDistanceTolerance)) {
            nearestPlace = p;
            minDistance = d;
          }
        }
      }
      return nearestPlace;
    }

    public object Clone() {
      return this.MemberwiseClone();
    }

    public readonly List<int> TraitValues = new List<int>();
  }
}
