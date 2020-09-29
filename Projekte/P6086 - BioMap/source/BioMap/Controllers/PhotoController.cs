using System;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace BioMap
{
  [Route("api/photos")]
  [ApiController]
  public class PhotoController : ControllerBase
  {
    [HttpGet("{id}")]
    public IActionResult GetPhoto(string id) {
      try {
        var ds = DataService.Instance;
        bool bReqThumbnail = Request.Query.ContainsKey("width");
        string sFilePath = System.IO.Path.Combine(ds.DataDir,System.IO.Path.Combine("images",id));
        if (!System.IO.File.Exists(sFilePath)) {
          sFilePath = System.IO.Path.Combine(ds.DataDir,System.IO.Path.Combine("images_orig",id));
        }
        if (System.IO.File.Exists(sFilePath)) {
          if (Request.Query.ContainsKey("width")) {
            int nReqWidth = ConvInvar.ToInt(Request.Query["width"]);
            var bmSrc=System.Drawing.Bitmap.FromFile(sFilePath);
            int nReqHeight = (nReqWidth*bmSrc.Height)/bmSrc.Width;
            var bmThumbnail = new System.Drawing.Bitmap(nReqWidth,nReqHeight);
            using (var graphics = System.Drawing.Graphics.FromImage(bmThumbnail)) {
              graphics.DrawImage(bmSrc,0,0,bmThumbnail.Width,bmThumbnail.Height);
            }
            var bs = new MemoryStream();
            bmThumbnail.Save(bs,System.Drawing.Imaging.ImageFormat.Jpeg);
            return File(bs.ToArray(),"image/jpeg");
          } else {
            Byte[] b = System.IO.File.ReadAllBytes(sFilePath);
            return File(b,"image/jpeg");
          }
        } else {
          return StatusCode(404,$"Photo not found: {id}");
        }
      } catch (Exception ex) {
        return StatusCode(500,$"Internal server error: {ex}");
      }
    }
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
            var fullPath = System.IO.Path.Combine(pathToSave,fileName);
            using (var stream = new System.IO.FileStream(fullPath,System.IO.FileMode.Create)) {
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