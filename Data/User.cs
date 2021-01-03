using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public bool MaySeeLocations { get => Level>=400; }
    public bool MayChangeLocations { get => Level>=500; }
    public bool MaySeeOtherUsers { get => Level>=500; }
    public bool MayChangeOtherUsers { get => Level>=600; }
    public bool MayChangeElements { get => Level>=500; }
    public bool MayDeleteElement(Element el) {
      if (Level>=500 || string.CompareOrdinal(el.ElementProp.UploadInfo.UserId,EMail)==0) {
        return true;
      }
      return false;
    }
  }
}
