using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap
{
  public class Migration
  {
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
      var sImagesDir = System.IO.Path.Combine(ds.DataDir, "images");
      var sImagesOrigDir = System.IO.Path.Combine(ds.DataDir, "images_orig");
      #region places.json
      try
      {
        var sr = new System.IO.StreamReader(System.IO.Path.Combine(sMigSrcDir, "places.json"));
        var sJson = sr.ReadToEnd();
        var aPlaces = JsonConvert.DeserializeObject<Place[]>(sJson);
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
        {
          foreach (var sFileName in aFileNames)
          {
            if (string.CompareOrdinal(sFileName, "all_elements.json") != 0)
            {
              try
              {
                var sr = new System.IO.StreamReader(sFileName);
                var sJson = sr.ReadToEnd();
                sr.Close();
                var jel = JObject.Parse(sJson);
                var exifData = new Element.ExifData_t {
                };
                if (jel["ElementProp"]["ExifData"]!=null)
                {
                  var sDateTimeOriginal = jel["ElementProp"]["ExifData"]["DateTimeOriginal"]?.Value<string>();
                  DateTime dtOriginal;
                  if (!DateTime.TryParse(sDateTimeOriginal,out dtOriginal))
                  {
                    if (sDateTimeOriginal!=null && sDateTimeOriginal.Length>=18 && sDateTimeOriginal[4]==':')
                    {
                      // Fehlerhaftes Datumsformat in Exif-Daten ('2020:06:21 16:27:00') korrigieren.
                      sDateTimeOriginal = sDateTimeOriginal.Substring(0, 4) + "-" + sDateTimeOriginal.Substring(5, 2) + "-" + sDateTimeOriginal.Substring(8, 2) + sDateTimeOriginal.Substring(10);
                    }
                  }
                  exifData = new Element.ExifData_t
                  {
                    Make = jel["ElementProp"]["ExifData"]["Make"]?.Value<string>(),
                    Model = jel["ElementProp"]["ExifData"]["Model"]?.Value<string>(),
                    DateTimeOriginal = DateTime.TryParse(sDateTimeOriginal, out DateTime dt)?dt:(DateTime?)null,
                  };
                }
                var indivData = new Element.IndivData_t {
                };
                var jIndivData = jel["ElementProp"]["IndivData"];
                if (jIndivData!=null && jIndivData.HasValues)
                {
                  var measuredData = new Element.IndivData_t.MeasuredData_t
                  {
                  };
                  if (
                  jIndivData["MeasuredData"] != null
                  &&
                  jIndivData["MeasuredData"]["HeadBodyLength"]!=null
                  )
                  {
                    measuredData = new Element.IndivData_t.MeasuredData_t
                    {
                      HeadPosition = new System.Numerics.Vector2
                      {
                        X = jIndivData["MeasuredData"]["HeadPosition"]["x"].Value<float>(),
                        Y = jIndivData["MeasuredData"]["HeadPosition"]["y"].Value<float>(),
                      },
                      BackPosition = new System.Numerics.Vector2
                      {
                        X = jIndivData["MeasuredData"]["BackPosition"]["x"].Value<float>(),
                        Y = jIndivData["MeasuredData"]["BackPosition"]["y"].Value<float>(),
                      },
                      HeadBodyLength = jIndivData["MeasuredData"]["HeadBodyLength"].Value<float>(),
                    };
                    if (
                      jIndivData["MeasuredData"]["OrigHeadPosition"]!=null
                      &&
                      jIndivData["MeasuredData"]["PtsOnCircle"]!=null
                    )
                    try {
                      measuredData.OrigHeadPosition = new System.Numerics.Vector2
                      {
                        X = jIndivData["MeasuredData"]["OrigHeadPosition"]["x"].Value<float>(),
                        Y = jIndivData["MeasuredData"]["OrigHeadPosition"]["y"].Value<float>(),
                      };
                      measuredData.OrigBackPosition = new System.Numerics.Vector2
                      {
                        X = jIndivData["MeasuredData"]["OrigBackPosition"]["x"].Value<float>(),
                        Y = jIndivData["MeasuredData"]["OrigBackPosition"]["y"].Value<float>(),
                      };
                      measuredData.PtsOnCircle = new System.Numerics.Vector2[]
                      {
                        new System.Numerics.Vector2
                        {
                          X = jIndivData["MeasuredData"]["PtsOnCircle"][0]["x"].Value<float>(),
                          Y = jIndivData["MeasuredData"]["PtsOnCircle"][0]["y"].Value<float>(),
                        },
                        new System.Numerics.Vector2
                        {
                          X = jIndivData["MeasuredData"]["PtsOnCircle"][1]["x"].Value<float>(),
                          Y = jIndivData["MeasuredData"]["PtsOnCircle"][1]["y"].Value<float>(),
                        },
                        new System.Numerics.Vector2
                        {
                          X = jIndivData["MeasuredData"]["PtsOnCircle"][2]["x"].Value<float>(),
                          Y = jIndivData["MeasuredData"]["PtsOnCircle"][2]["y"].Value<float>(),
                        },
                      };
                    } catch { }
                  }
                  indivData = new Element.IndivData_t
                  {
                      IId = ConvInvar.ToInt(jIndivData["IId"]?.Value<string>()),
                      Gender = jIndivData?["Gender"]?.Value<string>(),
                      MeasuredData = measuredData,
                  };
                  if (jIndivData["TraitValues"] != null)
                  {
                    var jTraitValues = jIndivData["TraitValues"];
                    foreach (var jTraitValue in jTraitValues)
                    {
                      if (jTraitValue is JProperty jProperty)
                      {
                        indivData.TraitValues.Add(jProperty.Name, jTraitValues[jProperty.Name].Value<int>());
                      }
                    }
                  }
                }
                var el = new Element
                {
                  ElementName = jel["ElementName"].Value<string>(),
                  ElementProp = new Element.ElementProp_t
                  {
                    MarkerInfo = new Element.MarkerInfo_t
                    {
                      category = jel["ElementProp"]["MarkerInfo"]["category"].Value<int>(),
                      position = new LatLng {
                        lat = jel["ElementProp"]["MarkerInfo"]["position"]["lat"].Value<double>(),
                        lng = jel["ElementProp"]["MarkerInfo"]["position"]["lng"].Value<double>(),
                      },
                    },
                    UploadInfo=new Element.UploadInfo_t
                    {
                      Timestamp = jel["ElementProp"]["UploadInfo"]["Timestamp"].Value<DateTime>(),
                      UserId = jel["ElementProp"]["UploadInfo"]["UserId"].Value<string>(),
                    },
                    ExifData = exifData,
                    IndivData = indivData,
                  },
                };
                el.ElementProp.CreationTime = ((el.ElementProp.ExifData != null && el.ElementProp.ExifData.DateTimeOriginal.HasValue) ? el.ElementProp.ExifData.DateTimeOriginal.Value : el.ElementProp.UploadInfo.Timestamp);
                if (indivData.Gender!=null && indivData.Gender.StartsWith("j"))
                {
                  int yob = 2016;
                  if (int.TryParse(indivData.Gender.Substring(1), out int age))
                  {
                    yob = el.ElementProp.CreationTime.Year - age;
                  }
                  indivData.YearOfBirth = yob;
                }
                else
                {
                  indivData.YearOfBirth = 2016;
                }
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
                ds.WriteElement(el);
              }
              catch { }
            }
          }
        }
      }
      catch { }
      #endregion
    }
  }
}
