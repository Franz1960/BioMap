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
          "exifdatetimeoriginal TEXT)";
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
          "traitYellowDominance TEXT," +
          "traitBlackDominance TEXT," +
          "traitVertBlackBreastCenterStrip TEXT," +
          "traitHorizBlackBreastBellyStrip TEXT," +
          "traitManyIsolatedBlackBellyDots TEXT," +
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
          " FROM indivdata INNER JOIN elements ON (elements.name=indivdata.name) WHERE (iid>=1)" +
          //" INNER JOIN photos ON (photos.name=indivdata.name)" +
          "";
        var dr = command.ExecuteReader();
        while (dr.Read())
        {
          var el = new Element
          {
            ElementName = dr.GetString(0),
            ElementProp=new Element.ElementProp_t
            {
              MarkerInfo=new Element.MarkerInfo_t
              {
                category=dr.GetInt32(1),
                position=new LatLng {
                  lat=ConvInvar.ToDouble(dr.GetString(2)),
                  lng=ConvInvar.ToDouble(dr.GetString(3)),
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
