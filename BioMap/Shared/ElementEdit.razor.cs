using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Blazorise;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;

namespace BioMap.Shared
{
  public partial class ElementEdit : ComponentBase
  {
    [Parameter]
    public bool Edit { get; set; } = false;
    //
    private bool hasPhoto = false;
    public Element Element {
      get {
        return this._Element;
      }
      set {
        if (value!=this._Element) {
          var el=this._Element=value;
          if (el==null) {
            throw new ArgumentException("Element must not be null.");
          } else {
            SD.SelectedElementName=el.ElementName;
            this.OrigJson=JsonConvert.SerializeObject(el);
            this.hasPhoto=el.HasPhotoData();
            this.Properties.Clear();
            if (el.HasIndivData() && el.HasMeasuredData()) {
              this.Properties.Add(new[] { Localize["Head-body-length"],el.GetHeadBodyLengthNice() });
            }
            if (el.HasIndivData()) {
              this.Properties.Add(new[] { Localize["Metamorphosis"],el.GetDateOfBirthAsString() });
              var els = DS.GetElements(SD,null,"indivdata.iid='"+el.GetIId()+"' AND elements.creationtime<'"+el.GetIsoDateTime()+"'","elements.creationtime DESC");
              if (els.Length>=1) {
                double dDistance = GeoCalculator.GetDistance(els[0].ElementProp.MarkerInfo.position,el.ElementProp.MarkerInfo.position);
                this.Properties.Add(new[] { Localize["Migration"],ConvInvar.ToDecimalString(dDistance,0)+" m" });
              }
            }
            this.Properties.Add(new[] { Localize["Place"],el.GetPlaceName() });
            this.Properties.Add(new[] { "File name",el.ElementName });
            if (hasPhoto) {
              this.Properties.Add(new[] { "Time",ConvInvar.ToString(el.ElementProp.CreationTime) });
            }
            this.Properties.Add(new[] { "Uploaded",ConvInvar.ToString(el.ElementProp.UploadInfo.Timestamp) });
            this.Properties.Add(new[] { "by",el.ElementProp.UploadInfo.UserId });
            if (this.hasPhoto) {
              this.Properties.Add(new[] { "Camera",el.ElementProp.ExifData.Make+" / "+el.ElementProp.ExifData.Model });
            }
          }
          this.StateHasChanged();
        }
      }
    }
    private Element _Element = null;
    private List<string[]> Properties { get; set; } = new List<string[]>();
    private string OrigJson = null;
    public string[] EditingChangedContent() {
      if (this.Element!=null && this.OrigJson!=null) {
        string sJson = JsonConvert.SerializeObject(this.Element);
        var saDiff=Utilities.FindDifferingCoreParts(this.OrigJson,sJson);
        return saDiff;
      }
      return null;
    }
  }
}
