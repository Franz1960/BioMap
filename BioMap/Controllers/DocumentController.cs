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
    [HttpPost]
    public IActionResult Upload() {
      string sProject = this.HttpContext.Request.Cookies["Project"];
      string EMailAddr = this.HttpContext.Request.Cookies["UserId"];
      var ds = DataService.Instance;
      var sImagesOrigDir = System.IO.Path.Combine(ds.GetDataDir(sProject),"images_orig");
      try {
        if (Request.Form.Files.Count>=1) {
          var file = Request.Form.Files[0];
          var pathToSave = sImagesOrigDir;
          if (file.Length > 0) {
            var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            var fullPath = System.IO.Path.Combine(pathToSave,fileName);
            using (var stream = new System.IO.FileStream(fullPath,System.IO.FileMode.Create)) {
              file.CopyTo(stream);
            }
            var el = Element.CreateFromImageFile(sProject,fullPath,EMailAddr);
            ds.WriteElement(sProject,el);
            return Ok(fileName);
          }
        }
      } catch (Exception ex) {
        return StatusCode(500,$"Internal server error: {ex}");
      }
      return BadRequest();
    }
  }
}