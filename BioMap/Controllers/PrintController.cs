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
        if (string.CompareOrdinal(id, "notes") == 0) {
          string sProject = "";
          if (this.Request.Query.ContainsKey("Project")) {
            sProject = this.Request.Query["Project"];
          }
          try {
            var aProtocolEntries = ds.GetProtocolEntries(sProject, "", "notes.dt ASC");
            string sTempFileName = System.IO.Path.GetTempFileName();
            var fs = new System.IO.StreamWriter(sTempFileName,false,System.Text.Encoding.UTF8);
            fs.WriteLine("");
            fs.WriteLine("<!DOCTYPE html>");
            fs.WriteLine("<html lang= \"de\">");
            fs.WriteLine("<head>");
            fs.WriteLine("  <meta charset=\"utf - 8\" />");
            fs.WriteLine("  <title>BioMap Notes</title>");
            fs.WriteLine("</head>");
            fs.WriteLine("<body>");
            fs.WriteLine("<h2>Notes</h2>");
            foreach (var pe in aProtocolEntries) {
              fs.WriteLine("<p><b>" + ConvInvar.ToIsoDateTime(pe.CreationTime) + "</b> / " + pe.Author + "</p>");
              fs.WriteLine(pe.Text);
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
        } else {
          return StatusCode(404, $"Document not found: {id}");
        }
      } catch (Exception ex) {
        return StatusCode(500, $"Internal server error: {ex}");
      }
    }
  }
}
