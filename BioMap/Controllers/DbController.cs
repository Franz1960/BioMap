using System;
using System.Linq;
using System.Net.Http.Headers;
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
      var ds = this.DS;
      try {
        if (string.CompareOrdinal(id, "biomap.sqlite") == 0) {
          string sProject = "";
          if (Request.Query.ContainsKey("Project")) {
            sProject = Request.Query["Project"];
          }
          try {
            string sTempFileName = System.IO.Path.GetTempFileName();
            System.IO.File.Copy(ds.GetDbFilePath(sProject), sTempFileName, true);
            var responseStream = new System.IO.FileStream(sTempFileName, System.IO.FileMode.Open);
            this.Response.OnCompleted(async () => {
              responseStream.Close();
              System.IO.File.Delete(sTempFileName);
            });
            return this.File(responseStream, "application/octet-stream");
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
