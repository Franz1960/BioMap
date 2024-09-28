using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BioMap.ImageProc;
using BioMap.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Convolution;
using static BioMap.Filters;

namespace BioMap.Pages.Workflow
{
  public partial class Identify : ComponentBase
  {
    private Element[] ElementsToIdentify = new Element[0];
    private string ElementToIdentifyPosition = "";
    private Element[] ElementsToCompare = new Element[0];
    private Dictionary<Element, double> ElementSimilarities = new();
    private bool ElementsToCompare_Invalid = true;
    private Element ElementToCompare = null;
    private Element PrevElementToCompare = null;
    private string PatternImgSrc = "";
    private string PatternCompareImgSrc = "";
    private string PatternMatchingImgSrc = "";
    private readonly Dictionary<Element, ElementReference> dictElementCards = new Dictionary<Element, ElementReference>();
    private PhotoPopup PhotoPopup1;
    private Blazorise.Alert Alert1;
    private bool DisplayAll { get; set; } = false;
    private Element Element {
      get => this._Element;
      set {
        if (value != this._Element) {
          if (this._Element != null) {
            this.DS.WriteElement(this.SD, this._Element);
          }
          this._Element = value;
          this.DisplayAll = false;
          if (this._Element != null) {
            this.SD.SelectedElementName = this._Element.ElementName;
            // Set position string to display.
            {
              int nPosition = -1;
              for (int i = 0; i < this.ElementsToIdentify.Length; i++) {
                if (string.CompareOrdinal(this.ElementsToIdentify[i].ElementName, this._Element.ElementName) == 0) {
                  nPosition = i;
                  break;
                }
              }
              this.ElementToIdentifyPosition = $"{nPosition + 1} / {this.ElementsToIdentify.Length}";
            }
            try {
              this.PatternImgSrc = Utilities.GetPatternImgSource(this._Element, this.DS, this.SD);
            } catch {
              this.PatternImgSrc = "";
            }
            if (this.ElementsToCompare.Length >= 2) {
              (List<Element> lElements, this.ElementSimilarities) = Element.GetPrunedListSortedBySimilarity(this.SD, this.ElementsToCompare, this.Element);
              this.ElementsToCompare = lElements.ToArray();
            }
          }
        };
      }
    }
    private Element _Element = null;
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.SD.Filters.FilteringTarget = Filters.FilteringTargetEnum.Elements;
      this.SD.Filters.FilterChanged += this.Filters_FilterChanged;
      await this.RefreshData();
      await this.SelectElement();
      this.NM.LocationChanged += this.NM_LocationChanged;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      if (this.PrevElementToCompare != null) {
        if (this.dictElementCards.TryGetValue(this.PrevElementToCompare, out ElementReference prevCard)) {
          Utilities.CallDelayed(1500, async (oaArgs) => await this.JSRuntime.InvokeVoidAsync("scrollToElement", prevCard));
        }
        this.PrevElementToCompare = null;
      }
      this.HasRendered = true;
    }
    private bool HasRendered = false;
    private async void Filters_FilterChanged(object sender, EventArgs e) {
      await this.RefreshData();
      await this.SelectElement();
      this.StateHasChanged();
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      this.NM.LocationChanged -= this.NM_LocationChanged;
      this.SD.Filters.FilterChanged -= this.Filters_FilterChanged;
      this.Element = null;
    }
    private async Task<Element[]> GetElementsToIdentify() {
      this.ElementsToIdentify = await this.DS.GetElementsAsync(this.SD, this.SD.Filters,
        WhereClauses.Is_ID_photo + " AND (indivdata.iid IS NULL OR indivdata.iid<1)",
        "elements.creationtime ASC LIMIT 99");
      if (this.ElementsToIdentify.Length < 1 && this.SD.Filters.ExpandIfNoUnidentifiedFilter) {
        this.ElementsToIdentify = await this.DS.GetElementsAsync(this.SD, this.SD.Filters,
          WhereClauses.Is_ID_photo,
          "elements.creationtime ASC LIMIT 99");
      }
      return this.ElementsToIdentify;
    }
    private async Task UpdateElementsToCompare() {
      if (this.ElementsToCompare_Invalid) {
        this.ElementsToCompare_Invalid = false;
        await this.RefreshElementsToCompare();
      }
    }
    private async Task OnComparePrev() {
      var le = this.ElementsToCompare.ToList();
      int idx0 = le.IndexOf(this.ElementToCompare);
      if (idx0 >= 1) {
        this.ElementToCompare = le[idx0 - 1];
      } else {
        this.ElementToCompare = le.LastOrDefault();
      }
      if (this.ElementToCompare != null) {
        this.PatternCompareImgSrc = Utilities.GetPatternImgSource(this.ElementToCompare, this.DS, this.SD);
        this.PatternMatchingImgSrc = Utilities.GetPatternMatchingImgSource(this.ElementToCompare, this.Element, this.SD);
      }
      if (this.HasRendered) {
        await this.InvokeAsync(() => this.StateHasChanged());
      }
    }
    private async Task OnCompareNext() {
      var le = this.ElementsToCompare.ToList();
      int idx0 = le.IndexOf(this.ElementToCompare);
      if (idx0 < le.Count - 1 && idx0 >= 0) {
        this.ElementToCompare = le[idx0 + 1];
      } else {
        this.ElementToCompare = le.FirstOrDefault();
      }
      if (this.ElementToCompare != null) {
        this.PatternCompareImgSrc = Utilities.GetPatternImgSource(this.ElementToCompare, this.DS, this.SD);
        this.PatternMatchingImgSrc = Utilities.GetPatternMatchingImgSource(this.ElementToCompare, this.Element, this.SD);
      }
      if (this.HasRendered) {
        await this.InvokeAsync(() => this.StateHasChanged());
      }
    }
    private async Task OnSelectPrev() {
      DateTime? dtSelectedElement = this.Element?.ElementProp.CreationTime;
      this.Element = null; // Save implicitely.
      this.ElementToCompare = null;
      await this.UpdateElementsToCompare();
      Element[] aElements = await this.GetElementsToIdentify();
      if (dtSelectedElement.HasValue) {
        this.Element = aElements.LastOrDefault((el) => (el.ElementProp.CreationTime < dtSelectedElement.Value));
        if (this.Element == null) {
          this.Element = aElements.LastOrDefault((el) => true);
        }
      } else if (aElements.Length >= 1) {
        this.Element = aElements[0];
      }
      await this.Alert1.Hide();
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private async Task OnSelectNext() {
      DateTime? dtSelectedElement = this.Element?.ElementProp.CreationTime;
      this.Element = null; // Save implicitely.
      this.ElementToCompare = null;
      await this.UpdateElementsToCompare();
      Element[] aElements = await this.GetElementsToIdentify();
      if (dtSelectedElement.HasValue) {
        this.Element = aElements.FirstOrDefault((el) => (el.ElementProp.CreationTime > dtSelectedElement.Value));
        if (this.Element == null) {
          this.Element = aElements.FirstOrDefault((el) => true);
        }
      } else if (aElements.Length >= 1) {
        this.Element = aElements[0];
      }
      await this.Alert1.Hide();
      if (this.HasRendered) {
        await this.InvokeAsync(() => this.StateHasChanged());
      }
    }
    private async Task SelectElement() {
      Element[] aElements = await this.GetElementsToIdentify();
      Element? selectedElement = aElements.FirstOrDefault(el => string.CompareOrdinal(el.ElementName, this.SD.SelectedElementName) == 0);
      if (selectedElement != null) {
        this.Element = selectedElement;
      } else {
        await this.OnSelectNext();
      }
    }
    private async Task RefreshData() {
      await this.RefreshElementsToCompare();
    }
    private async Task RefreshElementsToCompare() {
      Element[] els = await this.DS.GetElementsAsync(this.SD, null,
        WhereClauses.Is_Individuum,
        //WhereClauses.Is_Individuum + " AND elements.place='B1'",
        "elements.creationtime ASC");
      var lElements = new List<Element>();
      foreach (Element el in els) {
        if (!string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(this.DS, this.SD.CurrentUser.Project, el.ElementName))) {
          lElements.Add(el);
        }
      }
      this.ElementsToCompare = lElements.ToArray();
    }
    private async Task IdentifyAs(Element elementToCompare) {
      #region Log
      {
        if (this.Element.ElementProp.IndivData.IId >= 1) {
          if (this.Element.ElementProp.IndivData.IId != this.ElementToCompare.ElementProp.IndivData.IId) {
            this.DS.AddLogEntry(this.SD, $"Changed IId of {this.Element.ElementName}: {this.Element.ElementProp.IndivData.IId} --> {this.ElementToCompare.ElementProp.IndivData.IId}");
          }
        } else {
          this.DS.AddLogEntry(this.SD, $"Found IId for {this.Element.ElementName}: {this.ElementToCompare.ElementProp.IndivData.IId}");
        }
      }
      #endregion
      this.Element.ElementProp.IndivData.IId = this.ElementToCompare.ElementProp.IndivData.IId;
      this.Element.ElementProp.IndivData.DateOfBirth = this.ElementToCompare.ElementProp.IndivData.DateOfBirth;
      Element[] aPrevElements = this.DS.GetElements(this.SD, null, WhereClauses.Is_Iid(this.Element.ElementProp.IndivData.IId));
      bool bGenderConsistent = this.Element.TryDetermineGender(this.SD, aPrevElements, out string sGender);
      this.Element.Gender = sGender;
      this.DS.WriteElement(this.SD, this.Element);
      if (!bGenderConsistent) {
        await this.Alert1.Show();
      }
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private async Task CreateNewIId() {
      int nPrevIId = this.Element.ElementProp.IndivData.IId;
      this.Element.ElementProp.IndivData.IId = this.DS.GetNextFreeIId(this.SD);
      this.Element.TryDetermineGender(this.SD, null, out string sGender);
      this.Element.Gender = sGender;
      this.DS.WriteElement(this.SD, this.Element);
      if (nPrevIId >= 1) {
        this.DS.AddLogEntry(this.SD, $"Created new IId for {this.Element.ElementName}: {nPrevIId} --> {this.Element.ElementProp.IndivData.IId}");
      } else {
        this.DS.AddLogEntry(this.SD, $"Created new IId for {this.Element.ElementName}: {this.Element.ElementProp.IndivData.IId}");
      }
      this.ElementsToCompare_Invalid = true;
      await this.InvokeAsync(() => this.StateHasChanged());
    }
    private void ElementName_Click(Element el) {
      this.PhotoPopup1.Show(el);
    }
    private void ElementFromList_Clicked(Element el) {
      this.ElementToCompare = el;
      if (this.ElementToCompare != null) {
        this.PatternCompareImgSrc = Utilities.GetPatternImgSource(this.ElementToCompare, this.DS, this.SD);
        this.PatternMatchingImgSrc = Utilities.GetPatternMatchingImgSource(this.ElementToCompare, this.Element, this.SD);
        this.Element.CalcSimilarity(this.SD, this.ElementToCompare);
      }
      this.StateHasChanged();
    }
  }
}
