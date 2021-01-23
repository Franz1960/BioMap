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
    #pragma warning disable CS0649
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
    public static async Task MigrateCategories(SessionData sd,Action<int> callbackCompletion) {
      await Task.Run(()=>{
        var ds = DataService.Instance;
        var aElements=ds.GetElements(sd);
        int nLoopCnt=0;
        foreach (var el in aElements) {
          callbackCompletion(((nLoopCnt++)*100)/aElements.Length);
          int? nCat=el.ElementProp?.MarkerInfo?.category;
          switch (nCat.HasValue?nCat.Value:0) {
            case 120: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t(),
              };
              break;
            case 130: 
              el.Classification=new ElementClassification {
                ClassName="Other",
              };
              break;
            case 140: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Anura") },
              };
              break;
            case 141: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Bufo bufo") },
              };
              break;
            case 150: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Anura") },
              };
              break;
            case 151: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Rana temporaria") },
              };
              break;
            case 152: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Anura") },
              };
              break;
            case 153: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Pelophylax") },
              };
              break;
            case 160: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Caudata") },
              };
              break;
            case 161: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Lissotriton vulgaris") },
              };
              break;
            case 162: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Ichthyosaura alpestris") },
              };
              break;
            case 163: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Caudata") },
              };
              break;
            case 164: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Salamandra salamandra") },
              };
              break;
            case 170: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Insecta") },
              };
              break;
            case 173: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Nepa cinerea") },
              };
              break;
            case 174: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Odonata"), Stadium=ElementClassification.Stadium.Larvae },
              };
              break;
            case 210: 
              el.Classification=new ElementClassification {
                ClassName="Habitat",
                Habitat=new ElementClassification.Habitat_t { Quality=0 },
              };
              break;
            case 220: 
            case 230: 
            case 240: 
              el.Classification=new ElementClassification {
                ClassName="Habitat",
                Habitat=new ElementClassification.Habitat_t { Quality=4 },
              };
              break;
            case 242: 
            case 342: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Bombina variegata"), Stadium=ElementClassification.Stadium.Eggs },
              };
              break;
            case 244: 
            case 344: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Bombina variegata"), Stadium=ElementClassification.Stadium.Larvae },
              };
              break;
            case 320: 
              el.Classification=new ElementClassification {
                ClassName="Habitat",
                Habitat=new ElementClassification.Habitat_t { Quality=3 },
              };
              break;
            case 322: 
              el.Classification=new ElementClassification {
                ClassName="Habitat",
                Habitat=new ElementClassification.Habitat_t { Quality=3,Monitoring=true },
              };
              break;
            case 330: 
            case 340: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Bombina variegata"), Stadium=ElementClassification.Stadium.Adults },
              };
              break;
            case 346: 
              el.Classification=new ElementClassification {
                ClassName="Living being",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Bombina variegata"), Stadium=ElementClassification.Stadium.Deads },
              };
              break;
            case 350: 
              el.Classification=new ElementClassification {
                ClassName="ID photo",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Bombina variegata"), Stadium=el.GetWinters()<2?ElementClassification.Stadium.Juveniles:ElementClassification.Stadium.Adults },
              };
              break;
            case 351: 
              el.Classification=new ElementClassification {
                ClassName="Normalized non-ID photo",
                LivingBeing=new ElementClassification.LivingBeing_t { Species=sd.CurrentProject.GetSpecies("Bombina variegata"), Stadium=ElementClassification.Stadium.Adults },
              };
              break;
            default:
              el.Classification=new ElementClassification();
              break;
          }
          ds.WriteElement(sd,el);
        }
      });
    }
    public static async Task MigrateImageSize(SessionData sd,Action<int> callbackCompletion) {
      await Task.Run(()=>{
        var ds = DataService.Instance;
        var sDataDir = ds.GetDataDir(sd);
        ds.AddLogEntry(sd,"Migrating data");
        var sMigSrcDir = System.IO.Path.Combine(sDataDir,"migration_source");
        var aElements=ds.GetElements(sd);
        int nLoopCnt=0;
        foreach (var el in aElements) {
          callbackCompletion(((nLoopCnt++)*100)/aElements.Length);
          if (el.ElementProp?.IndivData?.MeasuredData!=null) {
            try {
              var sMigSrcFilePath = System.IO.Path.Combine(sMigSrcDir,"elements",el.ElementName+".orig.jpg");
              if (System.IO.File.Exists(sMigSrcFilePath)) {
                var sFilePath = ds.GetFilePathForImage(sd.CurrentUser.Project,el.ElementName,true);
                if (System.IO.File.Exists(sFilePath)) {
                  using (var imgMigSrc = Image.Load(sMigSrcFilePath)) {
                    using (var img = Image.Load(sFilePath)) {
                      float fScale=((float)img.Width)/imgMigSrc.Width;
                      //el.ElementProp.IndivData.MeasuredData.OrigHeadPosition.X*=fScale;
                      //el.ElementProp.IndivData.MeasuredData.OrigHeadPosition.Y*=fScale;
                      //el.ElementProp.IndivData.MeasuredData.OrigBackPosition.X*=fScale;
                      //el.ElementProp.IndivData.MeasuredData.OrigBackPosition.Y*=fScale;
                      //for (int i=0;i<3;i++) {
                      //  el.ElementProp.IndivData.MeasuredData.PtsOnCircle[i].X*=fScale;
                      //  el.ElementProp.IndivData.MeasuredData.PtsOnCircle[i].Y*=fScale;
                      //}
                      //ds.WriteElement(sd,el);
                    }
                  }
                }
              }
            } catch { }
          }
        }
      });
    }
   public static async Task MigrateData(SessionData sd,Action<int> callbackCompletion) {
      await Task.Run(()=>{
        var ds = DataService.Instance;
        var sDataDir = ds.GetDataDir(sd);
        ds.AddLogEntry(sd,"Migrating data");
        var sMigSrcDir = System.IO.Path.Combine(sDataDir,"migration_source");
        var sConfDir = System.IO.Path.Combine(sDataDir,"conf");
        var sImagesDir = System.IO.Path.Combine(sDataDir,"images");
        var sImagesOrigDir = System.IO.Path.Combine(sDataDir,"images_orig");
        #region conf
        try {
          var sSrcDir = System.IO.Path.Combine(sMigSrcDir,"conf");
          System.IO.Directory.CreateDirectory(sConfDir);
          foreach (var sSrcPath in System.IO.Directory.GetFiles(sSrcDir)) {
            System.IO.File.Copy(sSrcPath,System.IO.Path.Combine(sConfDir,System.IO.Path.GetFileName(sSrcPath)));
          }
        } catch { }
        #endregion
        #region places.json
        try {
          var sr = new System.IO.StreamReader(System.IO.Path.Combine(sMigSrcDir,"places.json"));
          var sJson = sr.ReadToEnd();
          var aPlaces = JsonConvert.DeserializeObject<Place[]>(sJson);
          sr.Close();
          ds.OperateOnDb(sd,(command) => {
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
        #region notes
        try {
          var aFileNames = System.IO.Directory.GetFiles(System.IO.Path.Combine(sMigSrcDir,"protocol"),"protocol_*.json");
          ds.OperateOnDb(sd,(command) => {
            foreach (var sFileName in aFileNames) {
              var sr = new System.IO.StreamReader(sFileName);
              var sJson = sr.ReadToEnd();
              var pe = JsonConvert.DeserializeObject<Protocol_t>(sJson);
              sr.Close();
              var dt = DateTime.Parse(pe.Content.Timestamp);
              command.CommandText = "INSERT INTO notes (dt,author,text) VALUES ('" + ConvInvar.ToString(dt) + "','" + pe.Content.Author + "','" + pe.Content.Text + "')";
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
          ds.AddLogEntry(sd,"Processing "+lFileNames.Count+" log files beginning with "+lFileNames?[0]);
          ds.OperateOnDb(sd,(command) => {
            var regEx = new System.Text.RegularExpressions.Regex("(.*) User\\((.*)\\): (.*)");
            int nLoopCnt=0;
            foreach (var sFileName in lFileNames) {
              callbackCompletion(((nLoopCnt++)*50)/lFileNames.Count);
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
                    command.CommandText = "INSERT INTO log (dt,user,action) VALUES ('"+ConvInvar.ToString(le.CreationTime)+"','" + le.User + "','" + le.Action + "')";
                    command.ExecuteNonQuery();
                  }
                } catch (Exception ex1) {
                  command.CommandText = "INSERT INTO log (dt,user,action) VALUES ('"+ConvInvar.ToString(DateTime.Now)+"','" + "Migration" + "','" + "Exception: "+ex1.ToString() + "')";
                  command.ExecuteNonQuery();
                }
              }
              sr.Close();
            }
          });
          ds.AddLogEntry(sd,"Migrated "+nTotalLineCount +" log lines");
        } catch (Exception ex) {
          ds.AddLogEntry(sd,"Exception migrating logs: "+ex.ToString());
        }
        #endregion
        #region elements
        try {
          var sElementsDir = System.IO.Path.Combine(sMigSrcDir,"elements");
          var aFileNames = System.IO.Directory.GetFiles(sElementsDir,"*.json");
          {
            System.IO.Directory.CreateDirectory(sImagesDir);
            System.IO.Directory.CreateDirectory(sImagesOrigDir);
            int nLoopCnt=0;
            foreach (var sFileName in aFileNames) {
              callbackCompletion(50+((nLoopCnt++)*50)/aFileNames.Length);
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
                    if (!DateTime.TryParse(sDateTimeOriginal,out var dtOriginal)) {
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
                      //measuredData = new Element.IndivData_t.MeasuredData_t {
                      //  HeadPosition = new System.Numerics.Vector2 {
                      //    X = jIndivData["MeasuredData"]["HeadPosition"]["x"].Value<float>(),
                      //    Y = jIndivData["MeasuredData"]["HeadPosition"]["y"].Value<float>(),
                      //  },
                      //  BackPosition = new System.Numerics.Vector2 {
                      //    X = jIndivData["MeasuredData"]["BackPosition"]["x"].Value<float>(),
                      //    Y = jIndivData["MeasuredData"]["BackPosition"]["y"].Value<float>(),
                      //  },
                      //  HeadBodyLength = jIndivData["MeasuredData"]["HeadBodyLength"].Value<float>(),
                      //};
                      //if (
                      //  jIndivData["MeasuredData"]["OrigHeadPosition"]!=null
                      //  &&
                      //  jIndivData["MeasuredData"]["PtsOnCircle"]!=null
                      //)
                      //  try {
                      //    measuredData.OrigHeadPosition = new System.Numerics.Vector2 {
                      //      X = jIndivData["MeasuredData"]["OrigHeadPosition"]["x"].Value<float>(),
                      //      Y = jIndivData["MeasuredData"]["OrigHeadPosition"]["y"].Value<float>(),
                      //    };
                      //    measuredData.OrigBackPosition = new System.Numerics.Vector2 {
                      //      X = jIndivData["MeasuredData"]["OrigBackPosition"]["x"].Value<float>(),
                      //      Y = jIndivData["MeasuredData"]["OrigBackPosition"]["y"].Value<float>(),
                      //    };
                      //    measuredData.PtsOnCircle = new System.Numerics.Vector2[]
                      //    {
                      //    new System.Numerics.Vector2
                      //    {
                      //      X = jIndivData["MeasuredData"]["PtsOnCircle"][0]["x"].Value<float>(),
                      //      Y = jIndivData["MeasuredData"]["PtsOnCircle"][0]["y"].Value<float>(),
                      //    },
                      //    new System.Numerics.Vector2
                      //    {
                      //      X = jIndivData["MeasuredData"]["PtsOnCircle"][1]["x"].Value<float>(),
                      //      Y = jIndivData["MeasuredData"]["PtsOnCircle"][1]["y"].Value<float>(),
                      //    },
                      //    new System.Numerics.Vector2
                      //    {
                      //      X = jIndivData["MeasuredData"]["PtsOnCircle"][2]["x"].Value<float>(),
                      //      Y = jIndivData["MeasuredData"]["PtsOnCircle"][2]["y"].Value<float>(),
                      //    },
                      //    };
                      //  } catch { }
                    }
                    indivData = new Element.IndivData_t {
                      IId = ConvInvar.ToInt(jIndivData["IId"]?.Value<string>()),
                      Gender = jIndivData?["Gender"]?.Value<string>()?.ToLowerInvariant(),
                      MeasuredData = measuredData,
                    };
                    if (jIndivData["TraitValues"] != null) {
                      var jTraitValues = jIndivData["TraitValues"];
                      foreach (var jTraitValue in jTraitValues.Children()) {
                        if (jTraitValue is JProperty jProperty) {
                          indivData.TraitValues.Add(jProperty.Name,jTraitValues[jProperty.Name].Value<int>());
                        }
                      }
                    }
                  }
                  var el = new Element(sd.CurrentUser.Project) {
                    ElementName = jel["ElementName"].Value<string>(),
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
                        Comment = (jel["ElementProp"]["UploadInfo"]["Comment"]==null) ? "" : jel["ElementProp"]["UploadInfo"]["Comment"].Value<string>(),
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
                  ds.WriteElement(sd,el);
                } catch { }
              }
            }
            #region Jahrgang aller Wiederfänge auf den zuerst ermittelten Jahrgang setzen.
            {
              Dictionary<int,List<Element>> Individuals = DataService.Instance.GetIndividuals(sd);
              foreach (var iid in Individuals.Keys) {
                int? nYoB = null;
                foreach (var el in Individuals[iid]) {
                  int? nElYoB = el.GetYearOfBirth();
                  if (!nYoB.HasValue) {
                    nYoB=el.GetYearOfBirth();
                  } else if (!nElYoB.HasValue || nElYoB.Value!=nYoB.Value) {
                    el.ElementProp.IndivData.DateOfBirth=new DateTime(nYoB.Value,7,1);
                    DataService.Instance.WriteElement(sd,el);
                  }
                }
              }
            }
            #endregion
            #region Orte bestimmen und schreiben.
            {
              Element[] elements = DataService.Instance.GetElements(sd);
              foreach (var el in elements) {
                el.ElementProp.MarkerInfo.PlaceName=Place.GetNearestPlace(sd,el.ElementProp.MarkerInfo.position).Name;
                DataService.Instance.WriteElement(sd,el);
              }
            }
            #endregion
          }
        } catch { }
        #endregion
      });
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
