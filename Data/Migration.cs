using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BioMap.Pages.Lists;
using System.Security.Principal;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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
    static string ToString(double d) {
      return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    public static void MigrateData() {
      var ds = DataService.Instance;
      ds.AddLogEntry("System","Migrating data");
      var sMigSrcDir = System.IO.Path.Combine(ds.DataDir,"migration_source");
      var sConfDir = System.IO.Path.Combine(ds.DataDir,"conf");
      var sImagesDir = System.IO.Path.Combine(ds.DataDir,"images");
      var sImagesOrigDir = System.IO.Path.Combine(ds.DataDir,"images_orig");
      #region conf
      try {
        var sSrcDir = System.IO.Path.Combine(sMigSrcDir,"conf");
        System.IO.Directory.CreateDirectory(sConfDir);
        foreach (var sSrcPath in System.IO.Directory.GetFiles(sSrcDir)) {
          System.IO.File.Copy(sSrcPath,System.IO.Path.Combine(sConfDir,System.IO.Path.GetFileName(sSrcPath)));
        }
      } catch { }
      #endregion
      #region Artenliste.
      try {
        ds.OperateOnDb((command) => {
          command.CommandText = "INSERT INTO species (genus,species,commonname_en)" +
          " VALUES" +
          " ('bombina','bombina','Fire-bellied toad')" +
          ",('bombina','variegata','Yellow-bellied toad')";
          command.ExecuteNonQuery();
        });

      } catch { }
      #endregion
      var nSpeciesId = ds.GetSpeciesId("bombina","variegata");
      #region Projekte.
      try {
        ds.OperateOnDb((command) => {
          command.CommandText = "INSERT INTO projects (name,description,target_species_id)" +
            " VALUES" +
            " ('bombina-variegata-de-2019-donaustauf','Gelbbauchunken im Donaustaufer und Kreuther Forst ab 2019','"+nSpeciesId.Value+"')";
          command.ExecuteNonQuery();
        });

      } catch { }
      #endregion
      #region places.json
      try {
        var sr = new System.IO.StreamReader(System.IO.Path.Combine(sMigSrcDir,"places.json"));
        var sJson = sr.ReadToEnd();
        var aPlaces = JsonConvert.DeserializeObject<Place[]>(sJson);
        sr.Close();
        ds.OperateOnDb((command) => {
          foreach (var place in aPlaces) {
            if (place.Radius==0) {
              place.Radius=150;
            }
            command.CommandText = "INSERT INTO places (name,radius,lat,lng) VALUES ('"+place.Name+ "'," + ToString(place.Radius) + "," + ToString(place.LatLng.lat) + "," + ToString(place.LatLng.lng) + ")";
            command.ExecuteNonQuery();
          }
        });

      } catch { }
      #endregion
      #region protocol
      try {
        var aFileNames = System.IO.Directory.GetFiles(System.IO.Path.Combine(sMigSrcDir,"protocol"),"protocol_*.json");
        ds.OperateOnDb((command) => {
          foreach (var sFileName in aFileNames) {
            var sr = new System.IO.StreamReader(sFileName);
            var sJson = sr.ReadToEnd();
            var pe = JsonConvert.DeserializeObject<Protocol_t>(sJson);
            sr.Close();
            command.CommandText = "INSERT INTO protocol (dt,author,text) VALUES ('" + pe.Content.Timestamp + "','" + pe.Content.Author + "','" + pe.Content.Text + "')";
            command.ExecuteNonQuery();
          }
        });

      } catch { }
      #endregion
      #region log
      try {
        var lFileNames = System.IO.Directory.GetFiles(System.IO.Path.Combine(sMigSrcDir,"logs"),"log_*.txt")?.ToList();
        lFileNames.Sort();
        int nTotalLineCount = 0;
        ds.AddLogEntry("Migration","Processing "+lFileNames.Count+" log files beginning with "+lFileNames?[0]);
        ds.OperateOnDb((command) => {
          var regEx = new System.Text.RegularExpressions.Regex("(.*) User\\((.*)\\): (.*)");
          foreach (var sFileName in lFileNames) {
            var sr = new System.IO.StreamReader(sFileName);
            while (true) {
              try {
                var sLine = sr.ReadLine();
                if (string.IsNullOrEmpty(sLine)) {
                  break;
                } else {
                  nTotalLineCount++;
                  var saLogEntry = regEx.Split(sLine);
                  var le = new LogEntry() {
                    CreationTime=DateTime.Parse(saLogEntry[1]),
                    User=saLogEntry[2],
                    Action=saLogEntry[3],
                  };
                  command.CommandText = "INSERT INTO log (dt,user,action) VALUES (datetime('" + saLogEntry[1] + "'),'" + le.User + "','" + le.Action + "')";
                  command.ExecuteNonQuery();
                }
              } catch (Exception ex1) {
                command.CommandText = "INSERT INTO log (dt,user,action) VALUES (datetime('now','localtime'),'" + "Migration" + "','" + "Exception: "+ex1.ToString() + "')";
                command.ExecuteNonQuery();
              }
            }
            sr.Close();
          }
        });
        ds.AddLogEntry("Migration","Migrated "+nTotalLineCount +" log lines");
      } catch (Exception ex) {
        ds.AddLogEntry("Migration","Exception migrating logs: "+ex.ToString());
      }
      #endregion
      #region elements
      try {
        var sElementsDir = System.IO.Path.Combine(sMigSrcDir,"elements");
        var aFileNames = System.IO.Directory.GetFiles(sElementsDir,"*.json");
        var nProjectId = ds.GetProjectId("bombina-variegata-de-2019-donaustauf");
        {
          System.IO.Directory.CreateDirectory(sImagesDir);
          System.IO.Directory.CreateDirectory(sImagesOrigDir);
          foreach (var sFileName in aFileNames) {
            if (string.CompareOrdinal(sFileName,"all_elements.json") != 0) {
              try {
                var sr = new System.IO.StreamReader(sFileName);
                var sJson = sr.ReadToEnd();
                sr.Close();
                var jel = JObject.Parse(sJson);
                var exifData = new Element.ExifData_t {
                };
                if (jel["ElementProp"]["ExifData"]!=null) {
                  var sDateTimeOriginal = jel["ElementProp"]["ExifData"]["DateTimeOriginal"]?.Value<string>();
                  DateTime dtOriginal;
                  if (!DateTime.TryParse(sDateTimeOriginal,out dtOriginal)) {
                    if (sDateTimeOriginal!=null && sDateTimeOriginal.Length>=18 && sDateTimeOriginal[4]==':') {
                      // Fehlerhaftes Datumsformat in Exif-Daten ('2020:06:21 16:27:00') korrigieren.
                      sDateTimeOriginal = sDateTimeOriginal.Substring(0,4) + "-" + sDateTimeOriginal.Substring(5,2) + "-" + sDateTimeOriginal.Substring(8,2) + sDateTimeOriginal.Substring(10);
                    }
                  }
                  exifData = new Element.ExifData_t {
                    Make = jel["ElementProp"]["ExifData"]["Make"]?.Value<string>(),
                    Model = jel["ElementProp"]["ExifData"]["Model"]?.Value<string>(),
                    DateTimeOriginal = DateTime.TryParse(sDateTimeOriginal,out DateTime dt) ? dt : (DateTime?)null,
                  };
                }
                var indivData = new Element.IndivData_t {
                };
                var jIndivData = jel["ElementProp"]["IndivData"];
                if (jIndivData!=null && jIndivData.HasValues) {
                  var measuredData = new Element.IndivData_t.MeasuredData_t {
                  };
                  if (
                  jIndivData["MeasuredData"] != null
                  &&
                  jIndivData["MeasuredData"]["HeadBodyLength"]!=null
                  ) {
                    measuredData = new Element.IndivData_t.MeasuredData_t {
                      HeadPosition = new System.Numerics.Vector2 {
                        X = jIndivData["MeasuredData"]["HeadPosition"]["x"].Value<float>(),
                        Y = jIndivData["MeasuredData"]["HeadPosition"]["y"].Value<float>(),
                      },
                      BackPosition = new System.Numerics.Vector2 {
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
                        measuredData.OrigHeadPosition = new System.Numerics.Vector2 {
                          X = jIndivData["MeasuredData"]["OrigHeadPosition"]["x"].Value<float>(),
                          Y = jIndivData["MeasuredData"]["OrigHeadPosition"]["y"].Value<float>(),
                        };
                        measuredData.OrigBackPosition = new System.Numerics.Vector2 {
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
                  indivData = new Element.IndivData_t {
                    IId = ConvInvar.ToInt(jIndivData["IId"]?.Value<string>()),
                    Gender = jIndivData?["Gender"]?.Value<string>()?.ToLowerInvariant(),
                    MeasuredData = measuredData,
                  };
                  if (jIndivData["TraitValues"] != null) {
                    var jTraitValues = jIndivData["TraitValues"];
                    foreach (var jTraitValue in jTraitValues) {
                      if (jTraitValue is JProperty jProperty) {
                        indivData.TraitValues.Add(jProperty.Name,jTraitValues[jProperty.Name].Value<int>());
                      }
                    }
                  }
                }
                var el = new Element {
                  ElementName = jel["ElementName"].Value<string>(),
                  SpeciesId = nSpeciesId,
                  ProjectId = nProjectId,
                  ElementProp = new Element.ElementProp_t {
                    MarkerInfo = new Element.MarkerInfo_t {
                      category = jel["ElementProp"]["MarkerInfo"]["category"].Value<int>(),
                      position = new LatLng {
                        lat = jel["ElementProp"]["MarkerInfo"]["position"]["lat"].Value<double>(),
                        lng = jel["ElementProp"]["MarkerInfo"]["position"]["lng"].Value<double>(),
                      },
                    },
                    UploadInfo=new Element.UploadInfo_t {
                      Timestamp = jel["ElementProp"]["UploadInfo"]["Timestamp"].Value<DateTime>(),
                      UserId = jel["ElementProp"]["UploadInfo"]["UserId"].Value<string>(),
                    },
                    ExifData = exifData,
                    IndivData = indivData,
                  },
                };
                el.ElementProp.CreationTime = ((el.ElementProp.ExifData != null && el.ElementProp.ExifData.DateTimeOriginal.HasValue) ? el.ElementProp.ExifData.DateTimeOriginal.Value : el.ElementProp.UploadInfo.Timestamp);
                if (indivData.Gender!=null && indivData.Gender.StartsWith("j")) {
                  int yob = 2016;
                  if (int.TryParse(indivData.Gender.Substring(1),out int age)) {
                    yob = el.ElementProp.CreationTime.Year - age;
                  }
                  indivData.DateOfBirth = new DateTime(yob,7,1);
                } else {
                  indivData.DateOfBirth = new DateTime(2016,7,1);
                }
                // Bilder verarbeiten.
                {
                  var sImageFile = System.IO.Path.Combine(sElementsDir,el.ElementName);
                  var sImageOrigFile = sImageFile + ".orig.jpg";
                  if (System.IO.File.Exists(sImageFile)) {
                    if (System.IO.File.Exists(sImageOrigFile)) {
                      CopyImageCompressed(sImageOrigFile,System.IO.Path.Combine(sImagesOrigDir,el.ElementName),2000);
                      CopyImageCompressed(sImageFile,System.IO.Path.Combine(sImagesDir,el.ElementName));
                    } else {
                      CopyImageCompressed(sImageFile,System.IO.Path.Combine(sImagesOrigDir,el.ElementName),2000);
                      CopyImageCompressed(sImageFile,System.IO.Path.Combine(sImagesDir,el.ElementName));
                    }
                  } else if (System.IO.File.Exists(sImageOrigFile)) {
                    CopyImageCompressed(sImageOrigFile,System.IO.Path.Combine(sImagesOrigDir,el.ElementName),2000);
                    CopyImageCompressed(sImageOrigFile,System.IO.Path.Combine(sImagesDir,el.ElementName));
                  }
                }
                ds.WriteElement(el);
              } catch { }
            }
          }
          #region Jahrgang aller Wiederfänge auf den zuerst ermittelten Jahrgang setzen.
          {
            Dictionary<int,List<Element>> Individuals = DataService.Instance.GetIndividuals();
            foreach (var iid in Individuals.Keys) {
              int? nYoB = null;
              foreach (var el in Individuals[iid]) {
                int? nElYoB = el.GetYearOfBirth();
                if (!nYoB.HasValue) {
                  nYoB=el.GetYearOfBirth();
                } else if (!nElYoB.HasValue || nElYoB.Value!=nYoB.Value) {
                  el.ElementProp.IndivData.DateOfBirth=new DateTime(nYoB.Value,7,1);
                  DataService.Instance.WriteElement(el);
                }
              }
            }
          }
          #endregion
          #region Orte bestimmen und schreiben.
          {
            ds.RefreshAllPlaces();
            Element[] elements = DataService.Instance.GetElements();
            foreach (var el in elements) {
              el.ElementProp.MarkerInfo.PlaceName=Place.GetNearestPlace(el.ElementProp.MarkerInfo.position).Name;
              DataService.Instance.WriteElement(el);
            }
          }
          #endregion
        }
      } catch { }
      #endregion
    }
    public static void CopyImageCompressed(string sSrcFile,string sDestFile,int nBiggerDim = 1200) {
      using (var imgSrc = Image.Load(sSrcFile)) {
        int nReqHeight;
        int nReqWidth;
        if (imgSrc.Height<imgSrc.Width) {
          nReqWidth=Math.Min(nBiggerDim,imgSrc.Width);
          nReqHeight=(nReqWidth*imgSrc.Height)/imgSrc.Width;
        } else {
          nReqHeight=Math.Min(nBiggerDim,imgSrc.Height);
          nReqWidth=(nReqHeight*imgSrc.Width)/imgSrc.Height;
        }
        imgSrc.Mutate(x => x.Resize(nReqWidth,nReqHeight));
        // GPS-Daten löschen.
        if (imgSrc?.Metadata?.ExifProfile!=null) {
          imgSrc.Metadata.ExifProfile.Parts=SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifParts.ExifTags | SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifParts.IfdTags;
        }
        imgSrc.SaveAsJpeg(sDestFile);
      }
    }
  }
}
