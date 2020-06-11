using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace BioMap
{
  public class Migration
  {
    [JsonObject(MemberSerialization.Fields)]
    internal class LatLng_t
    {
      public double lat;
      public double lng;
    }
    internal class Place_t
    {
      public string Name;
      public double Radius = 150;
      public LatLng_t LatLng;
    }
    [JsonObject(MemberSerialization.Fields)]
    internal class ProtocolContent_t
    {
      public string Timestamp;
      public string Author;
      public string Text;
    }
    internal class Protocol_t
    {
      public string Name;
      public ProtocolContent_t Content;
    }
    static string ToString(double d)
    {
      return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    public static void MigrateData()
    {
      var ds = DataService.Instance;
      ds.AddLogEntry("System", "Migrating data");
      var sMigSrcDir = System.IO.Path.Combine(ds.DataDir, "migration_source");
      #region places.json
      try
      {
        var sr = new System.IO.StreamReader(System.IO.Path.Combine(sMigSrcDir, "places.json"));
        var sJson = sr.ReadToEnd();
        var aPlaces = JsonConvert.DeserializeObject<Place_t[]>(sJson);
        sr.Close();
        ds.OperateOnDb((command) =>
        {
          foreach (var place in aPlaces)
          {
            command.CommandText = "INSERT INTO places (name,radius,lat,lng) VALUES ('"+place.Name+ "'," + ToString(place.Radius) + "," + ToString(place.LatLng.lat) + "," + ToString(place.LatLng.lng) + ")";
            command.ExecuteNonQuery();
          }
        });

      }
      catch { }
      #endregion
      #region protocol
      try
      {
        var aFileNames = System.IO.Directory.GetFiles(System.IO.Path.Combine(sMigSrcDir, "protocol"),"protocol_*.json");
        ds.OperateOnDb((command) =>
        {
          foreach (var sFileName in aFileNames)
          {
            var sr = new System.IO.StreamReader(sFileName);
            var sJson = sr.ReadToEnd();
            var pe = JsonConvert.DeserializeObject<Protocol_t>(sJson);
            sr.Close();
            command.CommandText = "INSERT INTO protocol (dt,author,text) VALUES ('" + pe.Content.Timestamp + "','" + pe.Content.Author + "','" + pe.Content.Text + "')";
            command.ExecuteNonQuery();
          }
        });

      }
      catch { }
      #endregion
    }
  }
}
