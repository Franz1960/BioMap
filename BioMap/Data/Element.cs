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
    public class Category
    {
      public Category(int nCatNum,string bgColor,string sCatName) {
        this.Num=nCatNum;
        this.NumString=ConvInvar.ToString(nCatNum);
        this.Name=sCatName;
        this.BgColor=bgColor;
      }
      public int Num { get; private set; }
      public string NumString { get; }
      public string Name { get; private set; }
      public string BgColor { get; private set; }
      public static Category[] AllCategories { get; private set; }
      public static readonly Dictionary<int,Category> CategoriesByNum = new Dictionary<int,Category>();
      static Category() {
        var l = new List<Category>();
        l.Add(new Category(100,"#FFFFFF","Neu hochgeladen"));
        l.Add(new Category(120,"#9132D1","Andere Tierart"));
        l.Add(new Category(130,"#9132D1","Sonstiges"));
        l.Add(new Category(140,"#846A00","Sonstige Kröte"));
        l.Add(new Category(141,"#846A00","Erdkröte"));
        l.Add(new Category(150,"#C96A00","Sonstiger Frosch"));
        l.Add(new Category(151,"#C96A00","Grasfrosch"));
        l.Add(new Category(152,"#CD6A00","Springfrosch"));
        l.Add(new Category(153,"#00A321","Grünfrosch"));
        l.Add(new Category(160,"#C96A00","Sonstiger Schwanzlurch"));
        l.Add(new Category(161,"#B200FF","Teichmolch"));
        l.Add(new Category(162,"#A17FFF","Bergmolch"));
        l.Add(new Category(163,"#FF7F7F","Kammmolch"));
        l.Add(new Category(164,"#FF7F7F","Feuersalamander"));
        l.Add(new Category(170,"#D730D0","Sonstiges Insekt"));
        l.Add(new Category(171,"#D730D0","Rückenschwimmer"));
        l.Add(new Category(172,"#D730D0","Wasserläufer"));
        l.Add(new Category(173,"#D730D0","Wasserskorpion"));
        l.Add(new Category(174,"#D730D0","Libellenlarve"));
        l.Add(new Category(175,"#D730D0","Gelbrandkäfer"));
        l.Add(new Category(210,"#D30000","Kein Habitat"));
        l.Add(new Category(220,"#AAC643","Potentielles Habitat ohne Unken"));
        l.Add(new Category(230,"#AAC643","Potentielles Habitat Ortsmeldung"));
        l.Add(new Category(240,"#AAC643","Potentielles Habitat Foto"));
        l.Add(new Category(242,"#B7B171","Foto mit vermutlichem Unkenlaich"));
        l.Add(new Category(244,"#B7B171","Foto mit vermutlichen Unkenquappen"));
        l.Add(new Category(320,"#A0FF70","Geprüftes Foto ohne Unken"));
        l.Add(new Category(322,"#A0FF70","Monitoring-Besuch ohne Unken"));
        l.Add(new Category(330,"#FFFF20","Geprüfte Ortsmeldung mit Unken"));
        l.Add(new Category(340,"#B7B171","Geprüftes Foto mit Unken"));
        l.Add(new Category(342,"#B7B171","Foto mit überprüftem Unkenlaich"));
        l.Add(new Category(344,"#B7B171","Foto mit überprüften Unkenquappen"));
        l.Add(new Category(346,"#000000","Foto mit überprüften Unkenkadavern"));
        l.Add(new Category(350,"#FFD800","Passbild"));
        l.Add(new Category(351,"#7F6420","Normbild Oberseite"));
        AllCategories=l.ToArray();
        CategoriesByNum.Clear();
        foreach (var category in AllCategories) {
          CategoriesByNum[category.Num]=category;
        }
      }
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
        public System.Numerics.Vector2 HeadPosition = new System.Numerics.Vector2(300,100);
        public System.Numerics.Vector2 BackPosition = new System.Numerics.Vector2(300,500);
        public double HeadBodyLength;
        public System.Numerics.Vector2 OrigHeadPosition = new System.Numerics.Vector2(300,100);
        public System.Numerics.Vector2 OrigBackPosition = new System.Numerics.Vector2(300,500);
        public System.Numerics.Vector2[] PtsOnCircle = { new System.Numerics.Vector2(300,300),new System.Numerics.Vector2(400,400),new System.Numerics.Vector2(500,300) };
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
    public ElementClassification Classification = new ElementClassification();
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
    public int GetCategoryNum() {
      return this.ElementProp.MarkerInfo.category;
    }
    public string GetCategoryNice() {
      if (Category.CategoriesByNum.TryGetValue(this.ElementProp.MarkerInfo.category,out Category category)) {
        return ConvInvar.ToString(this.ElementProp.MarkerInfo.category)+" ("+category.Name+")";
      } else {
        return ConvInvar.ToString(this.ElementProp.MarkerInfo.category)+" (???)";
      }
    }
    public string GetCategoryOrIId() {
      var sIId = this.GetIId();
      if (string.IsNullOrEmpty(sIId)) {
        return ConvInvar.ToString(this.GetCategoryNum());
      } else {
        return "#"+sIId;
      }
    }
    public string GetCategoryColor() {
      int nCat = this.GetCategoryNum();
      if (Category.CategoriesByNum.TryGetValue(nCat,out var category)) {
        return category.BgColor;
      } else {
        return "#777777";
      }
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
      var bgColor = this.GetCategoryColor();
      var sb = new SymbolProperties {
        Radius=(dHBL==0) ? 3 : (dHBL*0.10),
        BgColor=bgColor,
        FgColor=bgColor,
      };
      return sb;
    }
  }
}
