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
          "";
        var dr = command.ExecuteReader();
        while (dr.Read())
        {
          var el = new Element
          {
            ElementName = dr.GetString(0),
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
              ExifData = new Element.ExifData_t
              {
                Make = dr.GetString(7),
                Model = dr.GetString(8),
                DateTimeOriginal = dr.GetDateTime(9),
              },
              IndivData = new Element.IndivData_t
              {
                IId = dr.GetInt32(10),
                Gender = dr.GetString(11),
                YearOfBirth = dr.GetInt32(12),
                TraitValues = new Dictionary<string, int>
                {
                  { "YellowDominance",dr.GetInt32(13) },
                  { "BlackDominance",dr.GetInt32(14) },
                  { "VertBlackBreastCenterStrip",dr.GetInt32(15) },
                  { "HorizBlackBreastBellyStrip",dr.GetInt32(16) },
                  { "ManyIsolatedBlackBellyDots",dr.GetInt32(17) },
                },
                MeasuredData = new Element.IndivData_t.MeasuredData_t
                {
                  HeadBodyLength = dr.GetDouble(18),
                  OrigHeadPosition = new System.Numerics.Vector2(dr.GetFloat(19), dr.GetFloat(20)),
                  OrigBackPosition = new System.Numerics.Vector2(dr.GetFloat(21), dr.GetFloat(22)),
                  HeadPosition = new System.Numerics.Vector2(dr.GetFloat(23), dr.GetFloat(24)),
                  BackPosition = new System.Numerics.Vector2(dr.GetFloat(25), dr.GetFloat(26)),
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

          lElements.Add(el);
        }
        dr.Close();
      });
      return lElements.ToArray();
    }
  }
}
