using System;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
  [Route("api/photos")]
  [ApiController]
  public class PhotoController : ControllerBase
  {
    public static string GetFilePathForExistingImage(string sProject,string id,bool bForceOrig=false) {
      var ds = DataService.Instance;
      string sFilePath = ds.GetFilePathForImage(sProject,id,false);
      if (bForceOrig || !System.IO.File.Exists(sFilePath)) {
        sFilePath = ds.GetFilePathForImage(sProject,id,true);
      }
      if (System.IO.File.Exists(sFilePath)) {
        return sFilePath;
      }
      return "";
    }
    [HttpGet("{id}")]
    public IActionResult GetPhoto(string id) {
      try {
        bool bForceOrig = (Request.Query.ContainsKey("ForceOrig") && ConvInvar.ToInt(Request.Query["ForceOrig"],0)==1);
        bool bReqThumbnail = Request.Query.ContainsKey("width");
        string sProject="";
        if (Request.Query.ContainsKey("Project")) {
          sProject=Request.Query["Project"];
        }
        string sFilePath = GetFilePathForExistingImage(sProject,id,bForceOrig);
        if (!string.IsNullOrEmpty(sFilePath)) {
          try {
            if (Request.Query.ContainsKey("width")) {
              var bs = new MemoryStream();
              int nReqWidth = ConvInvar.ToInt(Request.Query["width"]);
              using (var image = Image.Load(sFilePath)) {
                int nReqHeight = (nReqWidth*image.Height)/image.Width;
                image.Mutate(x => x.AutoOrient());
                image.Mutate(x => x.Resize(nReqWidth,nReqHeight));
                image.SaveAsJpeg(bs);
              }
              return File(bs.ToArray(),"image/jpeg");
            }
            if (Request.Query.ContainsKey("maxdim")) {
              var bs = new MemoryStream();
              int nMaxDim = ConvInvar.ToInt(Request.Query["maxdim"]);
              using (var image = Image.Load(sFilePath)) {
                image.Mutate(x => x.AutoOrient());
                int nReqWidth;
                int nReqHeight;
                if (image.Height<=image.Width) {
                  nReqWidth = nMaxDim;
                  nReqHeight = (nMaxDim*image.Height)/image.Width;
                } else {
                  nReqWidth = (nMaxDim*image.Width)/image.Height;
                  nReqHeight = nMaxDim;
                }
                image.Mutate(x => x.Resize(nReqWidth,nReqHeight));
                if (nReqWidth!=nReqHeight) {
                  var sqImg = new Image<SixLabors.ImageSharp.PixelFormats.Argb32>(nMaxDim,nMaxDim,Color.Gray);
                  sqImg.Mutate(x => x.DrawImage(image,new Point((nMaxDim-nReqWidth)/2,(nMaxDim-nReqHeight)/2),1));
                  sqImg.SaveAsJpeg(bs);
                } else {
                  image.SaveAsJpeg(bs);
                }
              }
              return File(bs.ToArray(),"image/jpeg");
            }
          } catch {
            // System.Drawing does not work on Linux.
          }
          {
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