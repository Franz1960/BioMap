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
          "creationtime DATETIME NOT NULL," +
          "creator TEXT NOT NULL," +
          "uploadtime DATETIME NOT NULL," +
          "uploader TEXT NOT NULL," +
          "category INT NOT NULL," +
          "markerposlat REAL," +
          "markerposlng REAL," +
          "iid INT)";
          command.ExecuteNonQuery();
          command.CommandText = "CREATE TABLE IF NOT EXISTS photos (" +
          "name TEXT PRIMARY KEY NOT NULL," +
          "origname TEXT NOT NULL," +
          "uploadtime DATETIME NOT NULL," +
          "uploader TEXT NOT NULL," +
          "category INT NOT NULL," +
          "markerposlat REAL," +
          "markerposlng REAL," +
          "iid INT," +
          "exifdata TEXT)";
          command.ExecuteNonQuery();
          #endregion
        });
        //
        this.AddLogEntry("System", "Web service started");
        //
        Migration.MigrateData();
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

  }
}
