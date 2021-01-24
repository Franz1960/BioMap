using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Jpeg;
using System.Linq;

namespace BioMap
{
  public class Element
  {
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
      public int category;
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
      }
      public int IId;
      public string Gender;
      public DateTime DateOfBirth;
      public MeasuredData_t MeasuredData;
      public Dictionary<string,int> TraitValues = new Dictionary<string,int>();
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
    public ElementClassification Classification=new ElementClassification();
    public Blazor.ImageSurveyor.ImageSurveyorMeasureData? MeasureData=null;
    //
    public Element(string sProject) {
      this.Project=sProject;
    }
    public static Element CreateFromImageFile(string sProject,string sImageFilePath,string sUserId) {
      using (var sImageStream = new System.IO.FileStream(sImageFilePath,System.IO.FileMode.Open)) {
        var sElementName = System.IO.Path.GetFileName(sImageFilePath);
        return CreateFromImageFile(sProject,sImageStream,sElementName,sUserId);
      }
    }
    public static Element CreateFromImageFile(string sProject,System.IO.Stream sImageStream,string sElementName,string sUserId) {
      var metaData = ImageMetadataReader.ReadMetadata(sImageStream);
      var ifd0Directory = metaData.OfType<ExifIfd0Directory>().FirstOrDefault();
      var subIfdDirectory = metaData.OfType<ExifSubIfdDirectory>().FirstOrDefault();
      var gpsDirectory = metaData.OfType<GpsDirectory>().FirstOrDefault();
      var geoLocation = gpsDirectory?.GetGeoLocation();
      var el = new Element(sProject);
      el.ElementName = sElementName;
      el.ElementProp=new ElementProp_t();
      el.ElementProp.UploadInfo=new Element.UploadInfo_t {
        Timestamp = DateTime.Now,
        UserId = sUserId,
      };
      el.ElementProp.MarkerInfo=new MarkerInfo_t();
      el.ElementProp.MarkerInfo.category=100;
      //if (metaData.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal,out var dateTime))
      if (geoLocation!=null) {
        el.ElementProp.MarkerInfo.position=new LatLng {
          lat=geoLocation.Latitude,
          lng=geoLocation.Longitude,
        };
      }
      el.ElementProp.ExifData=new ExifData_t();
      if (subIfdDirectory!=null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal,out var dateTime)) {
        el.ElementProp.ExifData.DateTimeOriginal=dateTime;
      }
      if (ifd0Directory!=null) {
        el.ElementProp.ExifData.Make=ifd0Directory.GetString(ExifDirectoryBase.TagMake);
        el.ElementProp.ExifData.Model=ifd0Directory.GetString(ExifDirectoryBase.TagModel);
      }
      el.ElementProp.CreationTime = ((el.ElementProp.ExifData.DateTimeOriginal.HasValue) ? el.ElementProp.ExifData.DateTimeOriginal.Value : el.ElementProp.UploadInfo.Timestamp);
      return el;
    }
    public DateTime? GetDateOfBirth() {
      return this.ElementProp.IndivData?.DateOfBirth;
    }
    public string GetDateOfBirthAsString() {
      var dob = this.GetDateOfBirth();
      if (dob.HasValue) {
        return ConvInvar.ToString(dob.Value,false);
      }
      return "";
    }
    public int? GetYearOfBirth() {
      return this.ElementProp.IndivData?.DateOfBirth.Year;
    }
    public string GetYearOfBirthAsString() {
      var yob = this.GetYearOfBirth();
      if (yob.HasValue) {
        return ConvInvar.ToString(yob.Value);
      }
      return "";
    }
    public static string GetColorForYearOfBirth(int? nYearOfBirth) {
      var sColor = "rgba(100,100,100,0.5)";
      if (nYearOfBirth.HasValue) {
        if ((nYearOfBirth.Value%5)==0) {
          sColor = "rgba(255,0,0,0.5)";
        } else if ((nYearOfBirth.Value%5)==1) {
          sColor = "rgba(0,200,0,0.5)";
        } else if ((nYearOfBirth.Value%5)==2) {
          sColor = "rgba(0,0,255,0.5)";
        } else if ((nYearOfBirth.Value%5)==3) {
          sColor = "rgba(127,0,127,0.5)";
        } else if ((nYearOfBirth.Value%5)==4) {
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
      if (this.ElementProp.IndivData!=null) {
        sb.Append(", #");
        sb.Append(this.GetIId());
        sb.Append(", ");
        sb.Append(this.GetGender());
        sb.Append(", ");
        sb.Append(this.GetHeadBodyLengthNice());
      }
      return sb.ToString();
    }
    public string GetClassName() {
      return this.Classification.ClassName;
    }
    public string GetClassOrIId() {
      var sIId = this.GetIId();
      if (string.IsNullOrEmpty(sIId)) {
        return this.GetClassName();
      } else {
        return "#"+sIId;
      }
    }
    public string GetClassColor() {
      var sClass=this.GetClassName();
      if (!ElementClassification.ClassColors.TryGetValue(sClass,out string sColor)) {
        sColor="#DD00DD";
      }
      return sColor;
    }
    public string GetIId() {
      if (this.ElementProp.IndivData!=null) {
        return ConvInvar.ToString(this.ElementProp.IndivData.IId);
      }
      return "";
    }
    public double GetAgeYears() {
      if (this.ElementProp.IndivData!=null) {
        return (this.ElementProp.CreationTime-this.ElementProp.IndivData.DateOfBirth).TotalDays/365;
      }
      return 0;
    }
    public int GetWinters() {
      if (this.ElementProp.IndivData!=null) {
        return (int)Math.Max(0,this.ElementProp.CreationTime.Year-this.ElementProp.IndivData.DateOfBirth.Year);
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
      var sPlaceName = this.ElementProp.MarkerInfo.PlaceName;
      return sPlaceName;
    }
    public string GetGender() {
      if (this.ElementProp.IndivData!=null) {
        return this.ElementProp.IndivData.Gender;
      }
      return "";
    }
    public bool HasPhotoData() {
      return (!string.IsNullOrEmpty(this.ElementProp.ExifData?.Make) || !string.IsNullOrEmpty(this.ElementProp.ExifData?.Model));
    }
    public bool HasIndivData() {
      return (this.ElementProp.IndivData!=null);
    }
    public bool HasMeasuredData() {
      return (this.ElementProp.IndivData?.MeasuredData!=null);
    }
    public double GetHeadBodyLengthMm() {
      if (this.ElementProp.IndivData?.MeasuredData!=null) {
        return this.ElementProp.IndivData.MeasuredData.HeadBodyLength;
      }
      return 0;
    }
    public string GetHeadBodyLengthNice() {
      if (this.ElementProp.IndivData?.MeasuredData!=null) {
        return ConvInvar.ToDecimalString(this.ElementProp.IndivData.MeasuredData.HeadBodyLength,1)+" mm";
      }
      return "";
    }
    public SymbolProperties GetSymbolProperties() {
      var dHBL = this.GetHeadBodyLengthMm();
      var bgColor = this.GetClassColor();
      var sb = new SymbolProperties {
        Radius=(dHBL==0) ? 3 : (dHBL*0.10),
        BgColor=bgColor,
        FgColor=bgColor,
      };
      return sb;
    }
  }
}
