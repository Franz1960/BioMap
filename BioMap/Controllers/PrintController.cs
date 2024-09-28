using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
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
      DataService ds = this.DS;
      try {
        SessionData sd = ControllerHelper.CreateSessionData(this, ds);
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
            return this.StatusCode(404, $"Document not found: {id}");
          }
          fs.WriteLine("</body>");
          fs.Close();
          var responseStream = new System.IO.FileStream(sTempFileName, System.IO.FileMode.Open);
          this.Response.OnCompleted(async () => await Task.Run(() => {
            responseStream.Close();
            System.IO.File.Delete(sTempFileName);
          }));
          return this.File(responseStream, "text/html");
        } catch (Exception ex) {
          return this.StatusCode(500, $"Internal server error: {ex}");
        }
      } catch (Exception ex) {
        return this.StatusCode(500, $"Internal server error: {ex}");
      }
    }
    private void WriteNotes(System.IO.TextWriter tw, SessionData sd) {
      ProtocolEntry[] aProtocolEntries = this.DS.GetProtocolEntries(sd.CurrentUser.Project, "", "notes.dt ASC");
      tw.WriteLine("<h2>Notes</h2>");
      foreach (ProtocolEntry pe in aProtocolEntries) {
        tw.WriteLine("<p><b>" + ConvInvar.ToIsoDateTime(pe.CreationTime) + "</b> / " + pe.Author + "</p>");
        tw.WriteLine(pe.Text);
      }
    }
    private void WriteIdPhotos(System.IO.TextWriter tw, SessionData sd) {
      string sUrlBase = "https://" + this.HttpContext.Request.Host.ToUriComponent() + "/";
      bool bPortraitFormat = (sd.CurrentProject.ImageNormalizer.NormalizedHeightPx > 1.1 * sd.CurrentProject.ImageNormalizer.NormalizedWidthPx);
      Element[] els = this.DS.GetElements(sd, null, WhereClauses.Is_Individuum, $"indivdata.iid ASC,elements.creationtime ASC");
      tw.WriteLine("<h2>ID Photos</h2>");
      foreach (Element el in els) {
        if (bPortraitFormat) {
          tw.WriteLine($"<p>");
          tw.WriteLine($"<img src=\"../../api/photos/{el.ElementName}?rotate=-90&zoom=1&Project={sd.CurrentUser.Project}\" style=\"margin-top:1px;\"/>");
          tw.WriteLine("<br/><b>#" + el.GetIId() + "</b> (" + el.GetIsoDate() + " / " + el.GetHeadBodyLengthNice() + " / " + el.GetGenderFull(sd) + " / " + el.GetPlaceName() + ")");
          tw.WriteLine($"</p>");
        } else {
          tw.WriteLine($"<div style='float:left;width:250px;padding:5px;'>");
          tw.WriteLine($"<img src=\"../../api/photos/{el.ElementName}?rotate=0&zoom=0.40&Project={sd.CurrentUser.Project}\" style=\"margin-top:1px;\"/>");
          tw.WriteLine("<br/><small><b>#" + el.GetIId() + "</b> (" + el.GetIsoDate() + " / " + el.GetHeadBodyLengthNice() + " / " + el.GetGenderFull(sd) + " / " + el.GetPlaceName() + ")</small>");
          tw.WriteLine($"</div>");
        }
      }
    }
  }
}
