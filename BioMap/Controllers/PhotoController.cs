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
    public PhotoController(DataService ds) {
      this.DS = ds;
    }
    private readonly DataService DS;
    public static string GetFilePathForExistingImage(DataService ds, string sProject, string id, bool bForceOrig = false) {
      string sFilePath = ds.GetFilePathForImage(sProject, id, false);
      if (bForceOrig || !System.IO.File.Exists(sFilePath)) {
        sFilePath = ds.GetFilePathForImage(sProject, id, true);
      }
      if (System.IO.File.Exists(sFilePath)) {
        return sFilePath;
      }
      return "";
    }
    [HttpGet("{id}")]
    public IActionResult GetPhoto(string id) {
      try {
        DataService ds = this.DS;
        SessionData sd = ControllerHelper.CreateSessionData(this, ds);
        bool bForceOrig = (this.Request.Query.ContainsKey("ForceOrig") && ConvInvar.ToInt(this.Request.Query["ForceOrig"], 0) == 1);
        bool bReqThumbnail = this.Request.Query.ContainsKey("width");
        bool bIncludeGPS = this.Request.Query.ContainsKey("IncludeGPS") && sd != null && sd.MaySeeRealLocations;
        string sProject = "";
        if (this.Request.Query.ContainsKey("Project")) {
          sProject = this.Request.Query["Project"];
        }
        string sFilePath = GetFilePathForExistingImage(this.DS, sProject, id, bForceOrig);
        if (!string.IsNullOrEmpty(sFilePath)) {
          try {
            float fRotateAngleDeg = (float)this.GetRequestDoubleParam("rotate", 0);
            double dZoom = this.GetRequestDoubleParam("zoom", 0);
            int nReqWidth = this.GetRequestIntParam("width", 0);
            int nMaxDim = this.GetRequestIntParam("maxdim", 0);
            if (dZoom != 0 || nReqWidth != 0 || nMaxDim != 0) {
              using (var image = Image.Load(sFilePath)) {
                var bs = new MemoryStream();
                image.Mutate(x => x.AutoOrient());
                if (fRotateAngleDeg != 0) {
                  image.Mutate(x => x.Rotate(fRotateAngleDeg));
                }
                int nOrigWidth = image.Width;
                int nOrigHeight = image.Height;
                //
                if (dZoom != 0) {
                  int nBigWidth = (int)(dZoom * nOrigWidth);
                  int nBigHeight = (int)(dZoom * nOrigHeight);
                  image.Mutate(x => x.Resize(nBigWidth, nBigHeight));
                  if (dZoom > 1) {
                    image.Mutate(x => x.Crop(new Rectangle((nBigWidth - nOrigWidth) / 2, (nBigHeight - nOrigHeight) / 2, nOrigWidth, nOrigHeight)));
                  }
                }
                //
                if (nReqWidth != 0) {
                  int nReqHeight = (nReqWidth * image.Height) / image.Width;
                  image.Mutate(x => x.Resize(nReqWidth, nReqHeight));
                } else if (nMaxDim != 0) {
                  image.Mutate(x => x.AutoOrient());
                  int nReqHeight;
                  if (image.Height <= image.Width) {
                    nReqWidth = nMaxDim;
                    nReqHeight = (nMaxDim * image.Height) / image.Width;
                  } else {
                    nReqWidth = (nMaxDim * image.Width) / image.Height;
                    nReqHeight = nMaxDim;
                  }
                  image.Mutate(x => x.Resize(nReqWidth, nReqHeight));
                  if (nReqWidth != nReqHeight) {
                    var sqImg = new Image<SixLabors.ImageSharp.PixelFormats.Argb32>(nMaxDim, nMaxDim, Color.Gray);
                    sqImg.Mutate(x => x.DrawImage(image, new Point((nMaxDim - nReqWidth) / 2, (nMaxDim - nReqHeight) / 2), 1));
                    sqImg.SaveAsJpeg(bs);
                  }
                }
                if (bs.Length < 1) {
                  image.SaveAsJpeg(bs);
                }
                return this.File(bs.ToArray(), "image/jpeg");
              }
            }
          } catch {
            // System.Drawing does not work on Linux.
          }
          if (bIncludeGPS) {
            using (var image = Image.Load(sFilePath)) {
              var bs = new MemoryStream();
              Element el = ds.GetElements(sd, null, "elements.name='" + id + "'")?[0];
              if (el?.ElementProp?.MarkerInfo?.position != null && image.Metadata?.ExifProfile != null) {
                LatLng pos = el.ElementProp.MarkerInfo.position;
                DateTime dt = el.ElementProp.CreationTime;
                image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLatitude, Utilities.LatLngRational_from_double(pos.lat));
                image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLongitude, Utilities.LatLngRational_from_double(pos.lng));
                image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLatitudeRef, "N");
                image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSLongitudeRef, "E");
                image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSTimestamp, new[] { new Rational(dt.Hour), new Rational(dt.Minute), new Rational(dt.Second) });
                image.Metadata.ExifProfile.SetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.GPSDateStamp, dt.ToString("yyyy-MM-dd"));
              }
              image.SaveAsJpeg(bs);
              return this.File(bs.ToArray(), "image/jpeg");
            }
          } else {
            Byte[] b = System.IO.File.ReadAllBytes(sFilePath);
            return this.File(b, "image/jpeg");
          }
        } else {
          return this.StatusCode(404, $"Photo not found: {id}");
        }
      } catch (Exception ex) {
        return this.StatusCode(500, $"Internal server error: {ex}");
      }
    }
    //[HttpPost]
    //public IActionResult Upload() {
    //  string sProject = this.HttpContext.Request.Cookies["Project"];
    //  string EMailAddr = this.HttpContext.Request.Cookies["UserId"];
    //  var ds = DataService.Instance;
    //  var sImagesOrigDir = System.IO.Path.Combine(ds.GetDataDir(sProject),"images_orig");
    //  try {
    //    if (Request.Form.Files.Count>=1) {
    //      var file = Request.Form.Files[0];
    //      var pathToSave = sImagesOrigDir;
    //      if (file.Length > 0) {
    //        var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
    //        var fullPath = System.IO.Path.Combine(pathToSave,fileName);
    //        using (var stream = new System.IO.FileStream(fullPath,System.IO.FileMode.Create)) {
    //          file.CopyTo(stream);
    //        }
    //        var el = Element.CreateFromImageFile(sProject,fullPath,EMailAddr);
    //        ds.WriteElement(sProject,el);
    //        return Ok(fileName);
    //      }
    //    }
    //  } catch (Exception ex) {
    //    return StatusCode(500,$"Internal server error: {ex}");
    //  }
    //  return BadRequest();
    //}
    private int GetRequestIntParam(string sName, int nDefaultValue) {
      if (this.Request.Query.ContainsKey(sName)) {
        return ConvInvar.ToInt(this.Request.Query[sName]);
      }
      return nDefaultValue;
    }
    private double GetRequestDoubleParam(string sName, double dDefaultValue) {
      if (this.Request.Query.ContainsKey(sName)) {
        return ConvInvar.ToDouble(this.Request.Query[sName]);
      }
      return dDefaultValue;
    }
  }
}
