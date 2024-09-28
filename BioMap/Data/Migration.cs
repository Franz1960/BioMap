using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using BioMap.Pages.Lists;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
  public class Migration
  {
#pragma warning disable CS0649
    internal class ProtocolContent_t
    {
      public string Timestamp;
      public string Author;
      public string Text;
    }
    internal class Protocol_t
    {
      public string Name;
      public ProtocolContent_t Content;
    }
    static string ToString(double d) {
      return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    public static async Task MigrateGenders(SessionData sd, Action<int> callbackCompletion) {
      await Task.Run(() => {
        DataService ds = sd.DS;
        Element[] aElements = ds.GetElements(sd);
        int nLoopCnt = 0;
        foreach (Element el in aElements) {
          callbackCompletion(((nLoopCnt++) * 100) / aElements.Length);
          if (el.HasIndivData()) {
            string sOldGender = el.ElementProp?.IndivData?.Gender;
            if (sOldGender != null) {
              el.ElementProp.IndivData.Gender =
                sOldGender.StartsWith("f") ? "f" :
                sOldGender.StartsWith("m") ? "m" :
                sOldGender.StartsWith("j") ? "j" :
                "";
              string sOldGenderFeature = el.ElementProp?.IndivData?.GenderFeature;
              if (string.IsNullOrEmpty(sOldGenderFeature)) {
                string sGender = el.ElementProp.IndivData.Gender;
                if (sGender == "m") {
                  el.ElementProp.IndivData.GenderFeature = "m";
                } else {
                  el.ElementProp.IndivData.GenderFeature = "-";
                }
              }
              ds.WriteElement(sd, el);
            }
          }
        }
      });
    }
    public static async Task MigrateGenderFeatures(SessionData sd, Action<int> callbackCompletion) {
      await Task.Run(() => {
        DataService ds = sd.DS;
        Element[] aElements = ds.GetElements(sd);
        int nLoopCnt = 0;
        foreach (Element el in aElements) {
          callbackCompletion(((nLoopCnt++) * 100) / aElements.Length);
          string sGender = el.ElementProp?.IndivData?.Gender;
          if ((sGender == "f" || sGender == "j") && string.IsNullOrEmpty(el.ElementProp?.IndivData?.GenderFeature)) {
            el.ElementProp.IndivData.GenderFeature = "-";
            ds.WriteElement(sd, el);
          }
        }
      });
    }
    public static async Task MigrateImageSize(SessionData sd, Action<int> callbackCompletion) {
      await Task.Run(() => {
        DataService ds = sd.DS;
        string sDataDir = ds.GetDataDir(sd);
        ds.AddLogEntry(sd, "Migrating data");
        string sMigSrcDir = System.IO.Path.Combine(sDataDir, "migration_source");
        Element[] aElements = ds.GetElements(sd);
        int nLoopCnt = 0;
        foreach (Element el in aElements) {
          callbackCompletion(((nLoopCnt++) * 100) / aElements.Length);
          if (el.ElementProp?.IndivData?.MeasuredData != null) {
            try {
              string sMigSrcFilePath = System.IO.Path.Combine(sMigSrcDir, "elements", el.ElementName + ".orig.jpg");
              if (System.IO.File.Exists(sMigSrcFilePath)) {
                string sFilePath = ds.GetFilePathForImage(sd.CurrentUser.Project, el.ElementName, true);
                if (System.IO.File.Exists(sFilePath)) {
                  using (var imgMigSrc = Image.Load(sMigSrcFilePath)) {
                    using (var img = Image.Load(sFilePath)) {
                      float fScale = ((float)img.Width) / imgMigSrc.Width;
                      //el.ElementProp.IndivData.MeasuredData.OrigHeadPosition.X*=fScale;
                      //el.ElementProp.IndivData.MeasuredData.OrigHeadPosition.Y*=fScale;
                      //el.ElementProp.IndivData.MeasuredData.OrigBackPosition.X*=fScale;
                      //el.ElementProp.IndivData.MeasuredData.OrigBackPosition.Y*=fScale;
                      //for (int i=0;i<3;i++) {
                      //  el.ElementProp.IndivData.MeasuredData.PtsOnCircle[i].X*=fScale;
                      //  el.ElementProp.IndivData.MeasuredData.PtsOnCircle[i].Y*=fScale;
                      //}
                      //ds.WriteElement(sd,el);
                    }
                  }
                }
              }
            } catch { }
          }
        }
      });
    }
    public static void CopyImageCompressed(string sSrcFile, string sDestFile, bool bSaveGpsDataInOriginalImages, int nBiggerDim = 1200) {
      using (var imgSrc = Image.Load(sSrcFile)) {
        int nReqHeight;
        int nReqWidth;
        if (imgSrc.Height < imgSrc.Width) {
          nReqWidth = Math.Min(nBiggerDim, imgSrc.Width);
          nReqHeight = (nReqWidth * imgSrc.Height) / imgSrc.Width;
        } else {
          nReqHeight = Math.Min(nBiggerDim, imgSrc.Height);
          nReqWidth = (nReqHeight * imgSrc.Width) / imgSrc.Height;
        }
        imgSrc.Mutate(x => x.Resize(nReqWidth, nReqHeight));
        // GPS-Daten löschen.
        if (imgSrc?.Metadata?.ExifProfile != null && !bSaveGpsDataInOriginalImages) {
          imgSrc.Metadata.ExifProfile.Parts = SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifParts.ExifTags | SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifParts.IfdTags;
        }
        imgSrc.SaveAsJpeg(sDestFile);
      }
    }
  }
}
