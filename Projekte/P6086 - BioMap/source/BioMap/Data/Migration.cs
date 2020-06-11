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
    [JsonObject(MemberSerialization.Fields)]
    internal class UploadInfo_t
    {
      public string Timestamp;
      public string UserId;
    }
    [JsonObject(MemberSerialization.Fields)]
    internal class MarkerInfo_t
    {
      public int category;
      public LatLng_t position;
    }
    [JsonObject(MemberSerialization.Fields)]
    internal class ExifData_t
    {
      public string Make;
      public string Model;
      public string DateTimeOriginal;
    }
    [JsonObject(MemberSerialization.Fields)]
    internal class ElementProp_t
    {
      public MarkerInfo_t MarkerInfo;
      public UploadInfo_t UploadInfo;
      public ExifData_t ExifData;

    }
    internal class Element_t
    {
      public string ElementName;
      public ElementProp_t ElementProp;
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
      var sImagesDir = System.IO.Path.Combine(ds.DataDir, "images");
      var sImagesOrigDir = System.IO.Path.Combine(ds.DataDir, "images_orig");
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
      #region elements
      try
      {
        var sElementsDir = System.IO.Path.Combine(sMigSrcDir, "elements");
        var aFileNames = System.IO.Directory.GetFiles(sElementsDir, "*.json");
        ds.OperateOnDb((command) =>
        {
          foreach (var sFileName in aFileNames)
          {
            if (string.CompareOrdinal(sFileName, "all_elements.json") != 0)
            {
              try
              {
                var sr = new System.IO.StreamReader(sFileName);
                var sJson = sr.ReadToEnd();
                var el = JsonConvert.DeserializeObject<Element_t>(sJson);
                sr.Close();
                command.CommandText =
                  "INSERT INTO elements (name,category,markerposlat,markerposlng,uploadtime,uploader,creationtime) " +
                  "VALUES ('" + el.ElementName +
                  "','" + el.ElementProp.MarkerInfo.category +
                  "','" + el.ElementProp.MarkerInfo.position.lat +
                  "','" + el.ElementProp.MarkerInfo.position.lng +
                  "','" + el.ElementProp.UploadInfo.Timestamp +
                  "','" + el.ElementProp.UploadInfo.UserId +
                  "','" + ((el.ElementProp.ExifData != null) ? el.ElementProp.ExifData.DateTimeOriginal : el.ElementProp.UploadInfo.Timestamp) +
                  "')";
                command.ExecuteNonQuery();
                // Bilder verarbeiten.
                {
                  var sImageFile = System.IO.Path.Combine(sElementsDir, el.ElementName);
                  var sImageOrigFile = sImageFile + ".orig.jpg";
                  if (System.IO.File.Exists(sImageFile))
                  {
                    System.IO.File.Copy(sImageFile, System.IO.Path.Combine(sImagesDir, el.ElementName), true);
                    if (System.IO.File.Exists(sImageOrigFile))
                    {
                      System.IO.File.Copy(sImageOrigFile, System.IO.Path.Combine(sImagesOrigDir, el.ElementName), true);
                    }
                  } else if (System.IO.File.Exists(sImageOrigFile))
                  {
                    System.IO.File.Copy(sImageOrigFile, System.IO.Path.Combine(sImagesDir, el.ElementName), true);
                  }
                }
              }
              catch { }
            }
          }
        });

      }
      catch { }
      #endregion
    }
  }
}
