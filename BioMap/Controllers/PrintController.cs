using System;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
  [Route("api/print")]
  [ApiController]
  public class PrintController : ControllerBase
  {
    public PrintController(DataService ds) {
      this.DS = ds;
    }
    private readonly DataService DS;
    [HttpGet("{id}")]
    public IActionResult Print(string id) {
      var ds = this.DS;
      try {
        string sProject = "";
        if (this.Request.Query.ContainsKey("Project")) {
          sProject = this.Request.Query["Project"];
        }
        string sUser= "";
        if (this.Request.Query.ContainsKey("User")) {
          sUser = this.Request.Query["User"];
        }
        string sPermTicket = "";
        if (this.Request.Query.ContainsKey("PermTicket")) {
          sPermTicket = this.Request.Query["PermTicket"];
        }
        var sd = new SessionData(ds);
        sd.CurrentUser.EMail = sUser;
        sd.CurrentUser.Project = sProject;
        ds.LoadUser(sd, sPermTicket, sd.CurrentUser);
        try {
          string sTempFileName = System.IO.Path.GetTempFileName();
          var fs = new System.IO.StreamWriter(sTempFileName, false, System.Text.Encoding.UTF8);
          fs.WriteLine("");
          fs.WriteLine("<!DOCTYPE html>");
          fs.WriteLine("<html lang= \"de\">");
          fs.WriteLine("<head>");
          fs.WriteLine("  <meta charset=\"utf - 8\" />");
          fs.WriteLine("  <title>BioMap Print View</title>");
          fs.WriteLine("</head>");
          fs.WriteLine("<body>");
          if (string.CompareOrdinal(id, "notes") == 0) {
            this.WriteNotes(fs, sd);
          } else if (string.CompareOrdinal(id, "id-photos") == 0) {
            this.WriteIdPhotos(fs, sd);
          } else {
            return StatusCode(404, $"Document not found: {id}");
          }
          fs.WriteLine("</body>");
          fs.Close();
          var responseStream = new System.IO.FileStream(sTempFileName, System.IO.FileMode.Open);
          this.Response.OnCompleted(async () => {
            responseStream.Close();
            System.IO.File.Delete(sTempFileName);
          });
          return this.File(responseStream, "text/html");
        } catch (Exception ex) {
          return this.StatusCode(500, $"Internal server error: {ex}");
        }
        return this.StatusCode(500, $"Internal server error without exception.");
      } catch (Exception ex) {
        return StatusCode(500, $"Internal server error: {ex}");
      }
    }
    private void WriteNotes(System.IO.TextWriter tw, SessionData sd) {
      var aProtocolEntries = this.DS.GetProtocolEntries(sd.CurrentUser.Project, "", "notes.dt ASC");
      tw.WriteLine("<h2>Notes</h2>");
      foreach (var pe in aProtocolEntries) {
        tw.WriteLine("<p><b>" + ConvInvar.ToIsoDateTime(pe.CreationTime) + "</b> / " + pe.Author + "</p>");
        tw.WriteLine(pe.Text);
      }
    }
    private void WriteIdPhotos(System.IO.TextWriter tw, SessionData sd) {
      var sUrlBase = "https://" + this.HttpContext.Request.Host.ToUriComponent() + "/";
      var els = this.DS.GetElements(sd, null, WhereClauses.Is_Individuum, $"indivdata.iid ASC,elements.creationtime ASC");
      tw.WriteLine("<h2>ID Photos</h2>");
      foreach (var el in els) {
        tw.WriteLine($"<p>");
        tw.WriteLine($"<img src=\"../../api/photos/{el.ElementName}?rotate=-90&zoom=1&Project={sd.CurrentUser.Project}\" style=\"margin-top:1px;\"/>");
        tw.WriteLine("<br/><b>#" + el.GetIId() + "</b> (" + el.GetIsoDate() + " / " + el.GetHeadBodyLengthNice() + " / " + el.GetGenderFull(sd) + " / " + el.GetPlaceName() + ")");
        tw.WriteLine($"</p>");
      }
    }
  }
}
