using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleMapsComponents.Maps;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
  public class DataService
  {
    public DataService(string dataDir) {
      this.dataBaseDir = dataDir;
    }
    private readonly string dataBaseDir;
    private readonly string baseProject = "biomap";
    private static string GetEscapedSql(string sInput) {
      return sInput?.Replace("'", "''");
    }
    public string[] GetAllProjects() {
      var lProjects = new List<string>();
      try {
        foreach (string sDirPath in System.IO.Directory.GetDirectories(this.dataBaseDir)) {
          string sDirName = new System.IO.DirectoryInfo(sDirPath).Name;
          if (sDirName.StartsWith(this.baseProject)) {
            if (sDirName.Length <= this.baseProject.Length + 1) {
              lProjects.Add("");
            } else {
              lProjects.Add(sDirName.Substring(this.baseProject.Length + 1));
            }
          }
        }
      } catch { }
      lProjects.Sort();
      return lProjects.ToArray();
    }
    public string GetDataDir(string sProject) {
      string sProjectDir = this.baseProject;
      if (!string.IsNullOrEmpty(sProject)) {
        sProjectDir += "." + sProject;
      }
      return this.dataBaseDir + "/" + sProjectDir + "/";
    }
    public string GetDataDir(SessionData sd) => this.GetDataDir(sd.CurrentUser.Project);
    public string GetTempDir(string sProject) {
      string sDir = this.GetDataDir(sProject);
      string sFilePath = System.IO.Path.Combine(sDir, "temp");
      return sFilePath;
    }
    public string GetContentFromFileType(string sFileType) {
      string sLowerType = sFileType.ToLowerInvariant();
      string sContentType =
        string.CompareOrdinal(sLowerType, ".pdf") == 0 ? "application/pdf" :
        string.CompareOrdinal(sLowerType, ".png") == 0 ? "image/png" :
        string.CompareOrdinal(sLowerType, ".jpg") == 0 ? "image/jpeg" :
        string.CompareOrdinal(sLowerType, ".jpeg") == 0 ? "image/jpeg" :
        string.CompareOrdinal(sLowerType, ".mp4") == 0 ? "video/mp4" :
        "text/plain";
      return sContentType;
    }
    public string GetConfDir(string sProject) {
      string sDir = this.GetDataDir(sProject);
      string sFilePath = System.IO.Path.Combine(sDir, "conf");
      if (!System.IO.Directory.Exists(sFilePath)) {
        System.IO.Directory.CreateDirectory(sFilePath);
      }
      return sFilePath;
    }
    public Document[] GetConf(string sProject, string sSearchPattern = "*.*") {
      string sDir = this.GetConfDir(sProject);
      if (System.IO.Directory.Exists(sDir)) {
        string[] aFiles = System.IO.Directory.GetFiles(sDir, sSearchPattern);
        Array.Sort(aFiles);
        IEnumerable<Document> aDocuments = aFiles.Select(sFile => new Document {
          ContentType = this.GetContentFromFileType(System.IO.Path.GetExtension(sFile)),
          DisplayName = System.IO.Path.GetFileName(sFile),
          Filename = System.IO.Path.GetFileName(sFile),
        });
        return aDocuments.ToArray();
      }
      return Array.Empty<Document>();
    }
    public void DeleteConf(string sProject, string sFilename) {
      try {
        string sFilepath = ConfFileController.GetFilePathForConfFile(this, sProject, sFilename);
        if (System.IO.File.Exists(sFilepath)) {
          System.IO.File.Delete(sFilepath);
        }
      } catch { }
    }
    public string GetDocsDir(string sProject) {
      string sDir = this.GetDataDir(sProject);
      string sFilePath = System.IO.Path.Combine(sDir, "docs");
      if (!System.IO.Directory.Exists(sFilePath)) {
        System.IO.Directory.CreateDirectory(sFilePath);
      }
      return sFilePath;
    }
    public Document[] GetDocs(string sProject, string sSearchPattern = "*.*") {
      string sDir = this.GetDocsDir(sProject);
      if (System.IO.Directory.Exists(sDir)) {
        string[] aFiles = System.IO.Directory.GetFiles(sDir, sSearchPattern);
        Array.Sort(aFiles);
        IEnumerable<Document> aDocuments = aFiles.Select(sFile => new Document {
          ContentType = this.GetContentFromFileType(System.IO.Path.GetExtension(sFile)),
          DisplayName = System.IO.Path.GetFileName(sFile),
          Filename = System.IO.Path.GetFileName(sFile),
        });
        return aDocuments.ToArray();
      }
      return Array.Empty<Document>();
    }
    public void DeleteDoc(string sProject, string sFilename) {
      try {
        string sFilepath = DocumentController.GetFilePathForExistingDocument(this, sProject, sFilename);
        if (System.IO.File.Exists(sFilepath)) {
          System.IO.File.Delete(sFilepath);
        }
      } catch { }
    }
    public string GetLocalizedConfFile(string sProject, string sFilename, string sCultureName) {
      try {
        string sDir = this.GetDataDir(sProject);
        string sFilenameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(sFilename);
        string sExt = System.IO.Path.GetExtension(sFilename);
        string sLocalizedFilename = sFilename;
        foreach (string sLocale in new[] { sCultureName, "en" }) {
          string s = System.IO.Path.Combine(sDir, System.IO.Path.Combine("conf", sFilenameWithoutExt + "_" + sLocale + sExt));
          if (System.IO.File.Exists(s)) {
            sLocalizedFilename = System.IO.Path.GetFileName(s);
          }
        }
        string sFilePath = System.IO.Path.Combine(sDir, System.IO.Path.Combine("conf", sLocalizedFilename));
        if (System.IO.File.Exists(sFilePath)) {
          return System.IO.File.ReadAllText(sFilePath);
        }
      } catch { }
      return "";
    }
    public string GetFilePathForImage(string sProject, string id, bool bOrig) {
      DataService ds = this;
      string sDataDir = ds.GetDataDir(sProject);
      string sFilePath = System.IO.Path.Combine(sDataDir, bOrig ? "images_orig" : "images");
      sFilePath = System.IO.Path.Combine(sFilePath, id);
      return sFilePath;
    }
    public string GetFilePathForGpxFile(string sProject, string id) {
      DataService ds = this;
      string sDataDir = ds.GetDataDir(sProject);
      string sFilePath = System.IO.Path.Combine(sDataDir, "gpx_files");
      sFilePath = System.IO.Path.Combine(sFilePath, id);
      return sFilePath;
    }
    public async Task AddGpsDataToImages(bool bAddNotRemove, SessionData sd, Action<int> callbackCompletion) {
      await Task.Run(() => {
        DataService ds = sd.DS;
        Element[] els = ds.GetElements(sd);
        for (int iEl = 0; iEl < els.Length; iEl++) {
          Element el = els[iEl];
          if (el.HasPhotoData()) {
            try {
              string sFilePath = ds.GetFilePathForImage(sd.CurrentUser.Project, el.ElementName, true);
              using (var image = Image.Load(sFilePath)) {
                if (bAddNotRemove) {
                  // Add GPS.
                  if (el.ElementProp?.MarkerInfo?.position != null && image.Metadata?.ExifProfile != null) {
                    LatLng pos = el.ElementProp.MarkerInfo.position;
                    DateTime dt = el.ElementProp.CreationTime;
                    image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLatitude, Utilities.LatLngRational_from_double(pos.lat));
                    image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLongitude, Utilities.LatLngRational_from_double(pos.lng));
                    image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLatitudeRef, "N");
                    image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLongitudeRef, "E");
                    image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSTimestamp, new[] { new Rational(dt.Hour), new Rational(dt.Minute), new Rational(dt.Second) });
                    image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSDateStamp, dt.ToString("yyyy-MM-dd"));
                    image.Save(sFilePath);
                  }
                } else {
                  // Remove GPS.
                  if (el.ElementProp?.MarkerInfo?.position != null && image.Metadata?.ExifProfile != null) {
                    image.Metadata.ExifProfile.Parts = SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifParts.ExifTags | SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifParts.IfdTags;
                    image.Save(sFilePath);
                  }
                }
              }
            } catch { }
          }
          callbackCompletion(iEl * 100 / els.Length);
        }
      });
    }
    public event EventHandler Initialized {
      add {
        lock (this.lockInitialized) {
          if (this.isInitialized) {
            value.Invoke(this, EventArgs.Empty);
          } else {
            this._Initialized += value;
          }
        }
      }
      remove => this._Initialized -= value;
    }
    private event EventHandler _Initialized;
    private bool isInitialized = false;
    private readonly object lockInitialized = new object();
    //
    public void CreateNewProject(SessionData sd, string sProjectName) {
      try {
        string sDataDir = this.GetDataDir(sProjectName);
        if (!System.IO.Directory.Exists(sDataDir)) {
          System.IO.Directory.CreateDirectory(sDataDir);
          if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) {
            Utilities.Bash("chown www-data:www-data -R " + sDataDir);
          }
          this.SendMail(
            sd,
            "itools.de@posteo.de",
            "Neues BioMap Projekt erzeugt: " + sProjectName,
            "Der Benutzer '" + sd.CurrentUser?.EMail + "' erzeugte das neue Projekt '" + sProjectName + "'.");
        }
      } catch (Exception ex) {
        try {
          this.SendMail(
            sd,
            "itools.de@posteo.de",
            "Exception beim Erzeugen eines neuen BioMap-Projekts: " + sProjectName,
            "Der Benutzer '" + sd.CurrentUser?.EMail + "' versuchte, das neue Projekt '" + sProjectName + "' zu erzeugen. Dabei kam es zu einem Ausnahmefehler." + Environment.NewLine + ex.ToString());
        } catch { }
      }
    }
    public string GetDbFilePath(string sProject) {
      string sDbFilePath = System.IO.Path.Combine(this.GetDataDir(sProject), "biomap.sqlite");
      return sDbFilePath;
    }
    //
    private readonly List<string> AccessedDbs = new List<string>();
    public void OperateOnDb(SessionData sd, Action<IDbCommand> dbAction) {
      this.OperateOnDb(sd.CurrentUser.Project, dbAction);
    }
    public void OperateOnDb(string sProject, Action<IDbCommand> dbAction) {
      try {
        string sDataDir = this.GetDataDir(sProject);
        if (!System.IO.Directory.Exists(sDataDir)) {
          System.IO.Directory.CreateDirectory(sDataDir);
        }
        string sDbFilePath = this.GetDbFilePath(sProject);
        lock (DataService.DbLockObject) {
          var dbConnection = new SQLiteConnection();
          dbConnection.ConnectionString = "Data Source=" + sDbFilePath;
          bool bDbFileExisted = System.IO.File.Exists(sDbFilePath);
          dbConnection.Open();
          if (!this.AccessedDbs.Contains(sProject)) {
            if (!bDbFileExisted) {
              #region Ordner erzeugen.
              foreach (string sFolder in new[] { "conf", "images", "images_orig", "temp" }) {
                try {
                  string sPath = System.IO.Path.Combine(this.GetDataDir(sProject), sFolder);
                  if (!System.IO.Directory.Exists(sPath)) {
                    System.IO.Directory.CreateDirectory(sPath);
                  }
                } catch { }
              }
              #endregion
              using (IDbCommand command = dbConnection.CreateCommand()) {
                #region Ggf. Tabellenstruktur erzeugen.
                command.CommandText = "CREATE TABLE IF NOT EXISTS places (" +
                "name TEXT PRIMARY KEY NOT NULL," +
                "traitvalues TEXT," +
                "radius REAL," +
                "lat REAL," +
                "lng REAL)";
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
                command.CommandText = "CREATE TABLE IF NOT EXISTS elements (" +
                "name TEXT PRIMARY KEY NOT NULL," +
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
                "headbodylength REAL," +
                "dateofbirth DATETIME," +
                "ageyears REAL," +
                "winters INT," +
                "gender TEXT," +
                "iid INT)";
                command.ExecuteNonQuery();
                #endregion
              }
            }
            #region Ggf. neuere Tabellen erzeugen oder erweitern.
            using (IDbCommand command = dbConnection.CreateCommand()) {
              command.CommandText = "CREATE TABLE IF NOT EXISTS project (" +
              "name TEXT PRIMARY KEY NOT NULL," +
              "value TEXT)";
              command.ExecuteNonQuery();
              command.CommandText = "DROP TABLE IF EXISTS userprefs; CREATE TABLE IF NOT EXISTS userprops (" +
              "userid TEXT NOT NULL," +
              "name TEXT NOT NULL," +
              "value TEXT NOT NULL,UNIQUE(userid,name))";
              command.ExecuteNonQuery();
              command.CommandText = "DROP TABLE IF EXISTS monitoringevents; CREATE TABLE IF NOT EXISTS plannedmonitorings (" +
              "place TEXT PRIMARY KEY NOT NULL," +
              "kw INT," +
              "user TEXT," +
              "value TEXT)";
              command.ExecuteNonQuery();
              command.CommandText = "CREATE TABLE IF NOT EXISTS patternimages (" +
              "name TEXT PRIMARY KEY NOT NULL," +
              "measurepoints TEXT," +
              "dataimagesrc TEXT)";
              command.ExecuteNonQuery();
              command.CommandText = "CREATE TABLE IF NOT EXISTS stays (" +
                "lat INTEGER,lon INTEGER,timeslot INTEGER,userid TEXT,stay REAL," +
                "PRIMARY KEY(lat,lon,timeslot,userid))";
              command.ExecuteNonQuery();
              command.CommandText = "CREATE TABLE IF NOT EXISTS gpxfiles (" +
                "filename TEXT,userid TEXT)";
              command.ExecuteNonQuery();
              try {
                command.CommandText = "ALTER TABLE elements ADD COLUMN classification TEXT";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE elements ADD COLUMN measuredata TEXT";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE elements ADD COLUMN croppingconfirmed INT";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE elements ADD COLUMN classname TEXT;";
                command.CommandText += "ALTER TABLE elements ADD COLUMN lbsciname TEXT;";
                command.CommandText += "ALTER TABLE elements ADD COLUMN lbstadium INT;";
                command.CommandText += "ALTER TABLE elements ADD COLUMN lbcount INT;";
                command.CommandText += "ALTER TABLE elements ADD COLUMN habquality INT;";
                command.CommandText += "ALTER TABLE elements ADD COLUMN habmonitoring INT;";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE elements DROP COLUMN category"; // IF EXISTS funzt nicht mit SQLite.
                command.ExecuteNonQuery();
              } catch { }
              foreach (string sTrait in new[] {
                "normcirclepos0x",
                "normcirclepos0y",
                "normcirclepos1x",
                "normcirclepos1y",
                "normcirclepos2x",
                "normcirclepos2y",
                "headposx",
                "headposy",
                "backposx",
                "backposy",
                "origheadposx",
                "origheadposy",
                "origbackposx",
                "origbackposy",
                "traitYellowDominance",
                "traitBlackDominance",
                "traitVertBlackBreastCenterStrip",
                "traitHorizBlackBreastBellyStrip",
                "traitManyIsolatedBlackBellyDots"
              }) {
                try {
                  command.CommandText = $"ALTER TABLE indivdata DROP COLUMN {sTrait}"; // IF EXISTS funzt nicht mit SQLite.
                  command.ExecuteNonQuery();
                } catch { }
              }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN genderfeature TEXT";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN shareofblack REAL";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN centerofmass REAL";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN stddeviation REAL";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN entropy REAL";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN mass REAL";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN captivitybred INT";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN phenotypeidx INT";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE indivdata ADD COLUMN genotypeidx INT";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE places ADD COLUMN monitoringintervalweeks INT";
                command.ExecuteNonQuery();
              } catch { }
              try {
                command.CommandText = "ALTER TABLE places ADD COLUMN caption TEXT";
                command.ExecuteNonQuery();
              } catch { }
            }
            #endregion
            this.AccessedDbs.Add(sProject);
          }
          try {
            using (IDbCommand command = dbConnection.CreateCommand()) {
              dbAction(command);
            }
          } finally {
            dbConnection.Close();
            dbConnection.Dispose();
          }
        }
      } catch { }
    }
    private static readonly object DbLockObject = new object();
    public string[] GetSuperAdmins() {
      var lAdmins = new List<string>();
      try {
        string sFilePathJson = this.GetDataDir("") + "conf/superadmins.json";
        if (System.IO.File.Exists(sFilePathJson)) {
          string sJson = System.IO.File.ReadAllText(sFilePathJson);
          string[] aAdmins = JsonConvert.DeserializeObject<string[]>(sJson);
          lAdmins.AddRange(aAdmins);
        }
      } catch { }
      return lAdmins.ToArray();
    }
    public Task Init() {
      return Task.Run(() => {
        // Create base project if it does not exist.
        this.OperateOnDb("", (command) => {
        });
        //
        lock (this.lockInitialized) {
          this._Initialized?.Invoke(this, EventArgs.Empty);
          this.isInitialized = true;
        }
      });
    }
    public bool IsMigrationInProcess { get; } = false;
    public bool SendMail(SessionData sd, string sTo, string sSubject, string sTextBody) {
      // EMail per REST-API auf Server itools.de versenden.
      try {
        var client = new System.Net.Http.HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        var requestContent = new System.Net.Http.StringContent("{\"toEmail\":\"" + sTo + "\",\"subject\":\"" + sSubject + "\",\"textBody\":\"" + sTextBody + "\"}");
        requestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        bool bSuccess = client.PostAsync("https://api.itools.de/api/SendEMail", requestContent).Wait(4000);
        return bSuccess;
      } catch (Exception ex) {
        this.AddLogEntry("", sd.CurrentUser?.EMail, "SendMail Exception: " + ex.ToString());
      }
      return false;
    }
    public async Task<bool> RequestTAN(SessionData sd, string sFullName) {
      bool bSuccess = false;
      var rng = new Random();
      string sNewTAN = rng.Next(0, 999999999).ToString("000000000");
      string sPermTicket = "";
      int nLevel = 100;
      this.OperateOnDb(sd, async (command) => {
        command.CommandText = "SELECT emailaddr,permticket,level,fullname" +
          " FROM users" +
          "";
        IDataReader dr = command.ExecuteReader();
        int nUserCnt = 0;
        while (dr.Read()) {
          nUserCnt++;
          string sEmailAddr = dr.GetString(0);
          if (string.CompareOrdinal(sEmailAddr, sd.CurrentUser.EMail) == 0) {
            sPermTicket = dr.GetValue(1) as string;
            nLevel = dr.GetInt32(2);
            if (string.IsNullOrEmpty(sFullName)) {
              sFullName = dr.GetString(3);
            }
          }
        }
        dr.Close();
        if (nUserCnt < 1) {
          // First user in new project gets admin level.
          nLevel = 700;
          sd.CurrentProject.Owner = sd.CurrentUser.EMail;
          this.SetProjectProperty(sd, "Owner", sd.CurrentProject.Owner);
          sd.CurrentProject.StartDate = DateTime.Now.Date;
          this.SetProjectProperty(sd, "StartDate", sd.CurrentProject.StartDate.HasValue ? ConvInvar.ToString(sd.CurrentProject.StartDate.Value) : "");
        }
        if (string.IsNullOrEmpty(sPermTicket)) {
          sPermTicket = rng.Next(0, 999999999).ToString("000000000");
        }
        command.CommandText =
          "REPLACE INTO users (emailaddr,tan,permticket,level,fullname) " +
          "VALUES ('" + sd.CurrentUser.EMail + "','" + sNewTAN + "','" + sPermTicket + "'," + ConvInvar.ToString(nLevel) + ",'" + sFullName + "')" +
          "";
        command.ExecuteNonQuery();
        bSuccess = this.SendMail(
          sd,
          sd.CurrentUser.EMail,
          $"BioMap Projekt {sd.CurrentUser.Project} TAN: {sNewTAN}",
          "Geben Sie die TAN " + sNewTAN + " in das TAN-Feld auf der Web-Seite ein und bestätigen Sie es.");
        await Task.Delay(1000);
        this.SendMail(
          sd,
          "itools.de@posteo.de",
          $"Benutzer hat TAN angefordert: {sd.CurrentUser.EMail} für Projekt {sd.CurrentUser.Project}",
          $"Der Benutzer hat aktuell Zugangsstufe {sd.CurrentUser.Level}.");
      });
      return bSuccess;
    }
    public string ConfirmTAN(SessionData sd, string sTAN) {
      string sPermTicket = "";
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT tan,permticket" +
          " FROM users" +
          " WHERE emailaddr='" + sd.CurrentUser.EMail + "'" +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          string sRealTAN = dr.GetString(0);
          if (string.CompareOrdinal(sTAN, sRealTAN) == 0) {
            sPermTicket = dr.GetString(1);
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
    public int GetUserLevel(SessionData sd, string sPermTicket) {
      int nLevel = 0;
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT level" +
          " FROM users" +
          " WHERE emailaddr='" + sd.CurrentUser.EMail + "' AND permticket='" + sPermTicket + "'" +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          nLevel = dr.GetInt32(0);
          break;
        }
        dr.Close();
      });
      return nLevel;
    }
    public void LoadUser(SessionData sd, string sPermTicket, User user) {
      int nLevel = 0;
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT permticket,level,fullname" +
          " FROM users" +
          " WHERE emailaddr='" + sd.CurrentUser.EMail + "'" +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          string sRealPermTicket = dr.GetValue(0) as string;
          if (string.CompareOrdinal(sPermTicket, sRealPermTicket) == 0) {
            nLevel = dr.GetInt32(1);
          }
          user.FullName = dr.GetString(2);
          break;
        }
        dr.Close();
      });
      user.Level = nLevel;
      user.EMail = sd.CurrentUser.EMail;
      user.PermTicket = sPermTicket;
      this.LoadProject(sd, sd.CurrentProject);
      user.Prefs.MaptypeId = this.GetUserProperty(sd, "MaptypeId", "");
      user.Prefs.ShowCustomMap = (ConvInvar.ToInt(this.GetUserProperty(sd, "ShowCustomMap", "0")) != 0);
      user.Prefs.ShowPlaces = 0;//ConvInvar.ToInt(this.GetUserProperty(sd, "ShowPlaces", "1"));
      user.Prefs.DynaZoomed = (ConvInvar.ToInt(this.GetUserProperty(sd, "DynaZoomed", "0")) != 0);
      user.Prefs.DisplayConnectors = ConvInvar.ToInt(this.GetUserProperty(sd, "DisplayConnectors", "0"));
    }
    #region Users.
    public User[] GetAllUsers(SessionData sd) {
      var lUsers = new List<User>();
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT emailaddr,level,fullname" +
          " FROM users" +
          " ORDER BY emailaddr" +
          "";
        IDataReader dr = command.ExecuteReader();
        try {
          while (dr.Read()) {
            var user = new User {
              EMail = dr.GetString(0),
              Level = dr.GetInt32(1),
              FullName = dr.GetString(2),
            };
            lUsers.Add(user);
          }
        } finally {
          dr.Close();
        }
      });
      return lUsers.ToArray();
    }
    public void WriteUser(SessionData sd, User user) {
      //System.Diagnostics.Debug.WriteLine($"WriteUser({user.EMail},Level={user.Level})");
      if (user.Level >= 1) {
        this.OperateOnDb(sd, (command) => {
          command.CommandText =
            "UPDATE users SET " +
            "level='" + ConvInvar.ToString(user.Level) + "'," +
            "level='" + ConvInvar.ToString(user.Level) + "'," +
            "fullname='" + user.FullName + "' WHERE emailaddr='" + user.EMail + "'";
          command.ExecuteNonQuery();
        });
        this.SetUserProperty(sd, "MaptypeId", user.Prefs.MaptypeId);
        this.SetUserProperty(sd, "ShowCustomMap", user.Prefs.ShowCustomMap ? "1" : "0");
        this.SetUserProperty(sd, "ShowPlaces", ConvInvar.ToString(user.Prefs.ShowPlaces));
        this.SetUserProperty(sd, "DynaZoomed", user.Prefs.DynaZoomed ? "1" : "0");
        this.SetUserProperty(sd, "DisplayConnectors", ConvInvar.ToString(user.Prefs.DisplayConnectors));
      }
    }
    public string GetUserProperty(SessionData sd, string sPropertyName, string sDefaultValue = "") {
      string sValue = sDefaultValue;
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT value FROM userprops WHERE name='" + sPropertyName + "' AND userid='" + sd.CurrentUser.EMail + "'";
        object r = command.ExecuteScalar();
        sValue = command.ExecuteScalar() as string;
      });
      return sValue;
    }
    public void SetUserProperty(SessionData sd, string sPropertyName, string sValue) {
      string sOldValue = this.GetProjectProperty(sd, sPropertyName);
      string[] saDiff = Utilities.FindDifferingCoreParts(sOldValue, sValue);
      if (saDiff != null) {
        this.OperateOnDb(sd, (command) => {
          command.CommandText = "REPLACE INTO userprops (userid,name,value) VALUES ('" + sd.CurrentUser.EMail + "','" + sPropertyName + "','" + sValue + "')";
          command.ExecuteNonQuery();
        });
        //this.AddLogEntry(sd,"User preference \""+sPropertyName+"\" changed: "+saDiff[0]+" --> "+saDiff[1]);
      }
    }
    #endregion
    #region Project
    public string GetProjectProperty(SessionData sd, string sPropertyName, string sDefaultValue = "") {
      return this.GetProjectProperty(sd.CurrentUser.Project, sPropertyName, sDefaultValue);
    }
    public string GetProjectProperty(string sProject, string sPropertyName, string sDefaultValue = "") {
      string sValue = sDefaultValue;
      this.OperateOnDb(sProject, (command) => {
        command.CommandText = "SELECT value FROM project WHERE name='" + sPropertyName + "'";
        object r = command.ExecuteScalar();
        string sRawValue = command.ExecuteScalar() as string;
        if (!string.IsNullOrEmpty(sRawValue)) {
          sValue = sRawValue;
        }
      });
      return sValue;
    }
    public void SetProjectProperty(SessionData sd, string sPropertyName, string sValue) {
      string sOldValue = this.GetProjectProperty(sd, sPropertyName);
      string[] saDiff = Utilities.FindDifferingCoreParts(sOldValue, sValue);
      if (saDiff != null) {
        this.OperateOnDb(sd, (command) => {
          command.CommandText = "REPLACE INTO project (name,value) VALUES ('" + sPropertyName + "','" + sValue + "')";
          command.ExecuteNonQuery();
        });
        this.AddLogEntry(sd, "Project property \"" + sPropertyName + "\" changed: " + saDiff[0] + " --> " + saDiff[1]);
      }
    }
    public void LoadProject(SessionData sd, Project project) {
      project.Owner = this.GetProjectProperty(sd, "Owner");
      {
        if (!DateTime.TryParse(this.GetProjectProperty(sd, "StartDate", ""), out DateTime dt)) {
          dt = new DateTime(2019, 4, 1);
        }
        project.StartDate = dt;
      }
      project.MaxAllowedElements = ConvInvar.ToInt(this.GetProjectProperty(sd, "MaxAllowedElements", "20"));
      project.InitialHistoryYears = ConvInvar.ToInt(this.GetProjectProperty(sd, "InitialHistoryYears", "0"));
      project.SaveGpsDataInOriginalImages = ConvInvar.ToInt(this.GetProjectProperty(sd, "SaveGpsDataInOriginalImages", "0")) != 0;
      project.AoiCenterLat = ConvInvar.ToDouble(this.GetProjectProperty(sd, "AoiCenterLat"));
      project.AoiCenterLng = ConvInvar.ToDouble(this.GetProjectProperty(sd, "AoiCenterLng"));
      project.AoiMinLat = ConvInvar.ToDouble(this.GetProjectProperty(sd, "AoiMinLat"));
      project.AoiMinLng = ConvInvar.ToDouble(this.GetProjectProperty(sd, "AoiMinLng"));
      project.AoiMaxLat = ConvInvar.ToDouble(this.GetProjectProperty(sd, "AoiMaxLat"));
      project.AoiMaxLng = ConvInvar.ToDouble(this.GetProjectProperty(sd, "AoiMaxLng"));
      project.AoiTolerance = ConvInvar.ToDouble(this.GetProjectProperty(sd, "AoiTolerance"));
      project.SpeciesSciName = this.GetProjectProperty(sd, "SpeciesSciName");
      project.Phenotypes = this.GetProjectProperty(sd, "Phenotypes", "");
      project.Genotypes = this.GetProjectProperty(sd, "Genotypes", "");
      project.MaleGenderFeatures = this.GetProjectProperty(sd, "MaleGenderFeatures", "1") != "0";
      project.FemaleGenderFeatures = this.GetProjectProperty(sd, "FemaleGenderFeatures", "0") != "0";
      project.DisplayCaptivityBred = this.GetProjectProperty(sd, "DisplayCaptivityBred", "0") != "0";
      project.DisplayMass = this.GetProjectProperty(sd, "DisplayMass", "0") != "0";
      project.DisplayIdPhotosZoomed = this.GetProjectProperty(sd, "DisplayIdPhotosZoomed", "0") != "0";
      project.AdultMinLength = ConvInvar.ToDouble(this.GetProjectProperty(sd, "AdultMinLength", "36"));
      project.MinHeadBodyLength = ConvInvar.ToDouble(this.GetProjectProperty(sd, "MinHeadBodyLength", "12"));
      project.MaxHeadBodyLength = ConvInvar.ToDouble(this.GetProjectProperty(sd, "MaxHeadBodyLength", "60"));
      project.HideUpToBodyLength = ConvInvar.ToDouble(this.GetProjectProperty(sd, "HideUpToBodyLength", "0"));
      project.ImageNormalizer = new Blazor.ImageSurveyor.ImageSurveyorNormalizer(this.GetProjectProperty(sd, "NormalizeMethod", "HeadToCloakInPetriDish")) {
        NormalizeReference = ConvInvar.ToDouble(this.GetProjectProperty(sd, "NormalizeReference", "100")),
        NormalizePixelSize = ConvInvar.ToDouble(this.GetProjectProperty(sd, "NormalizePixelSize", "0.10")),
        NormalizedWidthPx = ConvInvar.ToInt(this.GetProjectProperty(sd, "NormalizedWidthPx", "600")),
        NormalizedHeightPx = ConvInvar.ToInt(this.GetProjectProperty(sd, "NormalizedHeightPx", "600")),
        PatternRelWidth = ConvInvar.ToFloat(this.GetProjectProperty(sd, "PatternRelWidth", "0.30")),
        PatternRelHeight = ConvInvar.ToFloat(this.GetProjectProperty(sd, "PatternRelHeight", "0.80")),
      };
      project.Identification.LocationWeight = ConvInvar.ToDouble(this.GetProjectProperty(sd, "Identification.LocationWeight", "3"));
      project.Identification.GenderWeight = ConvInvar.ToDouble(this.GetProjectProperty(sd, "Identification.GenderWeight", "1"));
      project.Identification.LengthWeight = ConvInvar.ToDouble(this.GetProjectProperty(sd, "Identification.LengthWeight", "3"));
      project.Identification.TraitsWeight = ConvInvar.ToDouble(this.GetProjectProperty(sd, "Identification.TraitsWeight", "1.5"));
      project.Identification.PatternMatchingWeight = ConvInvar.ToDouble(this.GetProjectProperty(sd, "Identification.PatternMatchingWeight", "0"));
      project.MinLevelToSeeElements = ConvInvar.ToInt(this.GetProjectProperty(sd, "MinLevelToSeeElements", "200"));
      project.MinLevelToSeeExactLocations = ConvInvar.ToInt(this.GetProjectProperty(sd, "MinLevelToSeeExactLocations", "400"));
      //
      {
        string sJson = this.GetProjectProperty(sd, "Taxa", "");
        if (string.IsNullOrEmpty(sJson)) {
          // No taxa save in project --> initialise.
          project.InitTaxaForYellowBelliedToad();
          this.WriteProject(sd, project);
        } else {
          IEnumerable<Taxon> taxaList = JsonConvert.DeserializeObject<IEnumerable<Taxon>>(sJson);
          project.TaxaTree.FromTaxaList(taxaList);
        }
      }
      //
      GrowthFunc.HatchSize = project.MinHeadBodyLength;
      GrowthFunc.FullSize = project.MaxHeadBodyLength;
      //
      sd.OnCurrentProjectLoaded();
    }
    public void WriteProject(SessionData sd, Project project) {
      this.SetProjectProperty(sd, "StartDate", sd.CurrentProject.StartDate.HasValue ? ConvInvar.ToString(sd.CurrentProject.StartDate.Value) : "");
      this.SetProjectProperty(sd, "MaxAllowedElements", ConvInvar.ToString(project.MaxAllowedElements));
      this.SetProjectProperty(sd, "InitialHistoryYears", ConvInvar.ToString(project.InitialHistoryYears));
      this.SetProjectProperty(sd, "SaveGpsDataInOriginalImages", ConvInvar.ToString(project.SaveGpsDataInOriginalImages ? 1 : 0));
      this.SetProjectProperty(sd, "AoiCenterLat", ConvInvar.ToString(project.AoiCenterLat));
      this.SetProjectProperty(sd, "AoiCenterLng", ConvInvar.ToString(project.AoiCenterLng));
      this.SetProjectProperty(sd, "AoiMinLat", ConvInvar.ToString(project.AoiMinLat));
      this.SetProjectProperty(sd, "AoiMinLng", ConvInvar.ToString(project.AoiMinLng));
      this.SetProjectProperty(sd, "AoiMaxLat", ConvInvar.ToString(project.AoiMaxLat));
      this.SetProjectProperty(sd, "AoiMaxLng", ConvInvar.ToString(project.AoiMaxLng));
      this.SetProjectProperty(sd, "AoiTolerance", ConvInvar.ToString(project.AoiTolerance));
      this.SetProjectProperty(sd, "SpeciesSciName", project.SpeciesSciName);
      this.SetProjectProperty(sd, "Phenotypes", project.Phenotypes);
      this.SetProjectProperty(sd, "Genotypes", project.Genotypes);
      this.SetProjectProperty(sd, "MaleGenderFeatures", project.MaleGenderFeatures ? "1" : "0");
      this.SetProjectProperty(sd, "FemaleGenderFeatures", project.FemaleGenderFeatures ? "1" : "0");
      this.SetProjectProperty(sd, "DisplayCaptivityBred", project.DisplayCaptivityBred ? "1" : "0");
      this.SetProjectProperty(sd, "DisplayMass", project.DisplayMass ? "1" : "0");
      this.SetProjectProperty(sd, "DisplayIdPhotosZoomed", project.DisplayIdPhotosZoomed ? "1" : "0");
      this.SetProjectProperty(sd, "AdultMinLength", ConvInvar.ToString(project.AdultMinLength));
      this.SetProjectProperty(sd, "MinHeadBodyLength", ConvInvar.ToString(project.MinHeadBodyLength));
      this.SetProjectProperty(sd, "MaxHeadBodyLength", ConvInvar.ToString(project.MaxHeadBodyLength));
      this.SetProjectProperty(sd, "HideUpToBodyLength", ConvInvar.ToString(project.HideUpToBodyLength));
      this.SetProjectProperty(sd, "NormalizeMethod", project.ImageNormalizer.NormalizeMethod);
      this.SetProjectProperty(sd, "NormalizeReference", ConvInvar.ToString(project.ImageNormalizer.NormalizeReference));
      this.SetProjectProperty(sd, "NormalizePixelSize", ConvInvar.ToString(project.ImageNormalizer.NormalizePixelSize));
      this.SetProjectProperty(sd, "NormalizedWidthPx", ConvInvar.ToString(project.ImageNormalizer.NormalizedWidthPx));
      this.SetProjectProperty(sd, "NormalizedHeightPx", ConvInvar.ToString(project.ImageNormalizer.NormalizedHeightPx));
      this.SetProjectProperty(sd, "Identification.LocationWeight", ConvInvar.ToString(project.Identification.LocationWeight));
      this.SetProjectProperty(sd, "Identification.GenderWeight", ConvInvar.ToString(project.Identification.GenderWeight));
      this.SetProjectProperty(sd, "Identification.LengthWeight", ConvInvar.ToString(project.Identification.LengthWeight));
      this.SetProjectProperty(sd, "Identification.TraitsWeight", ConvInvar.ToString(project.Identification.TraitsWeight));
      this.SetProjectProperty(sd, "Identification.PatternMatchingWeight", ConvInvar.ToString(project.Identification.PatternMatchingWeight));
      this.SetProjectProperty(sd, "PatternRelWidth", ConvInvar.ToString(project.ImageNormalizer.PatternRelWidth));
      this.SetProjectProperty(sd, "PatternRelHeight", ConvInvar.ToString(project.ImageNormalizer.PatternRelHeight));
      this.SetProjectProperty(sd, "MinLevelToSeeElements", ConvInvar.ToString(project.MinLevelToSeeElements));
      this.SetProjectProperty(sd, "MinLevelToSeeExactLocations", ConvInvar.ToString(project.MinLevelToSeeExactLocations));
      this.SetProjectProperty(sd, "Taxa", project.TaxaTree.ToJSON());
    }
    public IEnumerable<GoogleMapsComponents.Maps.LatLngLiteral> GetAoi(SessionData sd) {
      string sJson = this.GetProjectProperty(sd, "aoi");
      // If DB has no value, try to read it from conf/aoi.json.
      if (string.IsNullOrEmpty(sJson)) {
        string sFilePath = this.GetDataDir(sd) + "conf/aoi.json";
        if (System.IO.File.Exists(sFilePath)) {
          sJson = System.IO.File.ReadAllText(sFilePath);
        }
      }
      if (!string.IsNullOrEmpty(sJson)) {
        try {
          GoogleMapsComponents.Maps.LatLngLiteral[] vertices = JsonConvert.DeserializeObject<GoogleMapsComponents.Maps.LatLngLiteral[]>(sJson);
          var path = new List<GoogleMapsComponents.Maps.LatLngLiteral>(vertices);
          return path;
        } catch { }
      }
      return null;
    }
    public void WriteAoi(SessionData sd, IEnumerable<GoogleMapsComponents.Maps.LatLngLiteral> path) {
      string sJson = "";
      if (path != null) {
        sJson = JsonConvert.SerializeObject(path);
      }
      this.SetProjectProperty(sd, "aoi", sJson);
      //
      if (path != null && path.Any()) {
        double? AoiCenterLat = null;
        double? AoiCenterLng = null;
        double? AoiMinLat = null;
        double? AoiMinLng = null;
        double? AoiMaxLat = null;
        double? AoiMaxLng = null;
        foreach (GoogleMapsComponents.Maps.LatLngLiteral latLng in path) {
          if (AoiCenterLat.HasValue)
            AoiCenterLat += latLng.Lat;
          else
            AoiCenterLat = latLng.Lat;
          if (AoiCenterLng.HasValue)
            AoiCenterLng += latLng.Lng;
          else
            AoiCenterLng = latLng.Lng;
          if (!AoiMinLat.HasValue || latLng.Lat < AoiMinLat.Value)
            AoiMinLat = latLng.Lat;
          if (!AoiMinLng.HasValue || latLng.Lng < AoiMinLng.Value)
            AoiMinLng = latLng.Lng;
          if (!AoiMaxLat.HasValue || latLng.Lat > AoiMaxLat.Value)
            AoiMaxLat = latLng.Lat;
          if (!AoiMaxLng.HasValue || latLng.Lng > AoiMaxLng.Value)
            AoiMaxLng = latLng.Lng;
        }
        sd.CurrentProject.AoiCenterLat = AoiCenterLat.Value / path.Count();
        sd.CurrentProject.AoiCenterLng = AoiCenterLng.Value / path.Count();
        sd.CurrentProject.AoiMinLat = AoiMinLat.Value;
        sd.CurrentProject.AoiMinLng = AoiMinLng.Value;
        sd.CurrentProject.AoiMaxLat = AoiMaxLat.Value;
        sd.CurrentProject.AoiMaxLng = AoiMaxLng.Value;
        this.WriteProject(sd, sd.CurrentProject);
      }
    }
    #endregion
    #region Length of stay from GXP data.
    public bool TryAddGpxFilename(SessionData sd, string sGpxFilename) {
      bool bAdded = false;
      this.OperateOnDb(sd, (command) => {
        string sUserId = sd.CurrentUser.EMail;
        command.CommandText = $"SELECT filename FROM gpxfiles WHERE (userid='{sUserId}' AND filename='{sGpxFilename}')";
        IDataReader dr = command.ExecuteReader();
        if (!dr.Read()) {
          bAdded = true;
        }
        dr.Close();
        if (bAdded) {
          command.CommandText = $"INSERT INTO gpxfiles (filename,userid) VALUES('{sGpxFilename}','{sUserId}')";
        }
        command.ExecuteNonQuery();
      });
      return bAdded;
    }
    public void AddLengthOfStay(SessionData sd, int nLat, int nLon, int nTimeSlot, double staySeconds) {
      this.OperateOnDb(sd, (command) => {
        string sUserId = sd.CurrentUser.EMail;
        string sLat = ConvInvar.ToString(nLat);
        string sLon = ConvInvar.ToString(nLon);
        command.CommandText = $"SELECT SUM(stay) FROM stays WHERE (userid='{sUserId}' AND lat={sLat} AND lon={sLon} AND timeslot={nTimeSlot})";
        IDataReader dr = command.ExecuteReader();
        double? stayPrev = null;
        if (dr.Read()) {
          if (!dr.IsDBNull(0)) {
            stayPrev = dr.GetDouble(0);
          }
        }
        dr.Close();
        if (stayPrev.HasValue) {
          command.CommandText = $"UPDATE stays SET stay={ConvInvar.ToString(stayPrev.Value + staySeconds)} WHERE (userid='{sUserId}' AND lat={sLat} AND lon={sLon} AND timeslot={nTimeSlot})";
        } else {
          command.CommandText = $"INSERT INTO stays (lat,lon,timeslot,userid,stay) VALUES({sLat},{sLon},{nTimeSlot},'{sUserId}',{ConvInvar.ToString(staySeconds)})";
        }
        command.ExecuteNonQuery();
      });
    }
    public LengthOfStayEvent[] GetLengthOfStayEvents(SessionData sd, string sUserId = null, int? nTimeSlotBegin = null, int? nTimeSlotEnd = null) {
      var lEvents = new List<LengthOfStayEvent>();
      var project = sd.CurrentProject;
      string sWhereConditions= "";
      if (!string.IsNullOrEmpty(sUserId)) {
        sWhereConditions = Filters.AddToWhereClause(sWhereConditions, $"userid='{sUserId}'");
      }
      if (nTimeSlotBegin.HasValue) {
        sWhereConditions = Filters.AddToWhereClause(sWhereConditions, $"timeslot>={ConvInvar.ToString(nTimeSlotBegin.Value)}");
      }
      if (nTimeSlotEnd.HasValue) {
        sWhereConditions = Filters.AddToWhereClause(sWhereConditions, $"timeslot<={ConvInvar.ToString(nTimeSlotEnd.Value)}");
      }
      string sWhereClause = string.IsNullOrEmpty(sWhereConditions) ? "" : $" WHERE ({sWhereConditions})";
      this.OperateOnDb(sd, (command) => {
        command.CommandText = $"SELECT lat,lon,stay FROM stays{sWhereClause}";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          lEvents.Add(new LengthOfStayEvent { StayLat = dr.GetInt32(0), StayLon = dr.GetInt32(1), LengthOfStay = dr.GetDouble(2) });
        }
        dr.Close();
      });
      return lEvents.ToArray();
    }
    #endregion
    #region Places.
    private static System.Numerics.Matrix3x2 GetAlienationTransformation(SessionData sd) {
      if (!_AlienationTransformation.ContainsKey(sd.CurrentUser.Project)) {
        var m1 = System.Numerics.Matrix3x2.CreateTranslation((float)(-sd.CurrentProject.AoiCenterLng), (float)(-sd.CurrentProject.AoiCenterLat));
        var m2 = System.Numerics.Matrix3x2.CreateRotation(-0.33f);
        var m2a = System.Numerics.Matrix3x2.CreateSkew(0f, 0.25f);
        var m2b = System.Numerics.Matrix3x2.CreateTranslation(-0.0005f, -0.0013f);
        var m3 = System.Numerics.Matrix3x2.CreateScale(0.99f, 0.95f);
        System.Numerics.Matrix3x2.Invert(m1, out System.Numerics.Matrix3x2 m4);
        _AlienationTransformation.Add(sd.CurrentUser.Project, m1 * m2 * m2a * m2b * m3 * m4);
      }
      return _AlienationTransformation[sd.CurrentUser.Project];
    }
    private static readonly Dictionary<string, System.Numerics.Matrix3x2> _AlienationTransformation = new Dictionary<string, System.Numerics.Matrix3x2>();
    private static LatLng GetAlienatedPosition(SessionData sd, LatLng position) {
      var vRes = System.Numerics.Vector2.Transform(new System.Numerics.Vector2((float)position.lng, (float)position.lat), GetAlienationTransformation(sd));
      return new LatLng {
        lat = vRes.Y,
        lng = vRes.X,
      };
    }
    /// <summary>
    /// Orte zurückliefern; je nach Zugriffsrecht werden die Orte exakt oder verfremdet zurückgeliefert.
    /// </summary>
    /// <param name="sd">
    /// SessionData.
    /// </param>
    /// <param name="sWhereClause"></param>
    /// <param name="bIncludeNoPlace"></param>
    /// <returns></returns>
    public Place[] GetPlaces(SessionData sd, string sWhereClause = null, bool bIncludeNoPlace = false) {
      var result = new List<Place>();
      if (bIncludeNoPlace) {
        result.Add(Place.Nowhere);
      }
      if (this.IsMigrationInProcess || (sd != null && sd.MaySeeRealLocations)) {
        result.AddRange(this.GetRealPlaces(sd, sWhereClause));
      } else {
        var lAlienatedPlaces = new List<Place>();
        foreach (Place place in this.GetRealPlaces(sd, sWhereClause)) {
          var ap = new Place {
            Name = place.Name,
            Radius = place.Radius,
            LatLng = GetAlienatedPosition(sd, place.LatLng),
          };
          ap.TraitValues.Clear();
          ap.TraitValues.AddRange(place.TraitValues);
          lAlienatedPlaces.Add(ap);
        }
        result.AddRange(lAlienatedPlaces);
      }
      return result.ToArray();
    }
    public Place GetPlaceByName(SessionData sd, string sPlaceName) {
      Place[] aPlaces = this.GetPlaces(sd, "name='" + sPlaceName + "'");
      if (aPlaces.Length < 1) {
        return null;
      } else {
        return aPlaces[0];
      }
    }
    private Place[] GetRealPlaces(SessionData sd, string sWhereClause = null) {
      var lPlaces = new List<Place>();
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT name,radius,lat,lng,monitoringintervalweeks,traitvalues,caption" +
          " FROM places" +
          ((sWhereClause == null) ? "" : (" WHERE (" + sWhereClause + ")")) +
          " ORDER BY name" +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          var place = new Place {
            Name = dr.GetString(0),
            Radius = dr.GetDouble(1),
            LatLng = new LatLng {
              lat = dr.GetDouble(2),
              lng = dr.GetDouble(3),
            },
            MonitoringIntervalWeeks = dr.IsDBNull(4) ? 4 : dr.GetInt32(4),
            Caption = dr.IsDBNull(6) ? "" : dr.GetString(6),
          };
          string sTraitsJson = dr.GetValue(5) as string;
          if (!string.IsNullOrEmpty(sTraitsJson)) {
            int[] naTraitValues = JsonConvert.DeserializeObject<int[]>(sTraitsJson);
            for (int i = 0; i < Math.Min(naTraitValues.Length, place.TraitValues.Count); i++) {
              place.TraitValues[i] = naTraitValues[i];
            }
          }
          lPlaces.Add(place);
        }
        dr.Close();
      });
      return lPlaces.ToArray();
    }
    public void CreatePlace(SessionData sd, Place place) {
      this.OperateOnDb(sd, (command) => {
        string sTraitJson = JsonConvert.SerializeObject(place.TraitValues);
        command.CommandText =
          "REPLACE INTO places (name,radius,lat,lng,monitoringintervalweeks,traitvalues,caption)" +
          "VALUES ('" + place.Name + "'," +
          "'" + ConvInvar.ToString(place.Radius) +
          "','" + ConvInvar.ToString(place.LatLng.lat) +
          "','" + ConvInvar.ToString(place.LatLng.lng) +
          "','" + ConvInvar.ToString(place.MonitoringIntervalWeeks) +
          "','" + sTraitJson +
          "','" + place.Caption +
          "')";
        command.ExecuteNonQuery();
      });
      this.AddLogEntry(sd, "Created place " + place.Name + ": " + JsonConvert.SerializeObject(place));
    }
    public void DeletePlace(SessionData sd, string sPlaceName) {
      this.OperateOnDb(sd, (command) => {
        command.CommandText =
          "DELETE FROM places WHERE name='" + sPlaceName + "'";
        command.ExecuteNonQuery();
      });
      this.AddLogEntry(sd, "Deleted place " + sPlaceName);
    }
    public void DeleteAllMyGpxFiles(SessionData sd) {
      this.OperateOnDb(sd, (command) => {
        command.CommandText =
          $"DELETE FROM stays WHERE userid='{sd.CurrentUser.EMail}'";
        command.ExecuteNonQuery();
        command.CommandText =
          $"DELETE FROM gpxfiles WHERE userid='{sd.CurrentUser.EMail}'";
        command.ExecuteNonQuery();
      });
      this.AddLogEntry(sd, $"Deleted all GPX data for user '{sd.CurrentUser.EMail}'");
    }
    public void WritePlace(SessionData sd, Place place) {
      string[] saDiff = null;
      {
        Place prevPlace = this.GetPlaceByName(sd, place.Name);
        if (prevPlace != null) {
          string prevJson = JsonConvert.SerializeObject(prevPlace);
          string actJson = JsonConvert.SerializeObject(place);
          saDiff = Utilities.FindDifferingCoreParts(prevJson, actJson);
        }
      }
      if (saDiff != null) {
        this.OperateOnDb(sd, (command) => {
          string sTraitJson = JsonConvert.SerializeObject(place.TraitValues);
          command.CommandText =
            "UPDATE places SET " +
            "radius='" + ConvInvar.ToString(place.Radius) + "'," +
            "lat='" + ConvInvar.ToString(place.LatLng.lat) + "'," +
            "lng='" + ConvInvar.ToString(place.LatLng.lng) + "'," +
            "monitoringintervalweeks='" + ConvInvar.ToString(place.MonitoringIntervalWeeks) + "'," +
            "caption='" + place.Caption + "'," +
            "traitvalues='" + sTraitJson + "' WHERE name='" + place.Name + "'";
          command.ExecuteNonQuery();
        });
        this.AddLogEntry(sd, "Changed place " + place.Name + ": " + saDiff[0] + " --> " + saDiff[1]);
      }
    }
    public void RenamePlace(SessionData sd, Place place, string sNewName) {
      string sOldName = place.Name;
      if (string.CompareOrdinal(sNewName, sOldName) != 0) {
        this.OperateOnDb(sd, (command) => {
          command.CommandText =
            "UPDATE places SET " +
            "name='" + sNewName + "' WHERE name='" + place.Name + "'";
          command.ExecuteNonQuery();
        });
        place.Name = sNewName;
        this.AddLogEntry(sd, "Changed place name " + sOldName + " --> " + sNewName);
      }
    }
    #endregion
    #region Monitoring.
    public (int, string, string) GetPlannedMonitoring(SessionData sd, string sPlaceName) {
      int kw = -1;
      string user = null;
      string value = null;
      this.OperateOnDb(sd, (command) => {
        command.CommandText =
            "SELECT kw,user,value FROM plannedmonitorings WHERE place='" + sPlaceName + "'";
        IDataReader dr = command.ExecuteReader();
        try {
          while (dr.Read()) {
            kw = dr.GetInt32(0);
            user = dr.GetString(1);
            value = dr.GetString(2);
            break;
          }
        } finally {
          dr.Close();
        }
      });
      return (kw, user, value);
    }
    public void SetPlannedMonitoring(SessionData sd, string sPlaceName, int kw, string user, string value) {
      this.OperateOnDb(sd, (command) => {
        command.CommandText =
          "REPLACE INTO plannedmonitorings (place,kw,user,value) " +
          $"VALUES ('{sPlaceName}','{kw}','{user}','{value}')";
        command.ExecuteNonQuery();
      });
      this.AddLogEntry(sd, $"SetPlannedMonitoring({sPlaceName},{kw},{user},{value})");
    }
    #endregion
    public void WriteElement(SessionData sd, Element el) {
      this.WriteElement(sd.CurrentUser.Project, el);
    }
    public void WriteElement(string sProject, Element el) {
      this.OperateOnDb(sProject, (command) => {
        string sJsonClassification = JsonConvert.SerializeObject(el.Classification);
        string sJsonMeasureData = JsonConvert.SerializeObject(el.MeasureData);
        command.CommandText =
            "REPLACE INTO elements (name," +
            "classification,classname,habquality,habmonitoring,lbsciname,lbstadium,lbcount,croppingconfirmed,measuredata,markerposlat,markerposlng,place,comment,uploadtime,uploader,creationtime) " +
            "VALUES ('" + el.ElementName +
            "','" +
            "','" + el.Classification.ClassName +
            "','" + ConvInvar.ToString((el.Classification.Habitat != null) ? el.Classification.Habitat.Quality : 0) +
            "','" + ((el.Classification.Habitat != null && el.Classification.Habitat.Monitoring) ? "1" : "0") +
            "','" + ((el.Classification.LivingBeing?.Taxon != null) ? el.Classification.LivingBeing.Taxon.SciName : "") +
            "','" + ConvInvar.ToString((el.Classification.LivingBeing != null) ? ((int)el.Classification.LivingBeing.Stadium) : 0) +
            "','" + ConvInvar.ToString((el.Classification.LivingBeing != null) ? el.Classification.LivingBeing.Count : 0) +
            "','" + (el.CroppingConfirmed ? "1" : "0") +
            "','" + sJsonMeasureData +
            "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.position.lat) +
            "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.position.lng) +
            "','" + el.ElementProp.MarkerInfo.PlaceName +
            "','" + el.ElementProp.UploadInfo.Comment +
            "','" + ConvInvar.ToString(el.ElementProp.UploadInfo.Timestamp) +
            "','" + el.ElementProp.UploadInfo.UserId +
            "','" + ConvInvar.ToString(el.ElementProp.CreationTime) +
            "')";
        command.ExecuteNonQuery();
        if (el.ElementProp.ExifData != null) {
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
        if (el.HasIndivData() && el.ElementProp?.IndivData?.MeasuredData != null) {
          command.CommandText =
            "REPLACE INTO indivdata (name" +
            ",headbodylength" +
            ",shareofblack" +
            ",centerofmass" +
            ",stddeviation" +
            ",entropy" +
            ",mass" +
            ",dateofbirth" +
            ",ageyears" +
            ",winters" +
            ",genderfeature" +
            ",gender" +
            ",captivitybred" +
            ",phenotypeidx" +
            ",genotypeidx" +
            ",iid" +
            ") VALUES ('" + el.ElementName + "'" +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.HeadBodyLength) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.ShareOfBlack) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.CenterOfMass) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.StdDeviation) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.Entropy) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.MeasuredData.Mass) +
            ",'" + ConvInvar.ToString(el.ElementProp.IndivData.DateOfBirth) + "'" +
            "," + ConvInvar.ToString(el.GetAgeYears()) +
            "," + ConvInvar.ToString(el.GetWinters()) +
            ",'" + el.ElementProp.IndivData.GenderFeature + "'" +
            ",'" + el.ElementProp.IndivData.Gender + "'" +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.CaptivityBred) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.PhenotypeIdx) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.GenotypeIdx) +
            "," + ConvInvar.ToString(el.ElementProp.IndivData.IId) +
            ")";
          command.ExecuteNonQuery();
        } else if (!el.HasIndivData()) {
          command.CommandText =
            "DELETE FROM indivdata WHERE (name='" + el.ElementName + "')";
          command.ExecuteNonQuery();
        }
      });
    }
    public void DeleteElement(SessionData sd, Element el) {
      this.OperateOnDb(sd, (command) => {
        command.CommandText =
          "DELETE FROM elements WHERE (name='" + el.ElementName + "')";
        command.ExecuteNonQuery();
        command.CommandText =
          "DELETE FROM photos WHERE (name='" + el.ElementName + "')";
        command.ExecuteNonQuery();
        command.CommandText =
          "DELETE FROM indivdata WHERE (name='" + el.ElementName + "')";
        command.ExecuteNonQuery();
      });
      foreach (string sFolder in new[] { "images", "images_orig" }) {
        try {
          string sFilePath = System.IO.Path.Combine(this.GetDataDir(sd), System.IO.Path.Combine(sFolder, el.ElementName));
          if (System.IO.File.Exists(sFilePath)) {
            System.IO.File.Delete(sFilePath);
          }
        } catch { }
      }
    }
    public int GetElementCount(SessionData sd) {
      int nElementCount = 0;
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT COUNT(*) FROM elements";
        nElementCount = Convert.ToInt32(command.ExecuteScalar());
      });
      return nElementCount;
    }
    public async Task<Element[]> GetElementsAsync(SessionData sd, Filters filters = null, string sSqlCondition = "", string sSqlOrderBy = "elements.creationtime") {
      return await Task<Element[]>.Run(() => this.GetElements(sd, filters, sSqlCondition, sSqlOrderBy));
    }
    public Element[] GetElements(SessionData sd, Filters filters = null, string sSqlCondition = "", string sSqlOrderBy = "elements.creationtime", bool bIncludeHiddenJuveniles = false) {
      if (!sd.MaySeeElements) {
        return Array.Empty<Element>();
      }
      if (filters != null) {
        sSqlCondition = filters.AddAllFiltersToWhereClause(sSqlCondition, sd.CurrentProject);
      }
      if (sd.CurrentProject.HideUpToBodyLength > 0 && sSqlCondition.Contains("indivdata.iid") && !bIncludeHiddenJuveniles) {
        string sAddSqlCondition = "indivdata.headbodylength>" + ConvInvar.ToString(sd.CurrentProject.HideUpToBodyLength);
        if (string.IsNullOrEmpty(sSqlCondition)) {
          sSqlCondition = sAddSqlCondition;
        } else {
          sSqlCondition = sAddSqlCondition + " AND (" + sSqlCondition + ")";
        }
      }
      var lElements = new List<Element>();
      var lDirtyElements = new List<Element>();
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT elements.name" +
          ",elements.markerposlat" +
          ",elements.markerposlng" +
          ",elements.uploadtime" +
          ",elements.uploader" +
          ",elements.creationtime" +
          ",photos.exifmake" +
          ",photos.exifmodel" +
          ",photos.exifdatetimeoriginal" +
          ",indivdata.iid" +
          ",indivdata.gender" +                   // 10
          ",indivdata.dateofbirth" +              // 11
          ",indivdata.headbodylength" +           // 12 <-- 17
          ",elements.place" +                     // 13 <-- 32
          ",elements.comment" +                   // 14 <-- 33
          ",elements.classification" +            // 15 <-- 34
          ",elements.measuredata" +               // 16 <-- 35
          ",elements.croppingconfirmed" +         // 17 <-- 36
          ",indivdata.genderfeature" +            // 18 <-- 37
          ",indivdata.shareofblack" +             // 19 <-- 38
          ",indivdata.centerofmass" +             // 20 <-- 39
          ",indivdata.stddeviation" +             // 21 <-- 40
          ",indivdata.entropy" +                  // 22 <-- 41
          ",elements.classname" +                 // 23 <-- 42
          ",elements.habquality" +                // 24 <-- 43
          ",elements.habmonitoring" +             // 25 <-- 44
          ",elements.lbsciname" +                 // 26 <-- 45
          ",elements.lbstadium" +                 // 27 <-- 46
          ",elements.lbcount" +                   // 28 <-- 47
          ",indivdata.captivitybred" +            // 29 <-- 48
          ",indivdata.phenotypeidx" +             // 30 <-- 49
          ",indivdata.genotypeidx" +              // 31 <-- 50
          ",indivdata.mass" +                     // 32
          " FROM elements" +
          " LEFT JOIN indivdata ON (indivdata.name=elements.name)" +
          " LEFT JOIN photos ON (photos.name=elements.name)" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE (" + sSqlCondition + ")")) +
          (string.IsNullOrEmpty(sSqlOrderBy) ? "" : (" ORDER BY " + sSqlOrderBy + "")) +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          try {
            bool bDirty = false;
            string sElementName = dr.GetString(0);
            DateTime dtDateTimeOriginal = DateTime.MinValue;
            try {
              dtDateTimeOriginal = dr.GetDateTime(8);
            } catch {
              try {
                dtDateTimeOriginal = dr.GetDateTime(3);
              } catch { }
            }
            #region ElementClassification.
            var ec = new ElementClassification();
            if (dr.IsDBNull(23)) {
              // Has been written in old format (JSON).
              string sJsonClassification = dr.GetValue(15) as string;
              if (!string.IsNullOrEmpty(sJsonClassification)) {
                ec = JsonConvert.DeserializeObject<ElementClassification>(sJsonClassification);
                if (!string.IsNullOrEmpty(ec.LivingBeing?.Species?.SciName)) {
                  ec.LivingBeing.Taxon = sd.CurrentProject.GetTaxon(ec.LivingBeing.Species.SciName);
                }
              }
              bDirty = true;
            } else {
              // Has been written in new format.
              ec.ClassName = dr.GetString(23);
              if (ec.IsHabitat()) {
                ec.Habitat = new ElementClassification.Habitat_t {
                  Quality = dr.GetInt32(24),
                  Monitoring = (dr.GetInt32(25) != 0),
                };
              } else if (ec.IsLivingBeing()) {
                ec.LivingBeing = new ElementClassification.LivingBeing_t {
                  Taxon = sd.CurrentProject.GetTaxon(dr.GetString(26)),
                  Stadium = (ElementClassification.Stadium)dr.GetInt32(27),
                  Count = dr.GetInt32(28),
                };
              }
            }
            #endregion
            string sJsonMeasureData = dr.IsDBNull(16) ? null : dr.GetValue(16) as string;
            Blazor.ImageSurveyor.ImageSurveyorMeasureData md = null;
            if (!string.IsNullOrEmpty(sJsonMeasureData) && sJsonMeasureData != "null") {
              md = JsonConvert.DeserializeObject<Blazor.ImageSurveyor.ImageSurveyorMeasureData>(sJsonMeasureData);
              while (md.measurePoints.Length < 4) {
                md.measurePoints = md.measurePoints.Append(new System.Numerics.Vector2()).ToArray();
                bDirty = true;
              }
              if (md.normalizer.PatternRelWidth == 0f) {
                md.normalizer.PatternRelWidth = Blazor.ImageSurveyor.ImageSurveyorMeasureData.Empty.normalizer.PatternRelWidth;
              }
              if (md.normalizer.PatternRelHeight == 0f) {
                md.normalizer.PatternRelHeight = Blazor.ImageSurveyor.ImageSurveyorMeasureData.Empty.normalizer.PatternRelHeight;
              }
              if (md.Threshold == 0f) {
                md.Threshold = Blazor.ImageSurveyor.ImageSurveyorMeasureData.Empty.Threshold;
              }
              if (md.BinaryThresholdMode == 0) {
                md.BinaryThresholdMode = (new Blazor.ImageSurveyor.ImageSurveyorMeasureData()).BinaryThresholdMode;
              }
            }
            if (md != null && md.normalizer == null) {
              md.normalizer = sd.CurrentProject.ImageNormalizer;
            }

            var el = new Element(sd.CurrentUser.Project) {
              ElementName = sElementName,
              Classification = ec,
              CroppingConfirmed = (!dr.IsDBNull(17) && dr.GetInt32(17) == 1),
              MeasureData = md,
              ElementProp = new Element.ElementProp_t {
                MarkerInfo = new Element.MarkerInfo_t {
                  position = new LatLng {
                    lat = dr.GetDouble(1),
                    lng = dr.GetDouble(2),
                  },
                  PlaceName = dr.GetString(13),
                },
                UploadInfo = new Element.UploadInfo_t {
                  Timestamp = dr.GetDateTime(3),
                  UserId = dr.GetString(4),
                  Comment = dr.GetString(14),
                },
                CreationTime = dr.GetDateTime(5),
              }
            };
            if (!dr.IsDBNull(6) && !dr.IsDBNull(7)) {
              el.ElementProp.ExifData = new Element.ExifData_t {
                Make = dr.GetString(6),
                Model = dr.GetString(7),
                DateTimeOriginal = dtDateTimeOriginal,
              };
              if (!dr.IsDBNull(9)) {
                int nSpeciesVariantIdx = dr.IsDBNull(30) ? 0 : dr.GetInt32(30);
                el.ElementProp.IndivData = new Element.IndivData_t {
                  IId = dr.GetInt32(9),
                  CaptivityBred = (dr.IsDBNull(29) ? 0 : dr.GetInt32(29)),
                  PhenotypeIdx = (dr.IsDBNull(30) ? (nSpeciesVariantIdx + 1) : dr.GetInt32(30)),
                  GenotypeIdx = (dr.IsDBNull(31) ? 0 : dr.GetInt32(31)),
                  GenderFeature = (dr.IsDBNull(18) ? "" : dr.GetString(18)),
                  Gender = dr.GetString(10),
                  DateOfBirth = dr.GetDateTime(11),
                };
                if (!dr.IsDBNull(12)) {
                  el.ElementProp.IndivData.MeasuredData = new Element.IndivData_t.MeasuredData_t {
                    HeadBodyLength = dr.GetDouble(12),
                    ShareOfBlack = dr.IsDBNull(19) ? 0 : dr.GetDouble(19),
                    CenterOfMass = dr.IsDBNull(20) ? 0 : dr.GetDouble(20),
                    StdDeviation = dr.IsDBNull(21) ? 0 : dr.GetDouble(21),
                    Entropy = dr.IsDBNull(22) ? 0 : dr.GetDouble(22),
                    Mass = dr.IsDBNull(32) ? 0 : dr.GetDouble(32),
                  };
                }
              }
            }
            lElements.Add(el);
            if (bDirty) {
              lDirtyElements.Add(el);
            }
          } catch { }
        }
        dr.Close();
      });
      foreach (Element el in lDirtyElements) {
        this.WriteElement(sd, el);
      }
      if (this.IsMigrationInProcess || (sd != null && sd.MaySeeRealLocations)) {
        return lElements.ToArray();
      } else {
        foreach (Element el in lElements.ToArray()) {
          el.ElementProp.MarkerInfo.position = GetAlienatedPosition(sd, el.ElementProp.MarkerInfo.position);
        }
        return lElements.ToArray();
      }
    }
    public PatternImage GetPatternImage(SessionData sd, string sElementName) {
      PatternImage piResult = null;
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT measurepoints,dataimagesrc FROM patternimages WHERE name='" + sElementName + "'";
        IDataReader dr = command.ExecuteReader();
        if (dr.Read()) {
          try {
            piResult = new PatternImage() {
              MeasureDataJson = dr.GetString(0),
              DataImageSrc = dr.GetString(1),
            };
          } catch { }
        }
        dr.Close();
      });
      return piResult;
    }
    public void WritePatternImage(SessionData sd, string sElementName, PatternImage pi) {
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "REPLACE INTO patternimages (name,measurepoints,dataimagesrc) VALUES ('" + sElementName + "','" + pi.MeasureDataJson + "','" + pi.DataImageSrc + "')";
        command.ExecuteNonQuery();
      });
    }
    public Element[] GetCatches(SessionData sd, Filters filters = null, string sAdditionalWhereClause = null, bool bIncludeAllPhotosOfIndivuals = false) {
      var aaIndisByIId = new Dictionary<int, List<Element>>();
      string sWhereClause = WhereClauses.Is_Individuum;
      sWhereClause = Filters.AddToWhereClause(sWhereClause, sAdditionalWhereClause);
      Element[] aNormedElements = this.GetElements(sd, filters, sWhereClause, "indivdata.iid ASC,elements.creationtime ASC");
      if (bIncludeAllPhotosOfIndivuals && !(filters == null || filters.IsEmpty(sd.CurrentProject))) {
        var lIndivs = new List<Element>();
        foreach (int iid in aNormedElements.Select(e => e.GetIIdAsInt()).Distinct().Where(iid => iid.HasValue).Select(iid => iid.Value)) {
          string sWhereClause1 = WhereClauses.Is_Individuum;
          sWhereClause1 = Filters.AddToWhereClause(sWhereClause1, $"indivdata.iid={iid}");
          lIndivs.AddRange(this.GetElements(sd, null, sWhereClause1, "indivdata.iid ASC,elements.creationtime ASC"));
        }
        aNormedElements = lIndivs.ToArray();
      }
      return aNormedElements;
    }
    public Dictionary<int, List<Element>> GetIndividuals(SessionData sd, Filters filters = null, string sAdditionalWhereClause = null, bool bIncludeAllPhotosOfIndivuals = false) {
      var aaIndisByIId = new Dictionary<int, List<Element>>();
      Element[] aNormedElements = this.GetCatches(sd, filters, sAdditionalWhereClause, bIncludeAllPhotosOfIndivuals);
      foreach (Element el in aNormedElements) {
        if (el.ElementProp.IndivData != null) {
          int idx = el.ElementProp.IndivData.IId;
          if (!aaIndisByIId.ContainsKey(idx)) {
            aaIndisByIId.Add(idx, new List<Element>());
          }
          aaIndisByIId[idx].Add(el);
        }
      }
      return aaIndisByIId;
    }
    /// <summary>
    /// Calculate the population size.
    /// </summary>
    /// <param name="sd">
    /// The session data. Filter settings will be used.
    /// </param>
    /// <param name="dtWhen">
    /// When to calculate.
    /// </param>
    /// <param name="nMinWinters">
    /// Minimum hibernations for an individual to count to the population. If null, it will not be applied
    /// </param>
    /// <param name="nVanishAfterYearsMissing">
    /// The number of years after which an individual does no more count if not caught. If 0 individuals never vanish.
    /// </param>
    /// <returns>
    /// The number of individuals in the population.
    /// </returns>
    public int GetPopulationSize(SessionData sd, DateTime dtWhen, int? nMinWinters, int nVanishAfterYearsMissing) {
      string sAddFilter = "";
      sAddFilter = Filters.AddToWhereClause(sAddFilter, "elements.creationtime<'" + ConvInvar.ToString(dtWhen) + "'");
      if (nMinWinters.HasValue) {
        sAddFilter = Filters.AddToWhereClause(sAddFilter, "indivdata.winters>=" + ConvInvar.ToString(nMinWinters.Value) + "");
      }
      if (nVanishAfterYearsMissing >= 1) {
        DateTime dtVanish = dtWhen - TimeSpan.FromDays(365 * nVanishAfterYearsMissing);
        sAddFilter = Filters.AddToWhereClause(sAddFilter, "elements.creationtime>'" + ConvInvar.ToString(dtVanish) + "'");
      }
      Dictionary<int, List<Element>> aaIndisByIId = sd.DS.GetIndividuals(sd, sd.Filters, sAddFilter);
      int nIndis = aaIndisByIId.Keys.Count;
      return nIndis;

    }
    public int GetNextFreeIId(SessionData sd) {
      int nUsedIId = 0;
      this.OperateOnDb(sd, (command) => {
        //command.CommandText = "SELECT iid AS iid1 FROM indivdata WHERE NOT EXISTS (SELECT iid FROM indivdata WHERE (iid=(iid1+1))) ORDER BY iid1 LIMIT 1";
        command.CommandText = "SELECT iid FROM indivdata ORDER BY iid DESC LIMIT 1";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          nUsedIId = dr.GetInt32(0);
        }
        dr.Close();
      });
      return nUsedIId + 1;
    }
    public void AddOrUpdateProtocolEntry(SessionData sd, ProtocolEntry pe) {
      this.OperateOnDb(sd, (command) => {
        if (string.IsNullOrEmpty(pe.Text)) {
          command.CommandText = "DELETE FROM notes WHERE (dt='" + ConvInvar.ToString(pe.CreationTime) + "' AND author='" + pe.Author + "')";
        } else {
          command.CommandText = "REPLACE INTO notes (dt,author,text) VALUES ('" + ConvInvar.ToString(pe.CreationTime) + "','" + pe.Author + "','" + GetEscapedSql(pe.Text) + "')";
        }
        command.ExecuteNonQuery();
      });
    }
    public ProtocolEntry[] GetProtocolEntries(SessionData sd, Filters filters = null, string sSqlCondition = "", string sSqlOrderBy = "notes.dt", uint nLimit = 0) {
      if (filters != null) {
        sSqlCondition = filters.AddAllFiltersToWhereClause(sSqlCondition, sd.CurrentProject);
      }
      return this.GetProtocolEntries(sd.CurrentUser.Project, sSqlCondition, sSqlOrderBy, nLimit);
    }
    public ProtocolEntry[] GetProtocolEntries(string sProject, string sSqlCondition = "", string sSqlOrderBy = "notes.dt", uint nLimit = 0) {
      var lProtocolEntries = new List<ProtocolEntry>();
      this.OperateOnDb(sProject, (command) => {
        command.CommandText = "SELECT notes.dt" +
          ",notes.author" +
          ",notes.text" +
          " FROM notes" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE (" + sSqlCondition + ")")) +
          (string.IsNullOrEmpty(sSqlOrderBy) ? "" : (" ORDER BY " + sSqlOrderBy + "")) +
          (nLimit == 0 ? "" : (" LIMIT " + nLimit + "")) +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          try {
            var pe = new ProtocolEntry() {
              CreationTime = dr.GetDateTime(0),
              Author = dr.GetString(1),
              Text = dr.GetString(2),
            };
            lProtocolEntries.Add(pe);
          } catch { }
        }
        dr.Close();
      });
      return lProtocolEntries.ToArray();
    }
    public string[] GetProtocolAuthors(SessionData sd, Filters filters = null) {
      string sSqlCondition = filters.AddAllFiltersToWhereClause("", sd.CurrentProject);
      var lProtocolAuthors = new List<string>();
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT DISTINCT author FROM notes" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE (" + sSqlCondition + ")")) +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          lProtocolAuthors.Add(dr.GetString(0));
        }
        dr.Close();
      });
      return lProtocolAuthors.ToArray();
    }
    public void AddLogEntry(SessionData sd, string sAction) {
      this.AddLogEntry(sd.CurrentUser.Project, sd.CurrentUser.EMail, sAction);
    }
    public void AddLogEntry(string sProject, string sUserId, string sAction) {
      this.OperateOnDb(sProject, (command) => {
        command.CommandText = "INSERT INTO log (dt,user,action) VALUES ('" + ConvInvar.ToString(DateTime.Now) + "','" + sUserId + "','" + sAction + "')";
        command.ExecuteNonQuery();
      });
    }
    public LogEntry[] GetLogEntries(SessionData sd, Filters filters = null, string sSqlCondition = "", string sSqlOrderBy = "log.dt", uint nLimit = 0) {
      if (filters != null) {
        sSqlCondition = filters.AddAllFiltersToWhereClause(sSqlCondition, sd.CurrentProject);
      }
      var lLogEntries = new List<LogEntry>();
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT dt,user,action FROM log" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE (" + sSqlCondition + ")")) +
          (string.IsNullOrEmpty(sSqlOrderBy) ? "" : (" ORDER BY " + sSqlOrderBy + "")) +
          (nLimit == 0 ? "" : (" LIMIT " + nLimit + "")) +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          try {
            var pe = new LogEntry() {
              CreationTime = dr.GetDateTime(0),
              User = dr.GetString(1),
              Action = dr.GetString(2),
            };
            lLogEntries.Add(pe);
          } catch { }
        }
        dr.Close();
      });
      return lLogEntries.ToArray();
    }
    public string[] GetLogUsers(SessionData sd, Filters filters = null) {
      string sSqlCondition = filters.AddAllFiltersToWhereClause("", sd.CurrentProject);
      var lLogUsers = new List<string>();
      this.OperateOnDb(sd, (command) => {
        command.CommandText = "SELECT DISTINCT user FROM log" +
          (string.IsNullOrEmpty(sSqlCondition) ? "" : (" WHERE (" + sSqlCondition + ")")) +
          "";
        IDataReader dr = command.ExecuteReader();
        while (dr.Read()) {
          lLogUsers.Add(dr.GetString(0));
        }
        dr.Close();
      });
      return lLogUsers.ToArray();
    }
  }
}
