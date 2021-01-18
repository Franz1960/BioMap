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
    public static string GetFilePathForExistingDocument(string sProject,string id) {
      var ds = DataService.Instance;
      var sDocsDir = ds.GetDocsDir(sProject);
      string sFilePath = System.IO.Path.Combine(sDocsDir,id);
      if (System.IO.File.Exists(sFilePath)) {
        return sFilePath;
      }
      return "";
    }
    [HttpGet("{id}")]
    public IActionResult GetDocument(string id) {
      var ds = DataService.Instance;
      try {
        string sProject="";
        if (Request.Query.ContainsKey("Project")) {
          sProject=Request.Query["Project"];
        }
        var document = ds.GetDocs(sProject,id).FirstOrDefault();
        if (!string.IsNullOrEmpty(document.Filename)) {
          Byte[] b = System.IO.File.ReadAllBytes(System.IO.Path.Combine(ds.GetDocsDir(sProject),document.Filename));
          string sContentType=
            document.DocType==Document.DocType_en.Pdf?"application/pdf":
            "text/plain";
          return File(b,sContentType);
        } else {
          return StatusCode(404,$"Document not found: {id}");
        }
      } catch (Exception ex) {
        return StatusCode(500,$"Internal server error: {ex}");
      }
    }
  }
}