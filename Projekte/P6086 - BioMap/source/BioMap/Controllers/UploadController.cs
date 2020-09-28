using System;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace BioMap
{
  [Route("api/upload")]
  [ApiController]
  public class UploadController : ControllerBase
  {
    [HttpPost]
    public IActionResult Upload() {
      var ds = DataService.Instance;
      var sImagesOrigDir = System.IO.Path.Combine(ds.DataDir,"images_orig");
      try {
        if (Request.Form.Files.Count>=1) {
          var file = Request.Form.Files[0];
          var pathToSave = sImagesOrigDir;
          if (file.Length > 0) {
            var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            var fullPath = Path.Combine(pathToSave,fileName);
            using (var stream = new FileStream(fullPath,FileMode.Create)) {
              file.CopyTo(stream);
            }
            var el = Element.CreateFromImageFile(fullPath);
            ds.WriteElement(el);
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