﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace BioMap
{
  public class DataService
  {
    public readonly DateTime ProjectStart = new DateTime(2019,4,1);
    public readonly double ProjectArea = 3139.5;
    public readonly double ProjectCenterLat = 49.054052;
    public readonly double ProjectCenterLng = 12.217661;
    public DataService() {
      DataService.Instance = this;
    }
    public static DataService Instance { get; private set; }
    private string DataBaseDir = "../../../data/";
    public readonly string BaseProject = "biomap";
    public string[] GetAllProjects() {
      var lProjects=new List<string>();
      try {
        foreach (var sDirPath in System.IO.Directory.GetDirectories(DataBaseDir)) {
          string sDirName = new System.IO.DirectoryInfo(sDirPath).Name;
          if (sDirName.StartsWith(BaseProject)) {
            if (sDirName.Length<=BaseProject.Length+1) {
              lProjects.Add("");
            } else {
              lProjects.Add(sDirName.Substring(BaseProject.Length+1));
            }
          }
        }
      } catch { }
      lProjects.Sort();
      return lProjects.ToArray();
    }
    public string GetDataDir(string sProject) {
      string sProjectDir="biomap";
      if (!string.IsNullOrEmpty(sProject)) {
        sProjectDir+="."+sProject;
      }
      return DataBaseDir+sProjectDir+"/";
    }
    public string GetDataDir(SessionData sd) {
      return this.GetDataDir(sd.CurrentUser.Project);
    }
    public event EventHandler Initialized {
      add {
        lock (this.lockInitialized) {
          if (this.isInitialized) {
            value.Invoke(this,EventArgs.Empty);
          } else {
            this._Initialized += value;
          }
        }
      }
      remove {
        this._Initialized -= value;
      }
    }
    private event EventHandler _Initialized;
    private bool isInitialized = false;
    private readonly object lockInitialized = new object();
    public void OperateOnDb(SessionData sd,Action<IDbCommand> dbAction) {
      this.OperateOnDb(sd.CurrentUser.Project,dbAction);
    }
    private void OperateOnDb(string sProject,Action<IDbCommand> dbAction) {
      try {
        var sDbFilePath = System.IO.Path.Combine(this.GetDataDir(sProject),"biomap.sqlite");
        var dbConnection = new SQLiteConnection();
        dbConnection.ConnectionString = "Data Source="+sDbFilePath;
        bool bDbFileExisted = System.IO.File.Exists(sDbFilePath);
        dbConnection.Open();
        if (!bDbFileExisted) {
          using (IDbCommand command = dbConnection.CreateCommand()) {
            #region Ggf. Tabellenstruktur erzeugen.
            command.CommandText = "CREATE TABLE IF NOT EXISTS places (" +
            "name TEXT PRIMARY KEY NOT NULL," +
            "traitvalues TEXT," +
            "radius REAL," +
            "lat REAL," +
            "lng REAL)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS monitoringevents (" +
            "place TEXT," +
            "kw INT," +
            "user TEXT," +
            "value TEXT)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS notes (" +
            "dt DATETIME NOT NULL," +
            "author TEXT," +
            "text TEXT,UNIQUE(dt,author))";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS log (" +
            "dt DATETIME NOT NULL," +
            "user TEXT," +
            "action TEXT)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS users (" +
            "emailaddr TEXT PRIMARY KEY NOT NULL COLLATE NOCASE," +
            "fullname TEXT," +
            "level INT," +
            "tan TEXT," +
            "permticket TEXT)";
            command.ExecuteNonQuery();
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS elements (" +
            "name TEXT PRIMARY KEY NOT NULL," +
            "category INT NOT NULL," +
            "markerposlat REAL," +
            "markerposlng REAL," +
            "place TEXT," +
            "comment TEXT," +
            "uploadtime DATETIME NOT NULL," +
            "uploader TEXT NOT NULL," +
            "creationtime DATETIME NOT NULL)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS photos (" +
            "name TEXT PRIMARY KEY NOT NULL," +
            "filename TEXT NOT NULL," +
            "exifmake TEXT," +
            "exifmodel TEXT," +
            "exifdatetimeoriginal DATETIME)";
            command.ExecuteNonQuery();
            command.CommandText = "CREATE TABLE IF NOT EXISTS indivdata (" +
            "name TEXT PRIMARY KEY NOT NULL," +
            "normcirclepos0x REAL," +
            "normcirclepos0y REAL," +
            "normcirclepos1x REAL," +
            "normcirclepos1y REAL," +
            "normcirclepos2x REAL," +
            "normcirclepos2y REAL," +
            "headposx REAL," +
            "headposy REAL," +
            "backposx REAL," +
            "backposy REAL," +
            "origheadposx REAL," +
            "origheadposy REAL," +
            "origbackposx REAL," +
            "origbackposy REAL," +
            "headbodylength REAL," +
            "traitYellowDominance INT," +
            "traitBlackDominance INT," +
            "traitVertBlackBreastCenterStrip INT," +
            "traitHorizBlackBreastBellyStrip INT," +
            "traitManyIsolatedBlackBellyDots INT," +
            "dateofbirth DATETIME," +
            "ageyears REAL," +
            "winters INT," +
            "gender TEXT," +
            "iid INT)";
            command.ExecuteNonQuery();
            #endregion
          }
        }
        try {
          using (IDbCommand command = dbConnection.CreateCommand()) {
            dbAction(command);
          }
        } finally {
          dbConnection.Close();
          dbConnection.Dispose();
        }
      } catch { }
    }
    public Task Init() {
      return Task.Run(() => {
        // Create base project if it does not exist.
        this.OperateOnDb("",(command) => {
        });
        //
        lock (this.lockInitialized) {
          this._Initialized?.Invoke(this,EventArgs.Empty);
          this.isInitialized = true;
        }
      });
    }
    public bool IsMigrationInProcess = false;
    public bool SendMail(SessionData sd,string sTo,string sSubject,string sTextBody) {
      // EMail per REST-API auf Server itools.de versenden.
      try {
        var client = new System.Net.Http.HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        var requestContent = new System.Net.Http.StringContent("{\"Cmd\":\"SendMail\",\"To\":\""+sTo+"\",\"Subject\":\""+sSubject+"\",\"TextBody\":\""+sTextBody+"\"}");
        requestContent.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        bool bSuccess=client.PostAsync("https://itools.de/rfapi/rfapi.php",requestContent).Wait(4000);
        return bSuccess;
      } catch (Exception ex) {
        this.AddLogEntry(sd,"SendMail Exception: "+ex.ToString());
      }
      return false;
    }
    public bool RequestTAN(SessionData sd,string sFullName) {
      bool bSuccess = false;
      var rng = new Random();
      string sNewTAN = rng.Next(0,999999999).ToString("000000000");
      string sPermTicket = "";
      int nLevel = 100;
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT emailaddr,permticket,level,fullname" +
          " FROM users" +
          " WHERE emailaddr='"+sd.CurrentUser.EMail+"'" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          sPermTicket=dr.GetValue(1) as string;
          nLevel=dr.GetInt32(2);
          if (string.IsNullOrEmpty(sFullName)) {
            sFullName=dr.GetString(3);
          }
          break;
        }
        dr.Close();
        if (string.IsNullOrEmpty(sPermTicket)) {
          sPermTicket = rng.Next(0,999999999).ToString("000000000");
        }
        command.CommandText =
          "REPLACE INTO users (emailaddr,tan,permticket,level,fullname) " +
          "VALUES ('" + sd.CurrentUser.EMail + "','" + sNewTAN + "','" + sPermTicket + "'," + ConvInvar.ToString(nLevel) + ",'" + sFullName + "')" +
          "";
        command.ExecuteNonQuery();
        bSuccess = this.SendMail(
          sd,
          sd.CurrentUser.EMail,
          "Gelbbauchunken-Projekt TAN: "+sNewTAN,
          "Geben Sie die TAN "+sNewTAN+" in das TAN-Feld auf der Web-Seite ein und bestätigen Sie es.");
      });
      return bSuccess;
    }
    public string ConfirmTAN(SessionData sd,string sTAN) {
      string sPermTicket = "";
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT tan,permticket" +
          " FROM users" +
          " WHERE emailaddr='"+sd.CurrentUser.EMail+"'" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          string sRealTAN=dr.GetString(0);
          if (string.CompareOrdinal(sTAN,sRealTAN)==0) {
            sPermTicket=dr.GetString(1);
          }
          break;
        }
        dr.Close();
        command.CommandText =
          "UPDATE users SET tan='' WHERE emailaddr='" + sd.CurrentUser.EMail + "'" +
          "";
        command.ExecuteNonQuery();
      });
      return sPermTicket;
    }
    public int GetUserLevel(SessionData sd,string sPermTicket) {
      int nLevel = 0;
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT level" +
          " FROM users" +
          " WHERE emailaddr='"+sd.CurrentUser.EMail+"' AND permticket='"+sPermTicket+"'" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          nLevel=dr.GetInt32(0);
          break;
        }
        dr.Close();
      });
      return nLevel;
    }
    public void LoadUser(SessionData sd,string sPermTicket,User user) {
      int nLevel = 0;
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT permticket,level,fullname" +
          " FROM users" +
          " WHERE emailaddr='"+sd.CurrentUser.EMail+"'" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          var sRealPermTicket = dr.GetValue(0) as string;
          if (string.CompareOrdinal(sPermTicket,sRealPermTicket)==0) {
            nLevel=dr.GetInt32(1);
          }
          user.FullName=dr.GetString(2);
          break;
        }
        dr.Close();
      });
      user.Level=nLevel;
      user.EMail=sd.CurrentUser.EMail;
    }
    #region Users.
    private string[] PrevAllUsers = null;
    public User[] GetAllUsers(SessionData sd) {
      var lUsers = new List<User>();
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT emailaddr,level,fullname" +
          " FROM users" +
          " ORDER BY emailaddr" +
          "";
        var dr = command.ExecuteReader();
        try {
          while (dr.Read()) {
            var user = new User {
              EMail=dr.GetString(0),
              Level=dr.GetInt32(1),
              FullName=dr.GetString(2),
            };
            lUsers.Add(user);
          }
        } finally {
          dr.Close();
        }
      });
      this.PrevAllUsers=lUsers.Select(a => JsonConvert.SerializeObject(a)).ToArray();
      return lUsers.ToArray();
    }
    public void WriteUser(SessionData sd,User user) {
      this.OperateOnDb(sd,(command) => {
        command.CommandText =
          "UPDATE users SET " +
          "level='"+ConvInvar.ToString(user.Level)+"'," +
          "fullname='"+user.FullName+"' WHERE emailaddr='"+user.EMail+"'";
        command.ExecuteNonQuery();
      });
    }
    #endregion
    #region Places.
    private System.Numerics.Matrix3x2 AlienationTransformation {
      get {
        if (!this._AlienationTransformation.HasValue) {
          var m1=System.Numerics.Matrix3x2.CreateTranslation((float)(-ProjectCenterLng),(float)(-ProjectCenterLat));
          var m2=System.Numerics.Matrix3x2.CreateRotation(-0.33f);
          var m2a=System.Numerics.Matrix3x2.CreateSkew(0f,0.25f);
          var m2b=System.Numerics.Matrix3x2.CreateTranslation(-0.0005f,-0.0013f);
          var m3=System.Numerics.Matrix3x2.CreateScale(0.99f,0.95f);
          System.Numerics.Matrix3x2.Invert(m1,out var m4);
          this._AlienationTransformation=m1*m2*m2a*m2b*m3*m4;
        }
        return this._AlienationTransformation.Value;
      }
    }
    private System.Numerics.Matrix3x2? _AlienationTransformation=null;
    private LatLng GetAlienatedPosition(LatLng position) {
      var vRes=System.Numerics.Vector2.Transform(new System.Numerics.Vector2((float)position.lng,(float)position.lat),this.AlienationTransformation);
      return new LatLng {
        lat=vRes.Y,
        lng=vRes.X,
      };
    }
    public Place[] GetPlaces(SessionData sd,string sWhereClause=null) {
      if (this.IsMigrationInProcess || (sd!=null && sd.MaySeeRealLocations)) {
        return this.GetRealPlaces(sd,sWhereClause);
      } else {
        var lAlienatedPlaces=new List<Place>();
        foreach (var place in this.GetRealPlaces(sd,sWhereClause)) {
          var ap=new Place {
            Name=place.Name,
            Radius=place.Radius,
            LatLng=GetAlienatedPosition(place.LatLng),
          };
          ap.TraitValues.Clear();
          ap.TraitValues.AddRange(place.TraitValues);
          lAlienatedPlaces.Add(ap);
        }
        return lAlienatedPlaces.ToArray();
      }
    }
    public Place GetPlaceByName(SessionData sd,string sPlaceName) {
      var aPlaces=this.GetPlaces(sd,"name='"+sPlaceName+"'");
      if (aPlaces.Length<1) {
        return null;
      } else {
        return aPlaces[0];
      }
    }
    private Place[] GetRealPlaces(SessionData sd,string sWhereClause=null) {
      var lPlaces = new List<Place>();
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT name,radius,lat,lng,traitvalues" +
          " FROM places" +
          ((sWhereClause==null)?"":sWhereClause) +
          " ORDER BY name" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          var place = new Place {
            Name = dr.GetString(0),
            Radius = dr.GetDouble(1),
            LatLng = new LatLng {
              lat = dr.GetDouble(2),
              lng = dr.GetDouble(3),
            },
          };
          var sTraitsJson = dr.GetValue(4) as string;
          if (!string.IsNullOrEmpty(sTraitsJson)) {
            var naTraitValues = JsonConvert.DeserializeObject<int[]>(sTraitsJson);
            for (int i = 0;i<Math.Min(naTraitValues.Length,place.TraitValues.Count);i++) {
              place.TraitValues[i]=naTraitValues[i];
            }
          }
          lPlaces.Add(place);
        }
        dr.Close();
      });
      return lPlaces.ToArray();
    }
    public void WritePlace(SessionData sd,Place place) {
      bool bChanged=true;
      {
        var prevPlace=this.GetPlaceByName(sd,place.Name);
        if (prevPlace!=null) {
          var prevJson=JsonConvert.SerializeObject(prevPlace);
          var actJson=JsonConvert.SerializeObject(place);
          bChanged=(string.CompareOrdinal(actJson,prevJson)!=0);
        }
      }
      if (bChanged) {
        this.OperateOnDb(sd,(command) => {
          var sTraitJson = JsonConvert.SerializeObject(place.TraitValues);
          command.CommandText =
            "UPDATE places SET "+
            "radius='"+ConvInvar.ToString(place.Radius)+"',"+
            "traitvalues='"+sTraitJson+"' WHERE name='"+place.Name+"'";
          command.ExecuteNonQuery();
        });
      }
    }
    #endregion
    public void WriteElement(SessionData sd,Element el) {
      this.WriteElement(sd.CurrentUser.Project,el);
    }
    public void WriteElement(string sProject,Element el) {
      this.OperateOnDb(sProject,(command) => {
        command.CommandText =
          "REPLACE INTO elements (name,category,markerposlat,markerposlng,place,comment,uploadtime,uploader,creationtime) " +
          "VALUES ('" + el.ElementName + "'," +
          "'" + ConvInvar.ToString(el.ElementProp.MarkerInfo.category) +
          "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.position.lat) +
          "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.position.lng) +
          "','" + el.ElementProp.MarkerInfo.PlaceName +
          "','" + el.ElementProp.UploadInfo.Comment +
          "','" + ConvInvar.ToString(el.ElementProp.UploadInfo.Timestamp) +
          "','" + el.ElementProp.UploadInfo.UserId +
          "','" + ConvInvar.ToString(el.ElementProp.CreationTime) +
          "')";
        command.ExecuteNonQuery();
        if (el.ElementProp.ExifData!=null) {
          command.CommandText =
            "REPLACE INTO photos (name,filename,exifmake,exifmodel,exifdatetimeoriginal) " +
            "VALUES ('" + el.ElementName +
            "','" + el.ElementName +
            "','" + ((el.ElementProp.ExifData == null) ? "" : el.ElementProp.ExifData.Make?.TrimEnd('\0')) +
            "','" + ((el.ElementProp.ExifData == null) ? "" : el.ElementProp.ExifData.Model?.TrimEnd('\0')) +
            "','" + ((el.ElementProp.ExifData == null || !el.ElementProp.ExifData.DateTimeOriginal.HasValue) ? "" : ConvInvar.ToString(el.ElementProp.ExifData.DateTimeOriginal.Value)) +
            "')";
          command.ExecuteNonQuery();
        }
        if (el.ElementProp.MarkerInfo.category==350 || el.ElementProp.MarkerInfo.category==351) {
          command.CommandText =
            "REPLACE INTO indivdata (name,normcirclepos0x,normcirclepos0y,normcirclepos1x,normcirclepos1y,normcirclepos2x,normcirclepos2y" +
            ",headposx,headposy,backposx,backposy,origheadposx,origheadposy,origbackposx,origbackposy,headbodylength,dateofbirth,ageyears,winters,gender,iid" +
            ",traitYellowDominance" +
            ",traitBlackDominance" +
            ",traitVertBlackBreastCenterStrip" +
            ",traitHorizBlackBreastBellyStrip" +
            ",traitManyIsolatedBlackBellyDots" +
            ") VALUES ('" + el.ElementName + "'" +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.PtsOnCircle[0].X) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.PtsOnCircle[0].Y) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.PtsOnCircle[1].X) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.PtsOnCircle[1].Y) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.PtsOnCircle[2].X) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.PtsOnCircle[2].Y) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.HeadPosition.X) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.HeadPosition.Y) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.BackPosition.X) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.BackPosition.Y) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.OrigHeadPosition.X) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.OrigHeadPosition.Y) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.OrigBackPosition.X) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.OrigBackPosition.Y) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.HeadBodyLength) +
            ",'" + ConvInvar.ToString(el.ElementProp.IndivData.DateOfBirth) + "'" +
            "," + ConvInvar.ToString(el.GetAgeYears()) +
            "," + ConvInvar.ToString(el.GetWinters()) +
            ",'" + el.ElementProp.IndivData.Gender+"'" +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.IId) +
            "," + (el.ElementProp.IndivData.TraitValues.TryGetValue("YellowDominance",out int nYD) ? ConvInvar.ToString(nYD) : "0") + "" +
            "," + (el.ElementProp.IndivData.TraitValues.TryGetValue("BlackDominance",out int nBD) ? ConvInvar.ToString(nBD) : "0") + "" +
            "," + (el.ElementProp.IndivData.TraitValues.TryGetValue("VertBlackBreastCenterStrip",out int nVBBCS) ? ConvInvar.ToString(nVBBCS) : "0") + "" +
            "," + (el.ElementProp.IndivData.TraitValues.TryGetValue("HorizBlackBreastBellyStrip",out int nHBBCS) ? ConvInvar.ToString(nHBBCS) : "0") + "" +
            "," + (el.ElementProp.IndivData.TraitValues.TryGetValue("ManyIsolatedBlackBellyDots",out int nMIBBD) ? ConvInvar.ToString(nMIBBD) : "0") + "" +
            ")";
          command.ExecuteNonQuery();
        }
      });
    }
    public void DeleteElement(SessionData sd,Element el) {
      this.OperateOnDb(sd,(command) => {
        command.CommandText =
          "DELETE FROM elements WHERE (name='"+el.ElementName+"')";
        command.ExecuteNonQuery();
        command.CommandText =
          "DELETE FROM photos WHERE (name='"+el.ElementName+"')";
        command.ExecuteNonQuery();
        command.CommandText =
          "DELETE FROM indivdata WHERE (name='"+el.ElementName+"')";
        command.ExecuteNonQuery();
      });
      foreach (var sFolder in new[] { "images","images_orig" }) {
        try {
          string sFilePath = System.IO.Path.Combine(this.GetDataDir(sd),System.IO.Path.Combine(sFolder,el.ElementName));
          if (System.IO.File.Exists(sFilePath)) {
            System.IO.File.Delete(sFilePath);
          }
        } catch { }
      }
    }
    public Element[] GetElements(SessionData sd,Filters filters = null,string sSqlCondition = "",string sSqlOrderBy = "elements.creationtime") {
      if (filters!=null) {
        sSqlCondition=filters.AddAllFiltersToWhereClause(sSqlCondition);
      }
      var lElements = new List<Element>();
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT elements.name" +
          ",elements.category" +
          ",elements.markerposlat" +
          ",elements.markerposlng" +
          ",elements.uploadtime" +
          ",elements.uploader" +
          ",elements.creationtime" +
          ",photos.exifmake" +
          ",photos.exifmodel" +
          ",photos.exifdatetimeoriginal" +
          ",indivdata.iid" +
          ",indivdata.gender" +
          ",indivdata.dateofbirth" +
          ",indivdata.traitYellowDominance" +
          ",indivdata.traitBlackDominance" +
          ",indivdata.traitVertBlackBreastCenterStrip" +
          ",indivdata.traitHorizBlackBreastBellyStrip" +
          ",indivdata.traitManyIsolatedBlackBellyDots" +
          ",indivdata.headbodylength" +
          ",indivdata.origheadposx" +
          ",indivdata.origheadposy" +
          ",indivdata.origbackposx" +
          ",indivdata.origbackposy" +
          ",indivdata.headposx" +
          ",indivdata.headposy" +
          ",indivdata.backposx" +
          ",indivdata.backposy" +
          ",indivdata.normcirclepos0x" +
          ",indivdata.normcirclepos0y" +
          ",indivdata.normcirclepos1x" +
          ",indivdata.normcirclepos1y" +
          ",indivdata.normcirclepos2x" +
          ",indivdata.normcirclepos2y" +
          ",elements.place" +
          ",elements.comment" +
          " FROM elements" +
          " LEFT JOIN indivdata ON (indivdata.name=elements.name)" +
          " LEFT JOIN photos ON (photos.name=elements.name)" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE ("+sSqlCondition+")")) +
          (string.IsNullOrEmpty(sSqlOrderBy) ? "" : (" ORDER BY "+sSqlOrderBy+"")) +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          try {
            var sElementName = dr.GetString(0);
            DateTime dtDateTimeOriginal = dr.GetDateTime(4);
            var oDateTimeOriginal = dr.GetValue(9);
            if (oDateTimeOriginal is string sDateTimeOriginal) {
              DateTime.TryParse(sDateTimeOriginal,out dtDateTimeOriginal);
            }
            var el = new Element(sd.CurrentUser.Project) {
              ElementName = sElementName,
              ElementProp = new Element.ElementProp_t {
                MarkerInfo = new Element.MarkerInfo_t {
                  category = dr.GetInt32(1),
                  position = new LatLng {
                    lat = dr.GetDouble(2),
                    lng = dr.GetDouble(3),
                  },
                  PlaceName = dr.GetString(33),
                },
                UploadInfo = new Element.UploadInfo_t {
                  Timestamp = dr.GetDateTime(4),
                  UserId = dr.GetString(5),
                  Comment = dr.GetString(34),
                },
                CreationTime = dr.GetDateTime(6),
              }
            };
            if (!dr.IsDBNull(7) && !dr.IsDBNull(8)) {
              el.ElementProp.ExifData = new Element.ExifData_t {
                Make = dr.GetString(7),
                Model = dr.GetString(8),
                DateTimeOriginal = dtDateTimeOriginal,
              };
              if (!dr.IsDBNull(10)) {
                el.ElementProp.IndivData = new Element.IndivData_t {
                  IId = dr.GetInt32(10),
                  Gender = dr.GetString(11),
                  DateOfBirth = dr.GetDateTime(12),
                };
                if (!dr.IsDBNull(18)) {
                  el.ElementProp.IndivData.TraitValues = new Dictionary<string,int>();
                  {
                    int nIdx = 13;
                    foreach (var sTraitName in new string[] { "YellowDominance","BlackDominance","VertBlackBreastCenterStrip","HorizBlackBreastBellyStrip","ManyIsolatedBlackBellyDots" }) {
                      try {
                        var nValue = dr.GetInt32(nIdx++);
                        el.ElementProp.IndivData.TraitValues.Add(sTraitName,nValue);
                      } catch { }
                    }
                  }
                  el.ElementProp.IndivData.MeasuredData = new Element.IndivData_t.MeasuredData_t {
                    HeadBodyLength = dr.GetDouble(18),
                    OrigHeadPosition = new System.Numerics.Vector2(dr.GetFloat(19),dr.GetFloat(20)),
                    OrigBackPosition = new System.Numerics.Vector2(dr.GetFloat(21),dr.GetFloat(22)),
                    HeadPosition = new System.Numerics.Vector2(dr.GetFloat(23),dr.GetFloat(24)),
                    BackPosition = new System.Numerics.Vector2(dr.GetFloat(25),dr.GetFloat(26)),
                    PtsOnCircle = new System.Numerics.Vector2[]
                    {
                      new System.Numerics.Vector2(dr.GetFloat(27), dr.GetFloat(28)),
                      new System.Numerics.Vector2(dr.GetFloat(29), dr.GetFloat(30)),
                      new System.Numerics.Vector2(dr.GetFloat(31), dr.GetFloat(32)),
                    },
                  };
                }
              }
            }
            lElements.Add(el);
          } catch { }
        }
        dr.Close();
      });
      if (this.IsMigrationInProcess || (sd!=null && sd.MaySeeRealLocations)) {
        return lElements.ToArray();
      } else {
        foreach (var el in lElements.ToArray()) {
          el.ElementProp.MarkerInfo.position=this.GetAlienatedPosition(el.ElementProp.MarkerInfo.position);
        }
        return lElements.ToArray();
      }
    }
    public Dictionary<int,List<Element>> GetIndividuals(SessionData sd,Filters filters = null,string sAdditionalWhereClause=null) {
      var aaIndisByIId = new Dictionary<int,List<Element>>();
      var sWhereClause = "indivdata.iid>=1";
      sWhereClause=Filters.AddToWhereClause(sWhereClause,sAdditionalWhereClause);
      var aNormedElements = this.GetElements(sd,filters,sWhereClause,"indivdata.iid ASC,elements.creationtime ASC");
      foreach (var el in aNormedElements) {
        if (el.ElementProp.MarkerInfo.category==350 && el.ElementProp.IndivData!=null) {
          var idx = el.ElementProp.IndivData.IId;
          if (!aaIndisByIId.ContainsKey(idx)) {
            aaIndisByIId.Add(idx,new List<Element>());
          }
          aaIndisByIId[idx].Add(el);
        }
      }
      return aaIndisByIId;
    }
    public void AddOrUpdateProtocolEntry(SessionData sd,ProtocolEntry pe) {
      this.OperateOnDb(sd,(command) => {
        if (string.IsNullOrEmpty(pe.Text)) {
          command.CommandText = "DELETE FROM notes WHERE (dt='" + ConvInvar.ToString(pe.CreationTime) + "' AND author='"+pe.Author+"')";
        } else {
          command.CommandText = "REPLACE INTO notes (dt,author,text) VALUES ('" + ConvInvar.ToString(pe.CreationTime) + "','"+pe.Author+"','"+pe.Text+"')";
        }
        command.ExecuteNonQuery();
      });
    }
    public ProtocolEntry[] GetProtocolEntries(SessionData sd,Filters filters = null,string sSqlCondition = "",string sSqlOrderBy = "notes.dt",uint nLimit = 0) {
      if (filters!=null) {
        sSqlCondition=filters.AddAllFiltersToWhereClause(sSqlCondition);
      }
      var lProtocolEntries = new List<ProtocolEntry>();
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT notes.dt" +
          ",notes.author" +
          ",notes.text" +
          " FROM notes" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE ("+sSqlCondition+")")) +
          (string.IsNullOrEmpty(sSqlOrderBy) ? "" : (" ORDER BY "+sSqlOrderBy+"")) +
          (nLimit==0 ? "" : (" LIMIT "+nLimit+"")) +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          try {
            var pe = new ProtocolEntry() {
              CreationTime=dr.GetDateTime(0),
              Author=dr.GetString(1),
              Text=dr.GetString(2),
            };
            lProtocolEntries.Add(pe);
          } catch { }
        }
        dr.Close();
      });
      return lProtocolEntries.ToArray();
    }
    public string[] GetProtocolAuthors(SessionData sd,Filters filters = null) {
      string sSqlCondition = filters.AddAllFiltersToWhereClause("");
      var lProtocolAuthors = new List<string>();
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT DISTINCT author FROM notes" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE ("+sSqlCondition+")")) +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          lProtocolAuthors.Add(dr.GetString(0));
        }
        dr.Close();
      });
      return lProtocolAuthors.ToArray();
    }
    public void AddLogEntry(SessionData sd,string sAction) {
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "INSERT INTO log (dt,user,action) VALUES ('"+ConvInvar.ToString(DateTime.Now)+"','" + sd.CurrentUser.EMail + "','" + sAction + "')";
        command.ExecuteNonQuery();
      });
    }
    public LogEntry[] GetLogEntries(SessionData sd,Filters filters = null,string sSqlCondition = "",string sSqlOrderBy = "log.dt",uint nLimit = 0) {
      if (filters!=null) {
        sSqlCondition=filters.AddAllFiltersToWhereClause(sSqlCondition);
      }
      var lLogEntries = new List<LogEntry>();
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT dt,user,action FROM log" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE ("+sSqlCondition+")")) +
          (string.IsNullOrEmpty(sSqlOrderBy) ? "" : (" ORDER BY "+sSqlOrderBy+"")) +
          (nLimit==0 ? "" : (" LIMIT "+nLimit+"")) +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          try {
            var pe = new LogEntry() {
              CreationTime=dr.GetDateTime(0),
              User=dr.GetString(1),
              Action=dr.GetString(2),
            };
            lLogEntries.Add(pe);
          } catch { }
        }
        dr.Close();
      });
      return lLogEntries.ToArray();
    }
    public string[] GetLogUsers(SessionData sd,Filters filters = null) {
      string sSqlCondition = filters.AddAllFiltersToWhereClause("");
      var lLogUsers = new List<string>();
      this.OperateOnDb(sd,(command) => {
        command.CommandText = "SELECT DISTINCT user FROM log" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE ("+sSqlCondition+")")) +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          lLogUsers.Add(dr.GetString(0));
        }
        dr.Close();
      });
      return lLogUsers.ToArray();
    }
  }
}
