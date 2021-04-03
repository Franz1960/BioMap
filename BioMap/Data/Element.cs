using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Jpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Linq;

namespace BioMap
{
#pragma warning disable IDE1006 // Naming Styles
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
                public double ShareOfBlack;
                public double CenterOfMass;
                public double StdDeviation;
                public double Entropy;
                public double Granularity { get => (this.Entropy/Math.Max(0.01,this.ShareOfBlack)); }
            }
            public int IId;
            public string GenderFeature;
            public string Gender;
            public DateTime DateOfBirth;
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
        public readonly string Project;
        public string ElementName;
        public ElementProp_t ElementProp;
        public ElementClassification Classification = new ElementClassification();
        public bool CroppingConfirmed = false;
        public Blazor.ImageSurveyor.ImageSurveyorMeasureData? MeasureData = null;
        public bool HasImageButNoOrigImage(SessionData sd)
        {
            var ds = DataService.Instance;
            if (
              System.IO.File.Exists(ds.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, false))
              &&
              !System.IO.File.Exists(ds.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, true))
              )
            {
                return true;
            }
            return false;
        }
        public bool HasOrigImageButNoImage(SessionData sd)
        {
            var ds = DataService.Instance;
            if (
              !System.IO.File.Exists(ds.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, false))
              &&
              System.IO.File.Exists(ds.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, true))
              )
            {
                return true;
            }
            return false;
        }
        //
        public Element(string sProject)
        {
            this.Project = sProject;
        }
        public static Element CreateFromImageFile(string sProject, string sImageFilePath, SessionData sd)
        {
            using (var sImageStream = new System.IO.FileStream(sImageFilePath, System.IO.FileMode.Open))
            {
                var sElementName = System.IO.Path.GetFileName(sImageFilePath);
                return CreateFromImageFile(sProject, sImageStream, sElementName, sd);
            }
        }
        public static Element CreateFromImageFile(string sProject, System.IO.Stream sImageStream, string sElementName, SessionData sd)
        {
            var metaData = ImageMetadataReader.ReadMetadata(sImageStream);
            var ifd0Directory = metaData.OfType<ExifIfd0Directory>().FirstOrDefault();
            var subIfdDirectory = metaData.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            var gpsDirectory = metaData.OfType<GpsDirectory>().FirstOrDefault();
            var geoLocation = gpsDirectory?.GetGeoLocation();
            var el = new Element(sProject);
            el.ElementName = sElementName;
            el.ElementProp = new ElementProp_t();
            el.ElementProp.UploadInfo = new Element.UploadInfo_t
            {
                Timestamp = DateTime.Now,
                UserId = sd.CurrentUser.EMail,
            };
            el.ElementProp.MarkerInfo = new MarkerInfo_t();
            el.ElementProp.MarkerInfo.category = 100;
            //if (metaData.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal,out var dateTime))
            if (geoLocation != null)
            {
                el.ElementProp.MarkerInfo.position = new LatLng
                {
                    lat = geoLocation.Latitude,
                    lng = geoLocation.Longitude,
                };
                el.ElementProp.MarkerInfo.PlaceName = Place.GetNearestPlace(sd, el.ElementProp.MarkerInfo.position)?.Name;
            }
            el.ElementProp.ExifData = new ExifData_t();
            if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTime))
            {
                el.ElementProp.ExifData.DateTimeOriginal = dateTime;
            }
            if (ifd0Directory != null)
            {
                el.ElementProp.ExifData.Make = ifd0Directory.GetString(ExifDirectoryBase.TagMake);
                el.ElementProp.ExifData.Model = ifd0Directory.GetString(ExifDirectoryBase.TagModel);
            }
            el.ElementProp.CreationTime = ((el.ElementProp.ExifData.DateTimeOriginal.HasValue) ? el.ElementProp.ExifData.DateTimeOriginal.Value : el.ElementProp.UploadInfo.Timestamp);
            return el;
        }
        public void InitMeasureData(SessionData sd, bool bOnlyIfNotCompatible)
        {
            var DS = DataService.Instance;
            bool bPrevNormed = ElementClassification.IsNormed(this?.Classification?.ClassName);
            bool bNewNormed = (string.CompareOrdinal(this.MeasureData?.normalizer?.NormalizeMethod, sd.CurrentProject.ImageNormalizer.NormalizeMethod) == 0);
            if (!bOnlyIfNotCompatible || bNewNormed != bPrevNormed)
            {
                var sSrcFile = DS.GetFilePathForImage(sd.CurrentUser.Project, this.ElementName, true);
                if (System.IO.File.Exists(sSrcFile))
                {
                    using (var imgSrc = Image.Load(sSrcFile))
                    {
                        imgSrc.Mutate(x => x.AutoOrient());
                        if (ElementClassification.IsNormed(this.Classification?.ClassName))
                        {
                            var normalizer = sd.CurrentProject.ImageNormalizer;
                            this.MeasureData = new Blazor.ImageSurveyor.ImageSurveyorMeasureData
                            {
                                normalizer = normalizer,
                                normalizePoints = normalizer.GetDefaultNormalizePoints(imgSrc.Width, imgSrc.Height).ToArray(),
                                measurePoints = normalizer.GetDefaultMeasurePoints(imgSrc.Width, imgSrc.Height).ToArray(),
                            };
                        }
                        else
                        {
                            var normalizer = new Blazor.ImageSurveyor.ImageSurveyorNormalizer()
                            {
                                NormalizeMethod = "CropRectangle",
                            };
                            this.MeasureData = new Blazor.ImageSurveyor.ImageSurveyorMeasureData
                            {
                                normalizer = normalizer,
                                normalizePoints = normalizer.GetDefaultNormalizePoints(imgSrc.Width, imgSrc.Height).ToArray(),
                                measurePoints = normalizer.GetDefaultMeasurePoints(imgSrc.Width, imgSrc.Height).ToArray(),
                            };
                        }
                    }
                }
            }
        }
        public DateTime? GetDateOfBirth()
        {
            return this.ElementProp.IndivData?.DateOfBirth;
        }
        public string GetDateOfBirthAsString()
        {
            var dob = this.GetDateOfBirth();
            if (dob.HasValue)
            {
                return ConvInvar.ToString(dob.Value, false);
            }
            return "";
        }
        public int? GetYearOfBirth()
        {
            return this.ElementProp.IndivData?.DateOfBirth.Year;
        }
        public string GetYearOfBirthAsString()
        {
            var yob = this.GetYearOfBirth();
            if (yob.HasValue)
            {
                return ConvInvar.ToString(yob.Value);
            }
            return "";
        }
        public static string GetColorForYearOfBirth(int? nYearOfBirth)
        {
            var sColor = "rgba(100,100,100,0.5)";
            if (nYearOfBirth.HasValue)
            {
                if ((nYearOfBirth.Value % 5) == 0)
                {
                    sColor = "rgba(255,0,0,0.5)";
                }
                else if ((nYearOfBirth.Value % 5) == 1)
                {
                    sColor = "rgba(0,200,0,0.5)";
                }
                else if ((nYearOfBirth.Value % 5) == 2)
                {
                    sColor = "rgba(0,0,255,0.5)";
                }
                else if ((nYearOfBirth.Value % 5) == 3)
                {
                    sColor = "rgba(127,0,127,0.5)";
                }
                else if ((nYearOfBirth.Value % 5) == 4)
                {
                    sColor = "rgba(0,100,127,0.5)";
                }
            }
            return sColor;
        }
        public string GetColorForYearOfBirth()
        {
            return GetColorForYearOfBirth(this.GetYearOfBirth());
        }
        public string GetDetails()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(this.GetPlaceName());
            if (this.ElementProp.IndivData != null && this.Classification.IsIdPhoto())
            {
                sb.Append(", #");
                sb.Append(this.GetIId());
                sb.Append(", ");
                sb.Append(this.Gender);
                sb.Append(", ");
                sb.Append(this.GetHeadBodyLengthNice());
            }
            return sb.ToString();
        }
        public MarkupString GetTraitTable(string sClass, string sStyle, Func<string, string> localize = null)
        {
            if (localize == null)
            {
                localize = (t) => t;
            }
            var sb = new System.Text.StringBuilder();
            if (this.ElementProp?.IndivData?.MeasuredData != null)
            {
                var saaRows = new string[][] {
                new string[] { localize("Share of yellow"), ((1-this.ElementProp.IndivData.MeasuredData.ShareOfBlack)*100).ToString("0.0") + "%" },
                new string[] { localize("Asymmetry"), (this.ElementProp.IndivData.MeasuredData.CenterOfMass*100).ToString("0.0") + "%" },
                new string[] { localize("Standard deviation"), (this.ElementProp.IndivData.MeasuredData.StdDeviation*100).ToString("0.0") + "%" },
                new string[] { localize("Entropy"), (this.ElementProp.IndivData.MeasuredData.Entropy*100).ToString("0.0") + "%" },
                new string[] { localize("Granularity"), (this.ElementProp.IndivData.MeasuredData.Granularity*100).ToString("0.00") + "%" },
            };
                sb.Append("<table class=\"" + sClass + "\" style=\"" + sStyle + "\">");
                sb.Append("<tbody>");
                foreach (var saRow in saaRows)
                {
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
        public string GetClassName()
        {
            return this.Classification.ClassName;
        }
        public string GetClassOrIId()
        {
            var sIId = this.GetIId();
            if (string.IsNullOrEmpty(sIId))
            {
                return this.GetClassName();
            }
            else
            {
                return "#" + sIId;
            }
        }
        public string GetClassColor()
        {
            var sClass = this.GetClassName();
            if (!ElementClassification.ClassColors.TryGetValue(sClass, out string sColor))
            {
                sColor = "#DD00DD";
            }
            return sColor;
        }
        public string GetIId()
        {
            if (this.ElementProp.IndivData != null)
            {
                return ConvInvar.ToString(this.ElementProp.IndivData.IId);
            }
            return "";
        }
        public int? GetIIdAsInt() { return this.ElementProp.IndivData?.IId; }
        public double GetAgeYears()
        {
            if (this.ElementProp.IndivData != null)
            {
                return (this.ElementProp.CreationTime - this.ElementProp.IndivData.DateOfBirth).TotalDays / 365;
            }
            return 0;
        }
        public int GetWinters()
        {
            if (this.ElementProp.IndivData != null)
            {
                return (int)Math.Max(0, this.ElementProp.CreationTime.Year - this.ElementProp.IndivData.DateOfBirth.Year);
            }
            return 0;
        }
        public string GetIsoDate()
        {
            return this.ElementProp.CreationTime.ToString("yyyy-MM-dd");
        }
        public string GetIsoDateTime()
        {
            return this.ElementProp.CreationTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public string GetPlaceName()
        {
            var sPlaceName = this.ElementProp.MarkerInfo.PlaceName;
            return sPlaceName;
        }
        public string Gender
        {
            get
            {
                if (this.ElementProp.IndivData != null)
                {
                    return this.ElementProp.IndivData.Gender;
                }
                return "";
            }
            set
            {
                if (this.ElementProp.IndivData != null)
                {
                    this.ElementProp.IndivData.Gender = value;
                }
            }
        }
        public string GenderFeature
        {
            get
            {
                if (this.ElementProp.IndivData != null)
                {
                    return this.ElementProp.IndivData.GenderFeature;
                }
                return "";
            }
            set
            {
                if (this.ElementProp.IndivData != null)
                {
                    this.ElementProp.IndivData.GenderFeature = value;
                }
            }
        }
        public bool HasPhotoData()
        {
            return (!string.IsNullOrEmpty(this.ElementProp.ExifData?.Make) || !string.IsNullOrEmpty(this.ElementProp.ExifData?.Model));
        }
        public bool HasIndivData()
        {
            return (this.Classification.IsIdPhoto() && this.ElementProp.IndivData != null);
        }
        public bool HasMeasuredData()
        {
            return (this.Classification.IsIdPhoto() && this.ElementProp.IndivData?.MeasuredData != null);
        }
        public double GetHeadBodyLengthMm()
        {
            if (this.ElementProp.IndivData?.MeasuredData != null)
            {
                return this.ElementProp.IndivData.MeasuredData.HeadBodyLength;
            }
            return 0;
        }
        public string GetHeadBodyLengthNice()
        {
            if (this.ElementProp.IndivData?.MeasuredData != null)
            {
                return ConvInvar.ToDecimalString(this.ElementProp.IndivData.MeasuredData.HeadBodyLength, 1) + " mm";
            }
            return "";
        }
        public SymbolProperties GetSymbolProperties()
        {
            var dHBL = this.GetHeadBodyLengthMm();
            var bgColor = this.GetClassColor();
            var sb = new SymbolProperties
            {
                Radius = (dHBL == 0) ? 3 : (dHBL * 0.10),
                BgColor = bgColor,
                FgColor = bgColor,
            };
            return sb;
        }
        public static int CompareByDateTime(Element a, Element b)
        {
            return DateTime.Compare(a.ElementProp.CreationTime, b.ElementProp.CreationTime);
        }
        public static int CompareByIId(Element a, Element b)
        {
            var ai = a.GetIIdAsInt();
            var bi = b.GetIIdAsInt();
            if (ai.HasValue)
            {
                if (bi.HasValue)
                {
                    if (ai.Value == bi.Value)
                    {
                        return CompareByDateTime(a, b);
                    }
                    else
                    {
                        return ai.Value - bi.Value;
                    }
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                if (bi.HasValue)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }
        public static double? CalcDistance(Element el1, Element el2)
        {
            LatLng ll1 = el1?.ElementProp?.MarkerInfo?.position;
            LatLng ll2 = el2?.ElementProp?.MarkerInfo?.position;
            if (ll1 != null && ll2 != null)
            {
                return GeoCalculator.GetDistance(ll2, ll1);
            }
            return null;
        }
        public static double CalcSimilarity(Element el1, Element el2)
        {
            if (el1?.ElementProp?.IndivData?.MeasuredData==null || el2?.ElementProp?.IndivData?.MeasuredData==null) {
                return -10;
            }
            double fSimilarity = 0;
            // Zeitlich sortieren (y(oung) / o(ld)).
            DateTime d1 = el1.ElementProp.CreationTime;
            DateTime d2 = el2.ElementProp.CreationTime;
            int nAgeDiffDays = (int)(d2 - d1).TotalDays;
            Element ely = el1;
            Element elo = el2;
            {
                if (nAgeDiffDays < 0)
                {
                    ely = el2;
                    elo = el1;
                    nAgeDiffDays = -nAgeDiffDays;
                }
            }
            // Örtlicher Abstand.
            double? distance = Element.CalcDistance(el1,el2);
            if (!distance.HasValue)
            {
            }
            else if (nAgeDiffDays == 0)
            {
                // Am selben Tag gefangen -> müssen unterschiedlich sein.
                fSimilarity -= 3;
            }
            else
            {
                fSimilarity += Math.Max(-3, 3 - distance.Value / 50);
            }
            // Geschlecht.
            string g1 = el1.Gender;
            string g2 = el2.Gender;
            if (g1 == "m" && g2 == "m")
            {
                fSimilarity += 1;
            }
            else if ((g1 == "m" && g2 == "f") || (g1 == "f" && g2 == "m"))
            {
                fSimilarity += -0.5;
            }
            // Kopf-Rumpf-Länge.
            {
                double ly = ely.GetHeadBodyLengthMm();
                double lo = elo.GetHeadBodyLengthMm();
                {
                    double expectedGrowthMm = Math.Min(30, (4 * nAgeDiffDays / 30) * (20 / ly));
                    double relGrowth = (lo - ly) / Math.Min(ly, lo);
                    if (relGrowth < -0.20)
                    {
                        fSimilarity -= Math.Min(3, -relGrowth * 3);
                    }
                    fSimilarity += Math.Min(3, 4 / ((Math.Abs(lo - ly - expectedGrowthMm)) + 1));
                }
            }
            // Merkmalsabstand.
            var md1 = el1.ElementProp.IndivData?.MeasuredData;
            var md2 = el2.ElementProp.IndivData?.MeasuredData;
            if (md1!=null && md2!=null) {
                double fTraitScore = 0f;
                fTraitScore += Utilities.CalcTraitScore(md1.ShareOfBlack, md2.ShareOfBlack, 0.20);
                fTraitScore += Utilities.CalcTraitScore(md1.CenterOfMass, md2.CenterOfMass, 0.10);
                fTraitScore += Utilities.CalcTraitScore(md1.StdDeviation, md2.StdDeviation, 0.02);
                fTraitScore += Utilities.CalcTraitScore(md1.Entropy, md2.Entropy, 0.01);
                fTraitScore += Utilities.CalcTraitScore(md1.Granularity, md2.Granularity, 0.01);
                fSimilarity += 0.30 * fTraitScore;
            }
            //Trait.GetAllTraits().forEach((trait) =>
            //{
            //    let v1 = el1.getTraitValue(trait.Id);
            //    let v2 = el2.getTraitValue(trait.Id);
            //    fSimilarity += trait.GetValueSimilarity(v1, v2);
            //});
            return fSimilarity;
        }
        public double CalcSimilarity(Element other)
        {
            return Element.CalcSimilarity(this,other);
        }
        public static List<Element> GetPrunedListSortedBySimilarity(IEnumerable<Element> els, Element sample)
        {
            var lListToSort = new List<Element>(els);
            lListToSort.Sort((a, b) =>
            {
                var simA = a.CalcSimilarity(sample);
                var simB = b.CalcSimilarity(sample);
                if (simA > simB)
                {
                    return -1;
                }
                else if (simA < simB)
                {
                    return 1;
                }
                else
                {
                    return Element.CompareByIId(a, b);
                }
            });
            var lIidList = new List<int>();
            var lPrunedList = new List<Element>();
            foreach (var el in lListToSort) {
                int? iid = el.GetIIdAsInt();
                if (iid.HasValue) {
                    if (!lIidList.Contains(iid.Value)) {
                        lIidList.Add(iid.Value);
                        lPrunedList.Add(el);
                    }
                }
            }
            return lPrunedList;
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
