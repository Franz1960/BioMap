using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BioMap
{
  public class Document
  {
    public enum DocType_en
    {
      Unknown = 0,
      Pdf,
    }
    public string DisplayName;
    public DocType_en DocType;
    public string Filename;
  }
}
