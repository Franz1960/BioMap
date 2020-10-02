using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Jpeg;
using System.Linq;

namespace BioMap
{
  public class Element
  {
    public class UploadInfo_t
    {
      public DateTime Timestamp;
      public string UserId;
    }
    [JsonObject(MemberSerialization.Fields)]
    public class MarkerInfo_t
    {
      public int category;
      public LatLng position;
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
        public System.Numerics.Vector2 HeadPosition = new System.Numerics.Vector2(300, 100);
        public System.Numerics.Vector2 BackPosition = new System.Numerics.Vector2(300, 500);
        public double HeadBodyLength;
        public System.Numerics.Vector2 OrigHeadPosition = new System.Numerics.Vector2(300, 100);
        public System.Numerics.Vector2 OrigBackPosition = new System.Numerics.Vector2(300, 500);
        public System.Numerics.Vector2[] PtsOnCircle = { new System.Numerics.Vector2(300, 300), new System.Numerics.Vector2(400, 400), new System.Numerics.Vector2(500, 300) };
      }
      public int IId;
      public string Gender;
      public int YearOfBirth;
      public MeasuredData_t MeasuredData;
      public Dictionary<string, int> TraitValues = new Dictionary<string, int>();
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
    public string ElementName;
    public ElementProp_t ElementProp;
    public int? SpeciesId;
    public int? ProjectId;
    //
    public static Element CreateFromImageFile(string sImageFilePath) {
      var metaData = ImageMetadataReader.ReadMetadata(sImageFilePath);
      var ifd0Directory = metaData.OfType<ExifIfd0Directory>().FirstOrDefault();
      var subIfdDirectory = metaData.OfType<ExifSubIfdDirectory>().FirstOrDefault();
      var gpsDirectory = metaData.OfType<GpsDirectory>().FirstOrDefault();
      var geoLocation = gpsDirectory.GetGeoLocation();
      var el = new Element();
      el.ElementName = System.IO.Path.GetFileName(sImageFilePath);
      el.ElementProp=new ElementProp_t();
      el.ElementProp.UploadInfo=new Element.UploadInfo_t
      {
        Timestamp = DateTime.Now,
        UserId = DataService.Instance.CurrentUser.EMail,
      };
      el.ElementProp.MarkerInfo=new MarkerInfo_t();
      el.ElementProp.MarkerInfo.category=100;
      //if (metaData.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal,out var dateTime))
      if (geoLocation!=null) {
        el.ElementProp.MarkerInfo.position=new LatLng
        {
          lat=geoLocation.Latitude,
          lng=geoLocation.Longitude,
        };
      }
      el.ElementProp.ExifData=new ExifData_t();
      if (subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal,out var dateTime)) {
        el.ElementProp.ExifData.DateTimeOriginal=dateTime;
      }
      if (ifd0Directory!=null) {
        el.ElementProp.ExifData.Make=ifd0Directory.GetString(ExifDirectoryBase.TagMake);
        el.ElementProp.ExifData.Model=ifd0Directory.GetString(ExifDirectoryBase.TagModel);
      }
      el.ElementProp.CreationTime = ((el.ElementProp.ExifData.DateTimeOriginal.HasValue) ? el.ElementProp.ExifData.DateTimeOriginal.Value : el.ElementProp.UploadInfo.Timestamp);
      return el;
    }
    public int? GetYearOfBirth() {
      return this.ElementProp.IndivData?.YearOfBirth;
    }
    public string GetYearOfBirthAsString() {
      var yob = this.GetYearOfBirth();
      if (yob.HasValue) {
        return ConvInvar.ToString(yob.Value);
      }
      return "";
    }
    public string GetColorForYearOfBirth() {
      var sColor = "rgba(100,100,100,0.5)";
      if (this.ElementProp.IndivData!=null) {
        if ((this.ElementProp.IndivData.YearOfBirth%5)==0) {
          sColor = "rgba(255,0,0,0.5)";
        } else if ((this.ElementProp.IndivData.YearOfBirth%5)==1) {
          sColor = "rgba(0,200,0,0.5)";
        } else if ((this.ElementProp.IndivData.YearOfBirth%5)==2) {
          sColor = "rgba(0,0,255,0.5)";
        } else if ((this.ElementProp.IndivData.YearOfBirth%5)==3) {
          sColor = "rgba(127,0,127,0.5)";
        } else if ((this.ElementProp.IndivData.YearOfBirth%5)==4) {
          sColor = "rgba(0,100,127,0.5)";
        }
      }
      return sColor;
    }
    public string GetDetails() {
      var sb = new System.Text.StringBuilder();
      if (this.ElementProp.IndivData!=null) {
        sb.Append("#"+this.ElementProp.IndivData.IId);
      }
      return sb.ToString();
    }
    public int GetCategoryNum() {
      return this.ElementProp.MarkerInfo.category;
    }
    public string GetIId() {
      if (this.ElementProp.IndivData!=null) {
        return ConvInvar.ToString(this.ElementProp.IndivData.IId);
      }
      return "";
    }
    public string GetIsoDate() {
      return this.ElementProp.CreationTime.ToString("yyyy-MM-dd");
    }
    public Place GetPlace() {
      var place = new Place { Name="" };
      return place;
    }
    public string GetGender() {
      if (this.ElementProp.IndivData!=null) {
        return this.ElementProp.IndivData.Gender;
      }
      return "";
    }
    public bool HasPhotoData() {
      return (this.ElementProp.ExifData!=null);
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
  }
}
