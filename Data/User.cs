using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace BioMap
{
  public class User
  {
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email.")]
    public string EMail;
    [Required]
    public string FullName;
    public int Level;
  }
}
