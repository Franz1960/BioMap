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
    public Func<bool> DisplayOriginalImage { get; set; }
    public Func<Element, string> GetZoom { get; set; }
    //
    private TaxonDropDown taxonDropDown;
    private bool hasPhoto = false;
    public Element Element {
      get => this._Element;
      set {
        if (value != this._Element) {
          Element el = this._Element = value;
          if (this.taxonDropDown != null) {
            this.taxonDropDown.SetSelectedTaxon(el?.Classification?.LivingBeing?.Taxon);
          } else {
            this._ElementPending = true;
          }
          if (el == null) {
            throw new ArgumentException("Element must not be null.");
          } else {

            //System.Diagnostics.Debug.WriteLine($"ElementEdit.Element({el.ElementName}).Stadium={el.Classification.LivingBeing?.Stadium}");

            this.EditableCreationTime = el.ElementProp.CreationTime;
            this.SD.SelectedElementName = el.ElementName;
            this.OrigJson = JsonConvert.SerializeObject(el);
            this.OrigIId = el.ElementProp?.IndivData?.IId ?? null;

            this.hasPhoto = !string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(this.DS, this.SD.CurrentUser.Project, el.ElementName));
            this.Properties.Clear();
            if (el.HasIndivData() && el.HasMeasuredData()) {
              this.Properties.Add(new[] { this.Localize["Head-body-length"], el.GetHeadBodyLengthNice() });
            }
            if (el.HasIndivData()) {
              this.Properties.Add(new[] { this.Localize["Metamorphosis"], el.GetDateOfBirthAsString() });
              Element[] els = this.DS.GetElements(this.SD, null, "indivdata.iid='" + el.GetIId() + "' AND elements.creationtime<'" + el.GetIsoDateTime() + "'", "elements.creationtime DESC");
              if (els.Length >= 1) {
                double dDistance = GeoCalculator.GetDistance(els[0].ElementProp.MarkerInfo.position, el.ElementProp.MarkerInfo.position);
                this.Properties.Add(new[] { this.Localize["Migration"], ConvInvar.ToDecimalString(dDistance, 0) + " m" });
              }
            }
            this.Properties.Add(new[] { "File name", el.ElementName });
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
    private DateTime? EditableCreationTime;
    protected override void OnInitialized() {
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      if (this.taxonDropDown != null && this._ElementPending) {
        this.taxonDropDown.SetSelectedTaxon(this.Element?.Classification?.LivingBeing?.Taxon);
        this._ElementPending = false;
        this.StateHasChanged();
      }
    }
    private List<string[]> Properties { get; set; } = new List<string[]>();
    private string OrigJson = null;
    private int? OrigIId = null;
    public string EditingChangedIId() {
      if (this.Element.HasIndivData() && this.OrigIId.HasValue) {
        if (this.OrigIId.Value != this.Element.GetIIdAsInt().Value) {
          return $"{ConvInvar.ToString(this.OrigIId.Value)} --> {this.Element.GetIId()}";
        }
      }
      return null;
    }
    public string[] EditingChangedContent() {
      if (this.Element != null && this.OrigJson != null) {
        if (this.EditableCreationTime.HasValue) {
          this.Element.ElementProp.CreationTime = this.EditableCreationTime.Value;
        }
        string sJson = JsonConvert.SerializeObject(this.Element);
        string[] saDiff = Utilities.FindDifferingCoreParts(this.OrigJson, sJson);
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
      this.DS.DeleteElement(this.SD, el);
      this.DS.AddLogEntry(this.SD, "Deleted element " + el.ElementName);
      this.ElementDeleted?.Invoke();
      this.StateHasChanged();
    }
  }
}
