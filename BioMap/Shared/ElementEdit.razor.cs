using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using GoogleMapsComponents;
using GoogleMapsComponents.Maps;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Shared
{
  public partial class ElementEdit : ComponentBase
  {
    [Parameter]
    public bool Edit { get; set; } = false;
    [Parameter]
    public Action ElementDeleted { get; set; }
    //
    private TaxonDropDown taxonDropDown;
    private bool hasPhoto = false;
    public Element Element {
      get => this._Element;
      set {
        if (value != this._Element) {
          var el = this._Element = value;
          if (this.taxonDropDown != null) {
            this.taxonDropDown.SelectedTaxon = el?.Classification?.LivingBeing?.Taxon;
          } else {
            this._ElementPending = true;
          }
          if (el == null) {
            throw new ArgumentException("Element must not be null.");
          } else {

            //System.Diagnostics.Debug.WriteLine($"ElementEdit.Element({el.ElementName}).Stadium={el.Classification.LivingBeing?.Stadium}");

            SD.SelectedElementName = el.ElementName;
            this.OrigJson = JsonConvert.SerializeObject(el);
            this.hasPhoto = !string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(DS, SD.CurrentUser.Project, el.ElementName));
            this.Properties.Clear();
            if (el.HasIndivData() && el.HasMeasuredData()) {
              this.Properties.Add(new[] { Localize["Head-body-length"], el.GetHeadBodyLengthNice() });
            }
            if (el.HasIndivData()) {
              this.Properties.Add(new[] { Localize["Metamorphosis"], el.GetDateOfBirthAsString() });
              var els = DS.GetElements(SD, null, "indivdata.iid='" + el.GetIId() + "' AND elements.creationtime<'" + el.GetIsoDateTime() + "'", "elements.creationtime DESC");
              if (els.Length >= 1) {
                double dDistance = GeoCalculator.GetDistance(els[0].ElementProp.MarkerInfo.position, el.ElementProp.MarkerInfo.position);
                this.Properties.Add(new[] { Localize["Migration"], ConvInvar.ToDecimalString(dDistance, 0) + " m" });
              }
            }
            this.Properties.Add(new[] { "File name", el.ElementName });
            if (hasPhoto) {
              this.Properties.Add(new[] { "Time", ConvInvar.ToString(el.ElementProp.CreationTime) });
            }
            this.Properties.Add(new[] { "Uploaded", ConvInvar.ToString(el.ElementProp.UploadInfo.Timestamp) });
            this.Properties.Add(new[] { "by", el.ElementProp.UploadInfo.UserId });
            if (this.hasPhoto) {
              this.Properties.Add(new[] { "Camera", el.ElementProp.ExifData.Make + " / " + el.ElementProp.ExifData.Model });
            }
          }
          this.StateHasChanged();
        }
      }
    }
    private Element _Element = null;
    private bool _ElementPending = false;
    protected override void OnInitialized() {
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      if (this.taxonDropDown != null && this._ElementPending) {
        this.taxonDropDown.SelectedTaxon = this.Element?.Classification?.LivingBeing?.Taxon;
        this._ElementPending = false;
        this.StateHasChanged();
      }
    }
    private List<string[]> Properties { get; set; } = new List<string[]>();
    private string OrigJson = null;
    public string[] EditingChangedContent() {
      if (this.Element != null && this.OrigJson != null) {
        string sJson = JsonConvert.SerializeObject(this.Element);
        var saDiff = Utilities.FindDifferingCoreParts(this.OrigJson, sJson);
        return saDiff;
      }
      return null;
    }
    private void TaxonDropDown_SelectedTaxonChanged() {
      if (this.Element.Classification.LivingBeing == null) {
        this.Element.Classification.LivingBeing = new ElementClassification.LivingBeing_t {
          Taxon = this.taxonDropDown.SelectedTaxon,
          Stadium = ElementClassification.Stadium.None,
        };
      } else {
        this.Element.Classification.LivingBeing.Taxon = this.taxonDropDown.SelectedTaxon;
      }
    }
    private void DeleteElement(Element el) {
      DS.DeleteElement(SD, el);
      DS.AddLogEntry(SD, "Deleted element " + el.ElementName);
      this.ElementDeleted?.Invoke();
      this.StateHasChanged();
    }
  }
}
