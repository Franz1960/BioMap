using System;
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
    public DataService() {
      DataService.Instance = this;
    }
    public static DataService Instance { get; private set; }
    public string DataDir = "../../data/biomap/";
    public System.Data.IDbConnection DbConnection = null;
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
    public void OperateOnDb(Action<IDbCommand> dbAction) {
      try {
        this.DbConnection.Open();
        try {
          using (IDbCommand command = this.DbConnection.CreateCommand()) {
            dbAction(command);
          }
        } finally {
          this.DbConnection.Close();
        }
      } catch { }
    }
    public Task Init() {
      return Task.Run(() => {
        bool bMigrate = true;
        this.DbConnection = new SQLiteConnection();
        this.DbConnection.ConnectionString = "Data Source="+System.IO.Path.Combine(this.DataDir,"biomap.sqlite");
        this.OperateOnDb((command) => {
          #region Ggf. Tabellenstruktur erzeugen.
          command.CommandText = "CREATE TABLE IF NOT EXISTS places (" +
          "name TEXT PRIMARY KEY NOT NULL," +
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
          command.CommandText = "CREATE TABLE IF NOT EXISTS protocol (" +
          "dt DATETIME NOT NULL," +
          "author TEXT," +
          "text TEXT)";
          command.ExecuteNonQuery();
          command.CommandText = "CREATE TABLE IF NOT EXISTS log (" +
          "dt DATETIME NOT NULL," +
          "user TEXT," +
          "action TEXT)";
          command.ExecuteNonQuery();
          command.CommandText = "CREATE TABLE IF NOT EXISTS users (" +
          "emailaddr TEXT PRIMARY KEY NOT NULL," +
          "fullname TEXT," +
          "level INT," +
          "tan TEXT," +
          "permticket TEXT)";
          command.ExecuteNonQuery();
          command.CommandText = "CREATE TABLE IF NOT EXISTS species (" +
          "spec_id INTEGER PRIMARY KEY AUTOINCREMENT," +
          "genus TEXT NOT NULL," +
          "species TEXT NOT NULL," +
          "commonname_en TEXT," +
          "UNIQUE(species,genus))";
          command.ExecuteNonQuery();
          command.CommandText = "CREATE TABLE IF NOT EXISTS projects (" +
          "prj_id INTEGER PRIMARY KEY AUTOINCREMENT," +
          "name TEXT NOT NULL," +
          "description TEXT," +
          "target_species_id INT," +
          "UNIQUE(name))";
          command.ExecuteNonQuery();
          command.CommandText = "CREATE TABLE IF NOT EXISTS elements (" +
          "name TEXT PRIMARY KEY NOT NULL," +
          "species_id INT," +
          "project_id INT," +
          "category INT NOT NULL," +
          "markerposlat REAL," +
          "markerposlng REAL," +
          "place TEXT," +
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
          #region Prüfen, ob leer, also Migration notwendig.
          {
            command.CommandText = "SELECT name FROM elements";
            var dr = command.ExecuteReader();
            bMigrate=!dr.Read();
            dr.Close();
          }
          #endregion
        });
        //
        this.AddLogEntry("System","Web service started");
        //
        if (bMigrate) {
          Migration.MigrateData();
        }
        this.RefreshAllPlaces();
        //
        lock (this.lockInitialized) {
          this._Initialized?.Invoke(this,EventArgs.Empty);
          this.isInitialized = true;
        }
      });
    }
    public bool SendMail(string sTo,string sSubject,string sTextBody) {
      // EMail per REST-API auf Server itools.de versenden.
      try {
        var client = new System.Net.Http.HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        var requestContent = new System.Net.Http.StringContent("{\"Cmd\":\"SendMail\",\"To\":\""+sTo+"\",\"Subject\":\""+sSubject+"\",\"TextBody\":\""+sTextBody+"\"}");
        requestContent.Headers.ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        bool bSuccess=client.PostAsync("https://itools.de/rfapi/rfapi.php",requestContent).Wait(4000);
        return bSuccess;
      } catch (Exception ex) {
        this.AddLogEntry("SendMail","Exception: "+ex.ToString());
      }
      return false;
    }
    public bool RequestTAN(string sUser,string sFullName) {
      bool bSuccess = false;
      var rng = new Random();
      string sNewTAN = rng.Next(0,999999999).ToString("000000000");
      string sPermTicket = "";
      int nLevel = 100;
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT emailaddr,permticket,level,fullname" +
          " FROM users" +
          " WHERE emailaddr='"+sUser+"'" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          sPermTicket=dr.GetString(1);
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
          "VALUES ('" + sUser + "','" + sNewTAN + "','" + sPermTicket + "','" + ConvInvar.ToString(nLevel) + "','" + sFullName + "')" +
          "";
        command.ExecuteNonQuery();
        bSuccess = this.SendMail(
          sUser,
          "Gelbbauchunken-Projekt TAN: "+sNewTAN,
          "Geben Sie die TAN "+sNewTAN+" in das TAN-Feld auf der Web-Seite ein und bestätigen Sie es.");
      });
      return bSuccess;
    }
    public string ConfirmTAN(string sUser,string sTAN) {
      string sPermTicket = "";
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT tan,permticket" +
          " FROM users" +
          " WHERE emailaddr='"+sUser+"'" +
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
          "UPDATE users SET tan='' WHERE emailaddr='" + sUser + "'" +
          "";
        command.ExecuteNonQuery();
      });
      return sPermTicket;
    }
    public int GetUserLevel(string sUserId,string sPermTicket) {
      int nLevel = 0;
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT level" +
          " FROM users" +
          " WHERE emailaddr='"+sUserId+"' AND permticket='"+sPermTicket+"'" +
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
    public void LoadUser(string sUserId,string sPermTicket,User user) {
      int nLevel = 0;
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT permticket,level,fullname" +
          " FROM users" +
          " WHERE emailaddr='"+sUserId+"'" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          var sRealPermTicket=dr.GetString(0);
          if (string.CompareOrdinal(sPermTicket,sRealPermTicket)==0) {
            nLevel=dr.GetInt32(1);
          }
          user.FullName=dr.GetString(2);
          break;
        }
        dr.Close();
      });
      user.Level=nLevel;
      user.EMail=sUserId;
    }
    public void AddLogEntry(string sUser,string sAction) {
      this.OperateOnDb((command) => {
        command.CommandText = "INSERT INTO log (dt,user,action) VALUES (datetime('now','localtime'),'" + sUser + "','" + sAction + "')";
        command.ExecuteNonQuery();
      });
    }
    public Place[] AllPlaces {
      get;
      private set;
    }
    public Dictionary<string,Place> PlacesByNames {
      get;
      private set;
    } = new Dictionary<string,Place>();
    public void RefreshAllPlaces() {
      var lPlaces = new List<Place>();
      this.PlacesByNames.Clear();
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT name,radius,lat,lng" +
          " FROM places" +
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
          lPlaces.Add(place);
          this.PlacesByNames.Add(place.Name,place);
        }
        dr.Close();
      });
      this.AllPlaces=lPlaces.ToArray();

    }
    public int? GetSpeciesId(string sGenus,string sSpecies) {
      int? nSpeciesId = null;
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT spec_id" +
          " FROM species" +
          " WHERE (genus='"+sGenus+"' AND species='"+sSpecies+"')" +
          "";
        var dr = command.ExecuteReader();
        if (dr.Read()) {
          nSpeciesId = dr.GetInt32(0);
        }
        dr.Close();
      });
      return nSpeciesId;
    }
    public int? GetProjectId(string sProjectName) {
      int? nSpeciesId = null;
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT prj_id" +
          " FROM projects" +
          " WHERE (name='"+sProjectName+"')" +
          "";
        var dr = command.ExecuteReader();
        if (dr.Read()) {
          nSpeciesId = dr.GetInt32(0);
        }
        dr.Close();
      });
      return nSpeciesId;
    }
    public void WriteElement(Element el) {
      this.OperateOnDb((command) => {
        command.CommandText =
          "REPLACE INTO elements (name,species_id,project_id,category,markerposlat,markerposlng,place,uploadtime,uploader,creationtime) " +
          "VALUES ('" + el.ElementName + "'," +
          (el.SpeciesId.HasValue ? ("'" + ConvInvar.ToString(el.SpeciesId.Value) + "'") : ("NULL")) + "," +
          (el.ProjectId.HasValue ? ("'" + ConvInvar.ToString(el.ProjectId.Value) + "'") : ("NULL")) + "," +
          "'" + ConvInvar.ToString(el.ElementProp.MarkerInfo.category) +
          "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.position.lat) +
          "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.position.lng) +
          "','" + el.ElementProp.MarkerInfo.PlaceName +
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
            ",'" + (el.ElementProp.IndivData.TraitValues.TryGetValue("YellowDominance",out int nYD) ? ConvInvar.ToString(nYD) : "") + "'" +
            ",'" + (el.ElementProp.IndivData.TraitValues.TryGetValue("BlackDominance",out int nBD) ? ConvInvar.ToString(nBD) : "") + "'" +
            ",'" + (el.ElementProp.IndivData.TraitValues.TryGetValue("VertBlackBreastCenterStrip",out int nVBBCS) ? ConvInvar.ToString(nVBBCS) : "") + "'" +
            ",'" + (el.ElementProp.IndivData.TraitValues.TryGetValue("HorizBlackBreastBellyStrip",out int nHBBCS) ? ConvInvar.ToString(nHBBCS) : "") + "'" +
            ",'" + (el.ElementProp.IndivData.TraitValues.TryGetValue("ManyIsolatedBlackBellyDots",out int nMIBBD) ? ConvInvar.ToString(nMIBBD) : "") + "'" +
            ")";
          command.ExecuteNonQuery();
        }
      });
    }
    public Element[] GetElements(Filters filters = null,string sSqlCondition = "",string sSqlOrderBy = "elements.creationtime") {
      if (filters!=null) {
        sSqlCondition=filters.AddAllFiltersToWhereClause(sSqlCondition);
      }
      var lElements = new List<Element>();
      this.OperateOnDb((command) => {
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
            var sDateTimeOriginal = dr.GetString(9);
            DateTime.TryParse(sDateTimeOriginal,out dtDateTimeOriginal);
            var el = new Element {
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
                      var oValue = dr.GetValue(nIdx++);
                      var sValue = oValue as string;
                      if (int.TryParse(sValue,out int nValue)) {
                        el.ElementProp.IndivData.TraitValues.Add(sTraitName,nValue);
                      }
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
      return lElements.ToArray();
    }
    public Dictionary<int,List<Element>> GetIndividuals(Filters filters = null,string sAdditionalWhereClause=null) {
      var aaIndisByIId = new Dictionary<int,List<Element>>();
      var sWhereClause = "indivdata.iid>=1";
      sWhereClause=Filters.AddToWhereClause(sWhereClause,sAdditionalWhereClause);
      var aNormedElements = this.GetElements(filters,sWhereClause,"indivdata.iid ASC,elements.creationtime ASC");
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
    public ProtocolEntry[] GetProtocolEntries(Filters filters = null,string sSqlCondition = "",string sSqlOrderBy = "protocol.dt") {
      if (filters!=null) {
        sSqlCondition=filters.AddAllFiltersToWhereClause(sSqlCondition);
      }
      var lProtocolEntries = new List<ProtocolEntry>();
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT protocol.dt" +
          ",protocol.author" +
          ",protocol.text" +
          " FROM protocol" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE ("+sSqlCondition+")")) +
          (string.IsNullOrEmpty(sSqlOrderBy) ? "" : (" ORDER BY "+sSqlOrderBy+"")) +
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
    public LogEntry[] GetLogEntries(Filters filters = null,string sSqlCondition = "",string sSqlOrderBy = "log.dt") {
      if (filters!=null) {
        sSqlCondition=filters.AddAllFiltersToWhereClause(sSqlCondition);
      }
      var lLogEntries = new List<LogEntry>();
      this.OperateOnDb((command) => {
        command.CommandText = "SELECT dt,user,action FROM log" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE ("+sSqlCondition+")")) +
          (string.IsNullOrEmpty(sSqlOrderBy) ? "" : (" ORDER BY "+sSqlOrderBy+"")) +
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
  }
}
