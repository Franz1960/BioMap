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
    public DataService()
    {
      DataService.Instance = this;
    }
    public static DataService Instance { get; private set; }
    public string DataDir = "../../data/biomap/";
    public System.Data.IDbConnection DbConnection = null;
    public event EventHandler Initialized
    {
      add
      {
        lock (this.lockInitialized)
        {
          if (this.isInitialized)
          {
            value.Invoke(this, EventArgs.Empty);
          }
          else
          {
            this._Initialized += value;
          }
        }
      }
      remove
      {
        this._Initialized -= value;
      }
    }
    private event EventHandler _Initialized;
    private bool isInitialized = false;
    private readonly object lockInitialized = new object();
    public void OperateOnDb(Action<IDbCommand> dbAction)
    {
      try
      {
        this.DbConnection.Open();
        try
        {
          using (IDbCommand command = this.DbConnection.CreateCommand())
          {
            dbAction(command);
          }
        } finally {
          this.DbConnection.Close();
        }
      } catch { }
    }
    public Task Init()
    {
      return Task.Run(() =>
      {
        bool bMigrate = true;
        this.DbConnection = new SQLiteConnection();
        this.DbConnection.ConnectionString = "Data Source="+System.IO.Path.Combine(this.DataDir,"biomap.sqlite");
        this.OperateOnDb((command) =>
        {
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
          command.CommandText = "CREATE TABLE IF NOT EXISTS elements (" +
          "name TEXT PRIMARY KEY NOT NULL," +
          "category INT NOT NULL," +
          "markerposlat REAL," +
          "markerposlng REAL," +
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
          "yearofbirth INT," +
          "gender TEXT," +
          "iid INT)";
          command.ExecuteNonQuery();
          #endregion
          #region Prüfen, ob leer, also Migration notwendig.
          {
            command.CommandText = "SELECT name FROM elements";
            var dr = command.ExecuteReader();
            while (dr.Read())
            {
              bMigrate = false;
              break;
            }
            dr.Close();
          }
          #endregion
        });
        //
        this.AddLogEntry("System", "Web service started");
        //
        if (bMigrate)
        {
          Migration.MigrateData();
        }
        this.RefreshAllPlaces();
        //
        lock (this.lockInitialized)
        {
          this._Initialized?.Invoke(this, EventArgs.Empty);
          this.isInitialized = true;
        }
      });
    }
    public User CurrentUser { get; } = new User
    {
      EMail="f.x.haering@gmail.com", // Vorerst konstant vorbesetzen.
      FullName="Franz Häring",
      Level=700,
    };
    public void AddLogEntry(string sUser, string sAction)
    {
      this.OperateOnDb((command) =>
      {
        command.CommandText = "INSERT INTO log (dt,user,action) VALUES (datetime('now','localtime'),'" + sUser + "','" + sAction + "')";
        command.ExecuteNonQuery();
      });
    }
    public Place[] AllPlaces
    {
      get;
      private set;
    }
    public void RefreshAllPlaces()
    {
      var lPlaces = new List<Place>();
      this.OperateOnDb((command) =>
      {
        command.CommandText = "SELECT name,radius,lat,lng" +
          " FROM places" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read())
        {
          var place = new Place
          {
            Name = dr.GetString(0),
            Radius = dr.GetDouble(1),
            LatLng = new LatLng
            {
              lat = dr.GetDouble(2),
              lng = dr.GetDouble(3),
            },
          };

          lPlaces.Add(place);
        }
        dr.Close();
      });
      this.AllPlaces=lPlaces.ToArray();
    }
    public void WriteElement(Element el) {
      this.OperateOnDb((command) =>
      {
        command.CommandText =
          "INSERT INTO elements (name,category,markerposlat,markerposlng,uploadtime,uploader,creationtime) " +
          "VALUES ('" + el.ElementName +
          "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.category) +
          "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.position.lat) +
          "','" + ConvInvar.ToString(el.ElementProp.MarkerInfo.position.lng) +
          "','" + ConvInvar.ToString(el.ElementProp.UploadInfo.Timestamp) +
          "','" + el.ElementProp.UploadInfo.UserId +
          "','" + ConvInvar.ToString(el.ElementProp.CreationTime) +
          "')";
        command.ExecuteNonQuery();
        if (el.ElementProp.ExifData!=null) {
          command.CommandText =
            "INSERT INTO photos (name,filename,exifmake,exifmodel,exifdatetimeoriginal) " +
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
            "INSERT INTO indivdata (name,normcirclepos0x,normcirclepos0y,normcirclepos1x,normcirclepos1y,normcirclepos2x,normcirclepos2y" +
            ",headposx,headposy,backposx,backposy,origheadposx,origheadposy,origbackposx,origbackposy,headbodylength,yearofbirth,gender,iid" +
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
            "," + ConvInvar.ToString(el.ElementProp.IndivData.YearOfBirth) +
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
    public Element[] GetNormedElements()
    {
      var lElements = new List<Element>();
      this.OperateOnDb((command) =>
      {
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
          ",indivdata.yearofbirth" +
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
          " FROM indivdata" +
          " INNER JOIN elements ON (elements.name=indivdata.name)" +
          " INNER JOIN photos ON (photos.name=indivdata.name)" +
          " WHERE (indivdata.iid>=1)" +
          " ORDER BY elements.creationtime" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          try {
            var sElementName = dr.GetString(0);
            DateTime dtDateTimeOriginal = dr.GetDateTime(4);
            var sDateTimeOriginal = dr.GetString(9);
            DateTime.TryParse(sDateTimeOriginal,out dtDateTimeOriginal);
            var el = new Element
            {
              ElementName = sElementName,
              ElementProp = new Element.ElementProp_t
              {
                MarkerInfo = new Element.MarkerInfo_t
                {
                  category = dr.GetInt32(1),
                  position = new LatLng
                  {
                    lat = dr.GetDouble(2),
                    lng = dr.GetDouble(3),
                  },
                },
                UploadInfo = new Element.UploadInfo_t
                {
                  Timestamp = dr.GetDateTime(4),
                  UserId = dr.GetString(5),
                },
                CreationTime = dr.GetDateTime(6),
                ExifData = new Element.ExifData_t
                {
                  Make = dr.GetString(7),
                  Model = dr.GetString(8),
                  DateTimeOriginal = dtDateTimeOriginal,
                },
                IndivData = new Element.IndivData_t
                {
                  IId = dr.GetInt32(10),
                  Gender = dr.GetString(11),
                  YearOfBirth = dr.GetInt32(12),
                  TraitValues = new Dictionary<string,int>(),
                  MeasuredData = new Element.IndivData_t.MeasuredData_t
                  {
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
                  },
                },
              },
            };
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
            lElements.Add(el);
          } catch { }
        }
        dr.Close();
      });
      return lElements.ToArray();
    }
    public Element[] GetElements(string sSqlCondition=null)
    {
      var lElements = new List<Element>();
      this.OperateOnDb((command) =>
      {
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
          ",indivdata.yearofbirth" +
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
          " FROM elements" +
          " LEFT JOIN indivdata ON (indivdata.name=elements.name)" +
          " LEFT JOIN photos ON (photos.name=elements.name)" +
          (sSqlCondition==null ? "" : ("WHERE ("+sSqlCondition+")")) +
          " ORDER BY elements.creationtime" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read()) {
          try {
            var sElementName = dr.GetString(0);
            DateTime dtDateTimeOriginal = dr.GetDateTime(4);
            var sDateTimeOriginal = dr.GetString(9);
            DateTime.TryParse(sDateTimeOriginal,out dtDateTimeOriginal);
            var el = new Element
            {
              ElementName = sElementName,
              ElementProp = new Element.ElementProp_t
              {
                MarkerInfo = new Element.MarkerInfo_t
                {
                  category = dr.GetInt32(1),
                  position = new LatLng
                  {
                    lat = dr.GetDouble(2),
                    lng = dr.GetDouble(3),
                  },
                },
                UploadInfo = new Element.UploadInfo_t
                {
                  Timestamp = dr.GetDateTime(4),
                  UserId = dr.GetString(5),
                },
                CreationTime = dr.GetDateTime(6),
              }
            };
            if (!dr.IsDBNull(7) && !dr.IsDBNull(8)) {
              el.ElementProp.ExifData = new Element.ExifData_t
              {
                Make = dr.GetString(7),
                Model = dr.GetString(8),
                DateTimeOriginal = dtDateTimeOriginal,
              };
              if (!dr.IsDBNull(10)) {
                el.ElementProp.IndivData = new Element.IndivData_t
                {
                  IId = dr.GetInt32(10),
                  Gender = dr.GetString(11),
                  YearOfBirth = dr.GetInt32(12),
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
                  el.ElementProp.IndivData.MeasuredData = new Element.IndivData_t.MeasuredData_t
                  {
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
    public Dictionary<int,List<Element>> GetIndividuals() {
      var aaIndisByIId = new Dictionary<int,List<Element>>();
      var aNormedElements = this.GetNormedElements();
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
  }
}
