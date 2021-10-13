using System;
using System.Transactions;
using Geo.Geodesy;
using Geo.Geometries;
using Geo.Measure;

namespace BioMap
{
  public static class GeoCalculator
  {
    private static readonly SpheroidCalculator calculator = new SpheroidCalculator(Spheroid.Wgs84);
    public static double GetDistance(double lat1, double lon1, double lat2, double lon2) {
      var p1 = new Point(lat1, lon1);
      var p2 = new Point(lat2, lon2);
      var line = calculator.CalculateOrthodromicLine(p1, p2);
      if (line == null) {
        return 0;
      } else {
        var d = line.Distance.ConvertTo(DistanceUnit.M).Value;
        return d;
      }
    }
    public static double GetDistance(LatLng latLng1, LatLng latLng2) {
      return GetDistance(latLng1.lat, latLng1.lng, latLng2.lat, latLng2.lng);
    }
  }
}
