using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;

namespace BioMap
{
  [JsonObject(MemberSerialization.Fields)]
  public class User
  {
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email.")]
    public string EMail;
    [Required]
    public string FullName;
    public int Level;
    public string Project;
    public string PermTicket;
    //
    public class Preferences
    {
      public string MaptypeId;
      public bool ShowCustomMap;
      public int ShowPlaces;
      public bool DynaZoomed;
      public int DisplayConnectors;
      public int TimeIntervalWeeks;
    }
    public readonly Preferences Prefs = new Preferences();
    public bool MaySeeProject { get => this.Level >= 200; }
    public bool MayChangeLocations { get => this.Level >= 500; }
    public bool MaySeeOtherUsers { get => this.Level >= 500; }
    public bool MayChangeOtherUsers { get => this.Level >= 600; }
    public bool MayChangeElements { get => this.Level >= 500; }
    public bool MayChangeElementPlace(Element el) {
      if (this.Level >= 600 || (this.Level >= 500 && string.CompareOrdinal(el.ElementProp.UploadInfo.UserId, this.EMail) == 0)) {
        return true;
      }
      return false;
    }
    public bool MayUploadElements { get => this.Level >= 300; }
    public bool MayDeleteElement(Element el) {
      if (this.Level >= 500 || string.CompareOrdinal(el.ElementProp.UploadInfo.UserId, this.EMail) == 0) {
        return true;
      }
      return false;
    }
    public string GetNiceName() {
      if (!string.IsNullOrEmpty(this.FullName)) {
        return this.FullName + " (" + this.EMail + ")";
      } else {
        return this.EMail;
      }
    }
  }
}
