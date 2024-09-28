using System;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
  [Route("api/docs")]
  [ApiController]
  public class DocumentController : ControllerBase
  {
    public DocumentController(DataService ds) {
      this.DS = ds;
    }
    private readonly DataService DS;
    public static string GetFilePathForExistingDocument(DataService ds, string sProject, string id) {
      string sDocsDir = ds.GetDocsDir(sProject);
      string sFilePath = System.IO.Path.Combine(sDocsDir, id);
      if (System.IO.File.Exists(sFilePath)) {
        return sFilePath;
      }
      return "";
    }
    [HttpGet("{id}")]
    public IActionResult GetDocument(string id) {
      DataService ds = this.DS;
      try {
        string sProject = "";
        if (this.Request.Query.ContainsKey("Project")) {
          sProject = this.Request.Query["Project"];
        }
        Document document = ds.GetDocs(sProject, id).FirstOrDefault();
        if (!string.IsNullOrEmpty(document.Filename)) {
          Byte[] b = System.IO.File.ReadAllBytes(System.IO.Path.Combine(ds.GetDocsDir(sProject), document.Filename));
          string sContentType = document.ContentType;
          return this.File(b, sContentType);
        } else {
          return this.StatusCode(404, $"Document not found: {id}");
        }
      } catch (Exception ex) {
        return this.StatusCode(500, $"Internal server error: {ex}");
      }
    }
  }
}
