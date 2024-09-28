using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Jpeg;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace BioMap
{
#pragma warning disable IDE1006 // Naming Styles
  [System.Diagnostics.DebuggerDisplay("{ToString()}")]
  public class Element
  {
    public override string ToString() {
      return "Element(" + (this.HasIndivData() ? $"#{this.GetIIdAsInt()}, " : "") + $"{this.ElementName})";
    }
    public class SymbolProperties
    {
      public double Radius;
      public string BgColor;
      public string FgColor;
    }
    public class UploadInfo_t
    {
      public DateTime Timestamp;
      public string UserId;
      public string Comment;
    }
    [JsonObject(MemberSerialization.Fields)]
    public class MarkerInfo_t
    {
      public LatLng position;
      public string PlaceName;
    }
    [JsonObject(MemberSerialization.Fields)]
    public class ExifData_t
    {
      public string Make;
      public string Model;
      public DateTime? DateTimeOriginal;
    }
    [JsonObject(MemberSerialization.Fields)]
    public class IndivData_t
    {
      [JsonObject(MemberSerialization.Fields)]
      public class MeasuredData_t
      {
        public double HeadBodyLength;
        public double ShareOfBlack;
        public double CenterOfMass;
        public double StdDeviation;
        public double Entropy;
        public double Granularity => (this.Entropy / Math.Max(0.01, this.ShareOfBlack));
        public double Mass;
      }
      public int IId;
      public string GenderFeature;
      public string Gender;
      public int CaptivityBred;
      public int PhenotypeIdx;
      public int GenotypeIdx;
      public DateTime DateOfBirth;
      public MeasuredData_t MeasuredData;
    }
    [JsonObject(MemberSerialization.Fields)]
    public class ElementProp_t
    {
      public DateTime CreationTime;
      public MarkerInfo_t MarkerInfo;
      public UploadInfo_t UploadInfo;
      public ExifData_t ExifData;
      public IndivData_t IndivData;
    }
    public readonly string Project;
    public string ElementName;
    public ElementProp_t ElementProp;
    public ElementClassification Classification { get; init; } = new ElementClassification();
    public bool CroppingConfirmed = false;
    public Blazor.ImageSurveyor.ImageSurveyorMeasureData MeasureData = null;
    public bool HasImageButNoOrigImage(SessionData sd) {
      DataService ds = sd.DS;
      if (
        System.IO.File.Exists(ds.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, false))
        &&
        !System.IO.File.Exists(ds.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, true))
        ) {
        return true;
      }
      return false;
    }
    public bool HasOrigImageButNoImage(SessionData sd) {
      DataService ds = sd.DS;
      if (
        !System.IO.File.Exists(ds.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, false))
        &&
        System.IO.File.Exists(ds.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, true))
        ) {
        return true;
      }
      return false;
    }
    //
    public Element(string sProject) {
      this.Project = sProject;
    }
    public static Element CreateFromImageFile(string sProject, string sImageFilePath, SessionData sd) {
      using (var sImageStream = new System.IO.FileStream(sImageFilePath, System.IO.FileMode.Open)) {
        string sElementName = System.IO.Path.GetFileName(sImageFilePath);
        return CreateFromImageFile(sProject, sImageStream, sElementName, sd);
      }
    }
    public static Element CreateFromImageFile(string sProject, System.IO.Stream sImageStream, string sElementName, SessionData sd) {
      IReadOnlyList<Directory> metaData = ImageMetadataReader.ReadMetadata(sImageStream);
      ExifIfd0Directory ifd0Directory = metaData.OfType<ExifIfd0Directory>().FirstOrDefault();
      ExifSubIfdDirectory subIfdDirectory = metaData.OfType<ExifSubIfdDirectory>().FirstOrDefault();
      GpsDirectory gpsDirectory = metaData.OfType<GpsDirectory>().FirstOrDefault();
      GeoLocation geoLocation = gpsDirectory?.GetGeoLocation();
      var el = new Element(sProject);
      el.ElementName = sElementName;
      el.ElementProp = new ElementProp_t();
      el.ElementProp.UploadInfo = new Element.UploadInfo_t {
        Timestamp = DateTime.Now,
        UserId = sd.CurrentUser.EMail,
      };
      el.ElementProp.MarkerInfo = new MarkerInfo_t();
      if (geoLocation != null) {
        el.ElementProp.MarkerInfo.position = new LatLng {
          lat = geoLocation.Latitude,
          lng = geoLocation.Longitude,
        };
        el.ElementProp.MarkerInfo.PlaceName = Place.GetNearestPlace(sd, el.ElementProp.MarkerInfo.position)?.Name;
      }
      el.ElementProp.ExifData = new ExifData_t();
      el.TryExtractOriginalTimeFromExif(subIfdDirectory);
      el.ElementProp.CreationTime = ((el.ElementProp.ExifData.DateTimeOriginal.HasValue) ? el.ElementProp.ExifData.DateTimeOriginal.Value : el.ElementProp.UploadInfo.Timestamp);
      if (ifd0Directory != null) {
        el.ElementProp.ExifData.Make = ifd0Directory.GetString(ExifDirectoryBase.TagMake);
        el.ElementProp.ExifData.Model = ifd0Directory.GetString(ExifDirectoryBase.TagModel);
      }
      return el;
    }
    public void AdjustTimeFromPhoto(SessionData sd) {
      if (this.HasPhotoData()) {
        string sFilePath = PhotoController.GetFilePathForExistingImage(sd.DS, sd.CurrentUser.Project, this.ElementName, true);
        if (System.IO.File.Exists(sFilePath)) {
          System.IO.FileStream sImageStream = null;
          try {
            sImageStream = new System.IO.FileStream(sFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            IReadOnlyList<Directory> metaData = ImageMetadataReader.ReadMetadata(sImageStream);
            ExifSubIfdDirectory subIfdDirectory = metaData.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            Element el = this;
            if (this.TryExtractOriginalTimeFromExif(subIfdDirectory)) {
              el.ElementProp.CreationTime = el.ElementProp.ExifData.DateTimeOriginal.Value;
            }
          } catch {
          } finally {
            if (sImageStream != null) {
              sImageStream.Close();
            }
          }
        }
      }
    }
    private bool TryExtractOriginalTimeFromExif(ExifSubIfdDirectory subIfdDirectory) {
      if (subIfdDirectory != null && subIfdDirectory.ContainsTag(ExifDirectoryBase.TagDateTimeOriginal)) {
        string sDateTimeOriginal = subIfdDirectory.GetString(ExifDirectoryBase.TagDateTimeOriginal);
        if (sDateTimeOriginal != null && sDateTimeOriginal.Length >= 19) {
          sDateTimeOriginal = sDateTimeOriginal[..10].Replace(":", "-") + sDateTimeOriginal[10..];
          if (DateTimeOffset.TryParse(sDateTimeOriginal, out DateTimeOffset dateTimeOffset)) {
            //if (dateTimeOffset.Offset.TotalSeconds == 0)
            //{
            //    var sTimeZone = subIfdDirectory.GetString(ExifDirectoryBase.TagTimeZoneOriginal);
            //    if (!string.IsNullOrEmpty(sTimeZone) && TimeSpan.TryParse(sTimeZone.Replace("+", ""), out var tsOffset))
            //    {
            //        dateTimeOffset = new DateTimeOffset(dateTimeOffset.DateTime, tsOffset);
            //    }
            //}
            this.ElementProp.ExifData.DateTimeOriginal = dateTimeOffset.LocalDateTime;
            return true;
          }
        }
      }
      return false;
    }
    /// <summary>
    /// Set classification and initialize measure data including normalizer.
    /// </summary>
    /// <param name="sd">Session Data.</param>
    /// <param name="sNewClass">The new classification.</param>
    /// <param name="bSetNormalizerFromProject">Initialize normalizer from project settings even if normalizer type is not changed.</param>
    public void InitMeasureData(SessionData sd, string sNewClass, bool bSetNormalizerFromProject) {
      DataService DS = sd.DS;
      bool bPrevNormed = ElementClassification.IsNormed(this?.Classification?.ClassName);
      bool bNewNormed = ElementClassification.IsNormed(sNewClass);
      this.Classification.ClassName = sNewClass;
      if (bSetNormalizerFromProject || bNewNormed != bPrevNormed || this.MeasureData?.normalizer == null) {
        string sSrcFile = DS.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, true);
        if (System.IO.File.Exists(sSrcFile)) {
          using (var imgSrc = Image.Load(sSrcFile)) {
            imgSrc.Mutate(x => x.AutoOrient());
            if (bNewNormed) {
              Blazor.ImageSurveyor.ImageSurveyorNormalizer normalizer = sd.CurrentProject.ImageNormalizer;
              this.MeasureData = new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
                normalizer = normalizer,
                normalizePoints = normalizer.GetDefaultNormalizePoints(imgSrc.Width, imgSrc.Height).ToArray(),
                measurePoints = normalizer.GetDefaultMeasurePoints(imgSrc.Width, imgSrc.Height).ToArray(),
              };
            } else {
              var normalizer = new Blazor.ImageSurveyor.ImageSurveyorNormalizer("CropRectangle");
              this.MeasureData = new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
                normalizer = normalizer,
                normalizePoints = normalizer.GetDefaultNormalizePoints(imgSrc.Width, imgSrc.Height).ToArray(),
                measurePoints = normalizer.GetDefaultMeasurePoints(imgSrc.Width, imgSrc.Height).ToArray(),
              };
            }
          }
        }
      }
    }
    public DateTime? GetDateOfBirth() {
      return this.ElementProp.IndivData?.DateOfBirth;
    }
    public string GetDateOfBirthAsString() {
      DateTime? dob = this.GetDateOfBirth();
      if (dob.HasValue) {
        return ConvInvar.ToString(dob.Value, false);
      }
      return "";
    }
    public int? GetYearOfBirth() {
      return this.ElementProp.IndivData?.DateOfBirth.Year;
    }
    public string GetYearOfBirthAsString() {
      int? yob = this.GetYearOfBirth();
      if (yob.HasValue) {
        return ConvInvar.ToString(yob.Value);
      }
      return "";
    }
    public static string GetColorForYearOfBirth(int? nYearOfBirth) {
      string sColor = "rgba(100,100,100,0.5)";
      if (nYearOfBirth.HasValue) {
        if ((nYearOfBirth.Value % 5) == 0) {
          sColor = "rgba(255,0,0,0.5)";
        } else if ((nYearOfBirth.Value % 5) == 1) {
          sColor = "rgba(0,200,0,0.5)";
        } else if ((nYearOfBirth.Value % 5) == 2) {
          sColor = "rgba(0,0,255,0.5)";
        } else if ((nYearOfBirth.Value % 5) == 3) {
          sColor = "rgba(127,0,127,0.5)";
        } else if ((nYearOfBirth.Value % 5) == 4) {
          sColor = "rgba(0,100,127,0.5)";
        }
      }
      return sColor;
    }
    public string GetColorForYearOfBirth() {
      return GetColorForYearOfBirth(this.GetYearOfBirth());
    }
    public string GetDetails() {
      var sb = new System.Text.StringBuilder();
      sb.Append(this.GetPlaceName());
      if (this.ElementProp.IndivData != null && this.Classification.IsIdAnyPhoto()) {
        sb.Append(", #");
        sb.Append(this.GetIId());
        sb.Append(", ");
        sb.Append(this.Gender);
        sb.Append(", ");
        sb.Append(this.GetHeadBodyLengthNice());
      }
      return sb.ToString();
    }
    public MarkupString GetTraitTable(string sClass, string sStyle, Func<string, string> localize = null) {
      if (localize == null) {
        localize = (t) => t;
      }
      var sb = new System.Text.StringBuilder();
      if (this.ElementProp?.IndivData?.MeasuredData != null) {
        string[][] saaRows = new string[][] {
                new string[] { localize("Share of yellow"), ((1-this.ElementProp.IndivData.MeasuredData.ShareOfBlack)*100).ToString("0.0") + "%" },
                new string[] { localize("Asymmetry"), (this.ElementProp.IndivData.MeasuredData.CenterOfMass*100).ToString("0.0") + "%" },
                new string[] { localize("Standard deviation"), (this.ElementProp.IndivData.MeasuredData.StdDeviation*100).ToString("0.0") + "%" },
                new string[] { localize("Entropy"), (this.ElementProp.IndivData.MeasuredData.Entropy*100).ToString("0.0") + "%" },
                new string[] { localize("Granularity"), (this.ElementProp.IndivData.MeasuredData.Granularity*100).ToString("0.00") + "%" },
            };
        sb.Append("<table class=\"" + sClass + "\" style=\"" + sStyle + "\">");
        sb.Append("<tbody>");
        foreach (string[] saRow in saaRows) {
          sb.Append("<tr>");
          sb.Append("<td>");
          sb.Append(saRow[0]);
          sb.Append("</td>");
          sb.Append("<td>");
          sb.Append(saRow[1]);
          sb.Append("</td>");
          sb.Append("</tr>");
        }
        sb.Append("</tbody>");
        sb.Append("</table>");
      }
      return new MarkupString(sb.ToString());
    }
    public string GetClassName() {
      return this.Classification.ClassName;
    }
    public string GetClassOrIId() {
      int? nIId = this.GetIIdAsInt();
      if (nIId.HasValue && nIId.Value >= 1) {
        return "#" + nIId.Value;
      } else {
        return this.GetClassName();
      }
    }
    public string GetClassColor() {
      string sClass = this.GetClassName();
      if (!ElementClassification.ClassColors.TryGetValue(sClass, out string sColor)) {
        sColor = "#DD00DD";
      }
      return sColor;
    }
    public string GetIId() {
      if (this.ElementProp.IndivData != null) {
        return ConvInvar.ToString(this.ElementProp.IndivData.IId);
      }
      return "";
    }
    public int? GetIIdAsInt() { return this.ElementProp.IndivData?.IId; }
    public double GetAgeYears() {
      if (this.ElementProp.IndivData != null) {
        return (this.ElementProp.CreationTime - this.ElementProp.IndivData.DateOfBirth).TotalDays / 365;
      }
      return 0;
    }
    public int GetWinters() {
      if (this.ElementProp.IndivData != null) {
        return (int)Math.Max(0, this.ElementProp.CreationTime.Year - this.ElementProp.IndivData.DateOfBirth.Year);
      }
      return 0;
    }
    public string GetIsoDate() {
      return this.ElementProp.CreationTime.ToString("yyyy-MM-dd");
    }
    public string GetIsoDateTime() {
      return this.ElementProp.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
    public string GetPlaceName() {
      return this.ElementProp.MarkerInfo.PlaceName ?? "";
    }

    /// <summary>
    /// The gender 'f', 'm' or 'j', optionally followed by the gender feature 'm', 'f' or '-' in parantheses if gender and feature don't match.
    /// </summary>
    public string GetGenderFull(SessionData sd) {
      if (this.ElementProp.IndivData != null) {
        string sGender = this.ElementProp.IndivData.Gender;
        string sResult = sGender;
        string sFeature = this.ElementProp.IndivData.GenderFeature;
        if (sGender != sFeature && !(sGender == "j" && sFeature == "-")) {
          if (
            (sd.CurrentProject.FemaleGenderFeatures && sGender == "f")
            ||
            (sd.CurrentProject.MaleGenderFeatures && sGender == "m")
            ) {
            sResult += "(" + sFeature + ")";
          }
        }
        return sResult;
      }
      return "";
    }
    public string Gender {
      get {
        if (this.ElementProp.IndivData != null) {
          return this.ElementProp.IndivData.Gender;
        }
        return "";
      }
      set {
        if (this.ElementProp.IndivData != null) {
          this.ElementProp.IndivData.Gender = value;
        }
      }
    }
    public string GenderFeature {
      get {
        if (this.ElementProp.IndivData != null) {
          return this.ElementProp.IndivData.GenderFeature;
        }
        return "";
      }
      set {
        if (this.ElementProp.IndivData != null) {
          this.ElementProp.IndivData.GenderFeature = value;
        }
      }
    }
    public bool TryDetermineGender(SessionData sd, Element[] prevElements, out string sGender) {
      if (sd.CurrentProject.MaleGenderFeatures && this.GenderFeature == "m") {
        sGender = "m";
      } else if (sd.CurrentProject.FemaleGenderFeatures && this.GenderFeature == "f") {
        sGender = "f";
      } else if (!sd.CurrentProject.FemaleGenderFeatures && this.GenderFeature == "-") {
        if (this.GetHeadBodyLengthMm() < sd.CurrentProject.AdultMinLength) {
          sGender = "j";
        } else {
          sGender = "f";
        }
      } else if (!sd.CurrentProject.MaleGenderFeatures && this.GenderFeature == "-") {
        if (this.GetHeadBodyLengthMm() < sd.CurrentProject.AdultMinLength) {
          sGender = "j";
        } else {
          sGender = "m";
        }
      } else {
        sGender = "";
      }
      if (prevElements != null) {
        foreach (Element el in prevElements) {
          if (!(el.Gender == sGender || el.Gender == "j")) {
            return false;
          }
        }
      }
      return true;
    }
    public bool HasPhotoData() {
      return (!string.IsNullOrEmpty(this.ElementProp.ExifData?.Make) || !string.IsNullOrEmpty(this.ElementProp.ExifData?.Model) || this.ElementProp.ExifData?.DateTimeOriginal != null);
    }
    public bool HasGeoLocation() {
      return (this.ElementProp.MarkerInfo?.position != null);
    }
    public bool HasIndivData() {
      return (this.Classification.IsIdAnyPhoto() && this.ElementProp.IndivData != null);
    }
    public bool HasMeasuredData() {
      return (this.Classification.IsIdAnyPhoto() && this.ElementProp.IndivData?.MeasuredData != null);
    }
    public double GetHeadBodyLengthMm() {
      if (this.ElementProp.IndivData?.MeasuredData != null) {
        return this.ElementProp.IndivData.MeasuredData.HeadBodyLength;
      }
      return 0;
    }
    public double? GetConditionIndex(SessionData sd) {
      if (this.ElementProp.IndivData?.MeasuredData != null) {
        double L = this.ElementProp.IndivData.MeasuredData.HeadBodyLength;
        double M = this.ElementProp.IndivData.MeasuredData.Mass;
        if (L > 0 && M > 0) {
          double CI = M * 1000000 / (L * L * L);
          return CI;
        }
      }
      return null;
    }
    public string GetHeadBodyLengthNice() {
      if (this.ElementProp.IndivData?.MeasuredData != null) {
        return ConvInvar.ToDecimalString(this.ElementProp.IndivData.MeasuredData.HeadBodyLength, 1) + " mm";
      }
      return "";
    }
    public double GetMass() {
      if (this.ElementProp.IndivData?.MeasuredData != null) {
        return this.ElementProp.IndivData.MeasuredData.Mass;
      }
      return 0;
    }
    public string GetMassNice() {
      if (this.ElementProp.IndivData?.MeasuredData != null) {
        return ConvInvar.ToDecimalString(this.ElementProp.IndivData.MeasuredData.Mass, 1) + " g";
      }
      return "";
    }
    public SymbolProperties GetSymbolProperties(SessionData sd) {
      double dHBL = this.GetHeadBodyLengthMm() * 60 / sd.CurrentProject.MaxHeadBodyLength;
      string bgColor = this.GetClassColor();
      var sb = new SymbolProperties {
        Radius = (dHBL == 0) ? 3 : (dHBL * 0.10),
        BgColor = bgColor,
        FgColor = bgColor,
      };
      return sb;
    }
    public static int CompareByDateTime(Element a, Element b) {
      return DateTime.Compare(a.ElementProp.CreationTime, b.ElementProp.CreationTime);
    }
    public static int CompareByIId(Element a, Element b) {
      int? ai = a.GetIIdAsInt();
      int? bi = b.GetIIdAsInt();
      if (ai.HasValue) {
        if (bi.HasValue) {
          if (ai.Value == bi.Value) {
            return CompareByDateTime(a, b);
          } else {
            return ai.Value - bi.Value;
          }
        } else {
          return 1;
        }
      } else {
        if (bi.HasValue) {
          return -1;
        } else {
          return 0;
        }
      }
    }
    public static double? CalcDistance(Element el1, Element el2) {
      LatLng ll1 = el1?.ElementProp?.MarkerInfo?.position;
      LatLng ll2 = el2?.ElementProp?.MarkerInfo?.position;
      if (ll1 != null && ll2 != null) {
        return GeoCalculator.GetDistance(ll2, ll1);
      }
      return null;
    }
    public double CalcSimilarity(SessionData sd, Element other) {
      Element el1 = this;
      Element el2 = other;
      if (el1?.ElementProp?.IndivData?.MeasuredData == null || el2?.ElementProp?.IndivData?.MeasuredData == null) {
        return -10;
      }
      double fSimilarity = 0;
      // Der identische Fang kann es nicht sein.
      if (string.CompareOrdinal(el1.ElementName, el2.ElementName) == 0) {
        fSimilarity += -5000;
      }
      // Pattern Matching.
      if (sd.CurrentProject.Identification.PatternMatchingWeight > 0f) {
        fSimilarity += sd.CurrentProject.Identification.PatternMatchingWeight * Utilities.GetPatternMatching(el1, el2, sd);
      }
      // Zeitlich sortieren (y(oung) / o(ld)).
      DateTime d1 = el1.ElementProp.CreationTime;
      DateTime d2 = el2.ElementProp.CreationTime;
      int nAgeDiffDays = (int)(d2 - d1).TotalDays;
      Element ely = el1;
      Element elo = el2;
      {
        if (nAgeDiffDays < 0) {
          ely = el2;
          elo = el1;
          nAgeDiffDays = -nAgeDiffDays;
        }
      }
      // Örtlicher Abstand.
      double? distance = Element.CalcDistance(el1, el2);
      if (!distance.HasValue) {
      } else if (nAgeDiffDays == 0) {
        // Am selben Tag gefangen -> müssen unterschiedlich sein.
        fSimilarity -= sd.CurrentProject.Identification.LocationWeight;
      } else {
        double score = Math.Max(-1, Math.Min(1, (100 / Math.Max(20, distance.Value)) - 1));
        fSimilarity += sd.CurrentProject.Identification.LocationWeight * score;
      }
      // Geschlecht.
      string g1 = el1.Gender;
      string g2 = el2.Gender;
      if (sd.CurrentProject.MaleGenderFeatures && sd.CurrentProject.FemaleGenderFeatures) {
        if ((g1 == "m" && g2 == "m") || (g1 == "w" && g2 == "w")) {
          fSimilarity += sd.CurrentProject.Identification.GenderWeight;
        } else if ((g1 == "m" && g2 == "f") || (g1 == "f" && g2 == "m")) {
          fSimilarity += -0.5 * sd.CurrentProject.Identification.GenderWeight;
        } else {
          fSimilarity += 0.5 * sd.CurrentProject.Identification.GenderWeight;
        }
      } else if (sd.CurrentProject.MaleGenderFeatures && !sd.CurrentProject.FemaleGenderFeatures) {
        if (g1 == "m" && g2 == "m") {
          fSimilarity += sd.CurrentProject.Identification.GenderWeight;
        } else if ((g1 == "m" && g2 == "f") || (g1 == "f" && g2 == "m")) {
          fSimilarity += -0.5 * sd.CurrentProject.Identification.GenderWeight;
        } else {
          fSimilarity += 0.5 * sd.CurrentProject.Identification.GenderWeight;
        }
      } else if (!sd.CurrentProject.MaleGenderFeatures && sd.CurrentProject.FemaleGenderFeatures) {
        if (g1 == "w" && g2 == "w") {
          fSimilarity += sd.CurrentProject.Identification.GenderWeight;
        } else if ((g1 == "m" && g2 == "f") || (g1 == "f" && g2 == "m")) {
          fSimilarity += -0.5 * sd.CurrentProject.Identification.GenderWeight;
        } else {
          fSimilarity += 0.5 * sd.CurrentProject.Identification.GenderWeight;
        }
      }

      if (this.GetIIdAsInt() == 1132) {
        int iii = 0;
      }

      // Kopf-Rumpf-Länge.
      {
        double ly = ely.GetHeadBodyLengthMm();
        double lo = elo.GetHeadBodyLengthMm();
        GrowthFunc growthFunc;
        if (ely.GetDateOfBirth().HasValue) {
          growthFunc = new GrowthFunc() {
            DateOfBirth = ely.GetDateOfBirth().Value,
          };
        } else {
          growthFunc = GrowthFunc.FromCatches(new Element[] { ely });
        }
        double expectedSize = growthFunc.GetSize(elo.ElementProp.CreationTime);
        double sizeDeviation = (lo - expectedSize);
        if (sizeDeviation < -12 || sizeDeviation > 10) {
          fSimilarity += -500 - Math.Abs(sizeDeviation) * 10;
        } else {
          double sizeRelDeviation = sizeDeviation / Math.Max(expectedSize, Math.Min(ly, lo));
          double sizeSimilarity = Math.Max(0, Math.Min(1, 0.10 / Math.Max(0.00001, Math.Abs(sizeRelDeviation))));
          fSimilarity += sd.CurrentProject.Identification.LengthWeight * sizeSimilarity;
        }
      }
      // Merkmalsabstand.
      IndivData_t.MeasuredData_t md1 = el1.ElementProp.IndivData?.MeasuredData;
      IndivData_t.MeasuredData_t md2 = el2.ElementProp.IndivData?.MeasuredData;
      if (md1 != null && md2 != null) {
        double fTraitScore = 0f;
        fTraitScore += Utilities.CalcTraitScore(md1.ShareOfBlack, md2.ShareOfBlack, 0.20);
        fTraitScore += Utilities.CalcTraitScore(md1.CenterOfMass, md2.CenterOfMass, 0.10);
        fTraitScore += Utilities.CalcTraitScore(md1.StdDeviation, md2.StdDeviation, 0.07);
        fTraitScore += Utilities.CalcTraitScore(md1.Entropy, md2.Entropy, 0.05);
        fTraitScore += Utilities.CalcTraitScore(md1.Granularity, md2.Granularity, 0.05);
        fSimilarity += sd.CurrentProject.Identification.TraitsWeight * fTraitScore / 5;
      }
      //Trait.GetAllTraits().forEach((trait) =>
      //{
      //    let v1 = el1.getTraitValue(trait.Id);
      //    let v2 = el2.getTraitValue(trait.Id);
      //    fSimilarity += trait.GetValueSimilarity(v1, v2);
      //});
      return fSimilarity;
    }
    public static (List<Element>, Dictionary<Element, double>) GetPrunedListSortedBySimilarity(SessionData sd, IEnumerable<Element> els, Element sample) {
      var dictSimilarities = new Dictionary<Element, double>();
      foreach (Element el in els) {
        double dSim = el.CalcSimilarity(sd, sample);
        dictSimilarities.Add(el, dSim);
      }
      var lListToSort = new List<Element>(els);
      lListToSort.Sort((a, b) => {
        double simA = dictSimilarities[a];
        double simB = dictSimilarities[b];
        if (simA > simB) {
          return -1;
        } else if (simA < simB) {
          return 1;
        } else {
          return Element.CompareByIId(a, b);
        }
      });
      var lIidList = new List<int>();
      var lPrunedList = new List<Element>();
      foreach (Element el in lListToSort) {
        int? iid = el.GetIIdAsInt();
        if (iid.HasValue) {
          if (!lIidList.Contains(iid.Value)) {
            //if (dictSimilarities[el] >= -10) {
              lIidList.Add(iid.Value);
              lPrunedList.Add(el);
            //}
          }
        }
      }
      return (lPrunedList, dictSimilarities);
    }
  }
#pragma warning restore IDE1006 // Naming Styles
}
