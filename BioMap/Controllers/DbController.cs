using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
  [Route("api/db")]
  [ApiController]
  public class DbController : ControllerBase
  {
    public DbController(DataService ds) {
      this.DS = ds;
    }
    private readonly DataService DS;
    [HttpGet("{id}")]
    public IActionResult GetDb(string id) {
      DataService ds = this.DS;
      SessionData sd = ControllerHelper.CreateSessionData(this, ds);
      try {
        if (string.CompareOrdinal(id, "biomap.sqlite") == 0) {
          string sProject = "";
          if (this.Request.Query.ContainsKey("Project")) {
            sProject = this.Request.Query["Project"];
          }
          try {
            string sTempFileName = System.IO.Path.GetTempFileName();
            System.IO.File.Copy(ds.GetDbFilePath(sProject), sTempFileName, true);
            var responseStream = new System.IO.FileStream(sTempFileName, System.IO.FileMode.Open);
            this.Response.OnCompleted(async () => await Task.Run(() => {
              responseStream.Close();
              System.IO.File.Delete(sTempFileName);
            }));
            this.Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + "biomap-" + sd.CurrentUser.Project + "-db.sqlite" + "\"");
            return this.File(responseStream, "application/octet-stream");
          } catch (Exception ex) {
            return this.StatusCode(500, $"Internal server error: {ex}");
          }
        } else if (string.CompareOrdinal(id, "places.gpx") == 0) {
          string sProject = "";
          if (this.Request.Query.ContainsKey("Project")) {
            sProject = this.Request.Query["Project"];
          }
          try {
            string sTempFileName = System.IO.Path.GetTempFileName();
            Place[] allPlaces = this.DS.GetPlaces(sd);
            //
            var xmlTextWriter = new System.Xml.XmlTextWriter(sTempFileName, System.Text.Encoding.UTF8) {
              Formatting = System.Xml.Formatting.Indented,
              Indentation = 2,
              IndentChar = ' '
            };
            //
            xmlTextWriter.WriteStartDocument();
            xmlTextWriter.WriteStartElement("gpx");
            xmlTextWriter.WriteAttributeString("version", "1.1");
            xmlTextWriter.WriteAttributeString("creator", "BioMap");
            xmlTextWriter.WriteAttributeString("xmlns", "http://www.topografix.com/GPX/1/1");
            xmlTextWriter.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
            xmlTextWriter.WriteAttributeString("xsi:schemaLocation", "http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd");
            //
            xmlTextWriter.WriteStartElement("metadata");
            xmlTextWriter.WriteStartElement("name");
            xmlTextWriter.WriteString(sd.CurrentUser.Project);
            xmlTextWriter.WriteEndElement();
            xmlTextWriter.WriteEndElement();
            //
            foreach (Place place in allPlaces) {
              xmlTextWriter.WriteStartElement("wpt");
              xmlTextWriter.WriteAttributeString("lat", ConvInvar.ToDecimalString(place.LatLng.lat, 6));
              xmlTextWriter.WriteAttributeString("lon", ConvInvar.ToDecimalString(place.LatLng.lng, 6));
              //
              xmlTextWriter.WriteStartElement("name");
              xmlTextWriter.WriteString(place.Name);
              xmlTextWriter.WriteEndElement();
              //
              xmlTextWriter.WriteEndElement();
            }
            //
            xmlTextWriter.WriteEndElement();
            xmlTextWriter.WriteEndDocument();
            //
            xmlTextWriter.Close();
            //
            var responseStream = new System.IO.FileStream(sTempFileName, System.IO.FileMode.Open);
            this.Response.OnCompleted(async () => await Task.Run(() => {
              responseStream.Close();
              System.IO.File.Delete(sTempFileName);
            }));
            this.Response.Headers.Add("Content-Disposition", "attachment; filename=\"" + "biomap-" + sd.CurrentUser.Project + "-places.gpx" + "\"");
            return this.File(responseStream, "application/gpx+xml");
          } catch (Exception ex) {
            return this.StatusCode(500, $"Internal server error: {ex}");
          }
        } else {
          return this.StatusCode(404, $"Document not found: {id}");
        }
      } catch (Exception ex) {
        return this.StatusCode(500, $"Internal server error: {ex}");
      }
    }
  }
}
