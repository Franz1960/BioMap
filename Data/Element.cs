using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
  }
}
