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
    //
    public class Preferences
    {
      public string MaptypeId;
      public bool ShowCustomMap;
      public int ShowPlaces;
      public bool DynaZoomed;
      public bool DisplayConnectors;
    }
    public readonly Preferences Prefs = new Preferences();
    public bool MaySeeProject { get => Level >= 200; }
    public bool MayChangeLocations { get => Level >= 500; }
    public bool MaySeeOtherUsers { get => Level >= 500; }
    public bool MayChangeOtherUsers { get => Level >= 600; }
    public bool MayChangeElements { get => Level >= 500; }
    public bool MayChangeElementPlace(Element el) {
      if (Level >= 600 || (Level >= 500 && string.CompareOrdinal(el.ElementProp.UploadInfo.UserId, this.EMail) == 0)) {
        return true;
      }
      return false;
    }
    public bool MayUploadElements { get => Level >= 300; }
    public bool MayDeleteElement(Element el) {
      if (Level >= 500 || string.CompareOrdinal(el.ElementProp.UploadInfo.UserId, EMail) == 0) {
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
