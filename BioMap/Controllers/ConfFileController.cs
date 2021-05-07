using System;
using System.Linq;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
    [Route("api/conf")]
    [ApiController]
    public class ConfFileController : ControllerBase
    {
        public static string GetFilePathForConfFile(string sProject, string id)
        {
            var ds = DataService.Instance;
            var sConfDir = ds.GetConfDir(sProject);
            string sFilePath = System.IO.Path.Combine(sConfDir, id);
            if (System.IO.File.Exists(sFilePath))
            {
                return sFilePath;
            }
            return "";
        }
        [HttpGet("{id}")]
        public IActionResult GetFile(string id)
        {
            var ds = DataService.Instance;
            try
            {
                string sProject = "";
                if (Request.Query.ContainsKey("Project"))
                {
                    sProject = Request.Query["Project"];
                }
                string sFilePath = GetFilePathForConfFile(sProject, id);
                if (System.IO.File.Exists(sFilePath))
                {
                    byte[] b = System.IO.File.ReadAllBytes(sFilePath);
                    string sContentType = MimeMapping.MimeUtility.GetMimeMapping(sFilePath);
                    return File(b, sContentType);
                }
                else
                {
                    return StatusCode(404, $"Document not found: {id}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    }
}
