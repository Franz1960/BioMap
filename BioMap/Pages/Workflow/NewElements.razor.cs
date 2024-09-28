using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using BioMap.ImageProc;
using BioMap.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Convolution;

namespace BioMap.Pages.Workflow
{
  public partial class NewElements : ComponentBase
  {
    private Element[] Elements = new Element[0];
    private int RemainingElements = 0;
    private DateTime? SelectedElementTime = null;
    private PhotoPopup PhotoPopup1;
    protected Blazor.ImageSurveyor.ImageSurveyor ImageSurveyor;
    private Blazor.ImageSurveyor.ImageSurveyor imageSurveyorPrev;
    private string PatternImgSrc = "";
    private bool disableSetImage = false;
    private bool elementChanged = false;
    private Element Element {
      get => this._Element;
      set {
        if (value != this._Element) {
          if (this._Element != null) {
            if (this._Element.Gender != "f" && this._Element.Gender != "m") {
              if (this._Element.TryDetermineGender(this.SD, null, out string sGender)) {
                this._Element.Gender = sGender;
              }
            }
            this.DS.WriteElement(this.SD, this._Element);
            // Bild normieren.
            if (this._Element.MeasureData.normalizePoints != null) {
              string sSrcFile = this.DS.GetFilePathForImage(this.SD.CurrentUser.Project, this._Element.ElementName, true);
              string sDstFile = this.DS.GetFilePathForImage(this.SD.CurrentUser.Project, this._Element.ElementName, false);
              using (var imgSrc = Image.Load(sSrcFile)) {
                try {
                  imgSrc.Mutate(x => x.AutoOrient());
                  imgSrc.Mutate(x => x.ExtractNormalized(this._Element.MeasureData));
                } catch { }
                imgSrc.SaveAsJpeg(sDstFile);
              }
            }
          }
          this._Element = value;
          if (this._Element != null) {
            this.SD.SelectedElementName = this._Element?.ElementName;
            this.SelectedElementTime = this._Element.ElementProp.CreationTime;
          }
        };
      }
    }
    private Element _Element = null;
    private bool Raw {
      get => this._Raw;
      set {
        if (value != this._Raw) {
          this._Raw = value;
          this.disableSetImage = false;
          this.elementChanged = true;
          this.StateHasChanged();
        }
      }
    }
    private bool _Raw = true;
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      this.SD.Filters.FilteringTarget = Filters.FilteringTargetEnum.Elements;
      this.SD.Filters.FilterChanged += this.Filters_FilterChanged;
      await this.RefreshData();
      await this.SelectElement();
      this.NM.LocationChanged += this.NM_LocationChanged;
    }
    private async void Filters_FilterChanged(object sender, EventArgs e) {
      await this.RefreshData();
      await this.SelectElement();
      this.disableSetImage = false;
      this.elementChanged = true;
      this.StateHasChanged();
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      this.NM.LocationChanged -= this.NM_LocationChanged;
      this.SD.Filters.FilterChanged -= this.Filters_FilterChanged;
      this.Element = null;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        if (this.IsSpecialAuto) {
          var timer = new System.Timers.Timer(3000);
          timer.Elapsed += async (sender, e) => {
            await this.OnSelectNext(false);
            if (this.RemainingElements < 1) {
              timer.Enabled = false;
            }
          };
          timer.Enabled = true;
        }
      }
      if (this.ImageSurveyor != this.imageSurveyorPrev) {
        if (this.imageSurveyorPrev != null) {
          this.imageSurveyorPrev.AfterRender -= this.imageSurveyor_AfterRenderEvent;
          this.imageSurveyorPrev.MeasureDataChanged -= this.imageSurveyor_MeasureDataChanged;
        }
        if (this.ImageSurveyor != null) {
          this.ImageSurveyor.AfterRender += this.imageSurveyor_AfterRenderEvent;
          this.ImageSurveyor.MeasureDataChanged += this.imageSurveyor_MeasureDataChanged;
        }
        this.imageSurveyorPrev = this.ImageSurveyor;
      } else {
        if (this.elementChanged) {
          if (this.Element != null) {
            this.elementChanged = false;
            await this.LoadElement();
          }
        }
      }
    }
    private async void imageSurveyor_AfterRenderEvent(object sender, EventArgs e) {
      if (this.Element != null) {
        await this.LoadElement();
        await this.RefreshPatternImg();
      }
    }
    private void imageSurveyor_MeasureDataChanged(object sender, Blazor.ImageSurveyor.ImageSurveyorMeasureData measureData) {
      lock (this.LockMeasureDataProcessing) {
        if (this.Element != null) {
          if (this.MeasureDataProcessing_CurrentMeasureData == null) {
            // No processing in progress.
            this.MeasureDataProcessing_CurrentMeasureData = measureData;
            Task.Run(this.MeasureDataChanged);
          } else {
            this.MeasureDataProcessing_NextMeasureData = measureData;
          }
        }
      }
    }
    private readonly object LockMeasureDataProcessing = new object();
    private Blazor.ImageSurveyor.ImageSurveyorMeasureData MeasureDataProcessing_CurrentMeasureData = null;
    private Blazor.ImageSurveyor.ImageSurveyorMeasureData MeasureDataProcessing_NextMeasureData = null;
    private async Task SelectElement() {
      this.Element = (await this.DS.GetElementsAsync(this.SD, this.SD.Filters, "elements.name='" + this.SD.SelectedElementName + "'")).FirstOrDefault();
      if (this.Element == null) {
        await this.OnSelectNext(false);
      }
    }
    private bool IsSpecialAuto { get => (this.SD.CurrentUser.Level == 742); }
    private async Task RefreshData() {
      var lElements = new List<Element>();
      if (this.IsSpecialAuto) {
        Element[] els = await this.DS.GetElementsAsync(this.SD, this.SD.Filters,
          "(((elements.classification LIKE '%\"ClassName\":\"ID photo\"%') AND ifnull(indivdata.stddeviation,0)=0))",
          "elements.creationtime ASC");
        foreach (Element el in els) {
          if (!string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(this.DS, this.SD.CurrentUser.Project, el.ElementName))) {
            lElements.Add(el);
          }
        }
      } else {
        Element[] els = await this.DS.GetElementsAsync(this.SD, this.SD.Filters,
          "((elements.classification LIKE '%\"ClassName\":\"New\"%') OR (elements.croppingconfirmed<>1) OR (elements.croppingconfirmed IS NULL))",
          "elements.creationtime ASC");
        foreach (Element el in els) {
          if (!string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(this.DS, this.SD.CurrentUser.Project, el.ElementName))) {
            lElements.Add(el);
          }
        }
        if (lElements.Count < 1) {
          els = await this.DS.GetElementsAsync(this.SD, this.SD.Filters,
            "(((elements.classification LIKE '%\"ClassName\":\"ID photo\"%') AND ifnull(indivdata.genderfeature,'')=''))",
            "elements.creationtime ASC");
          foreach (Element el in els) {
            if (!string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(this.DS, this.SD.CurrentUser.Project, el.ElementName))) {
              lElements.Add(el);
            }
          }
        }
      }
      this.Elements = lElements.ToArray();
      this.RemainingElements = this.Elements.Length;
    }
    private async Task newClass_Selected(string sNewClass) {
      if (string.CompareOrdinal(sNewClass, this.Element?.Classification?.ClassName) != 0) {
        this.Element.InitMeasureData(this.SD, sNewClass, false);
        this.DS.WriteElement(this.SD, this.Element);
        this.disableSetImage = false;
        await this.LoadElement();
        this.StateHasChanged();
      }
    }
    private void ElementName_Click(Element el) {
      this.PhotoPopup1.Show(el);
    }
    private async Task OnSelectPrev() {
      this.Element = null;
      this.Raw = true;
      await this.RefreshData();
      if (this.SelectedElementTime.HasValue) {
        this.Element = this.Elements.LastOrDefault((el) => (el.ElementProp.CreationTime < this.SelectedElementTime));
      } else if (this.Elements.Length >= 1) {
        this.Element = this.Elements[0];
      }
      this.disableSetImage = false;
      this.elementChanged = true;
      this.StateHasChanged();
    }
    private async Task OnSelectNext(bool bSave) {
      if (bSave) {
        if (!this.Element.CroppingConfirmed) {
          this.Element.CroppingConfirmed = true;
        }
      }
      this.Element = null; // Save implicitely.
      this.Raw = true;
      await this.RefreshData();
      if (this.SelectedElementTime.HasValue) {
        this.Element = this.Elements.FirstOrDefault((el) => (el.ElementProp.CreationTime > this.SelectedElementTime));
      } else if (this.Elements.Length >= 1) {
        this.Element = this.Elements[0];
      }
      this.disableSetImage = false;
      this.elementChanged = true;
      this.StateHasChanged();
    }
    private async Task LoadElement() {
      if (!this.disableSetImage) {
        if (this.Element != null) {
          this.disableSetImage = true;
          if (this.Element.MeasureData?.normalizer == null) {
            this.Element.InitMeasureData(this.SD, this.Element.Classification.ClassName, false);
          } else if (this.Element.HasImageButNoOrigImage(this.SD)) {
            this.Element.InitMeasureData(this.SD, this.Element.Classification.ClassName, false);
          }
          bool bHasImageButNoOrigImage = (this.Element.MeasureData?.normalizePoints == null);
          if (bHasImageButNoOrigImage) {
            this.Raw = false;
          }
          string sUrlOrigImage = "api/photos/" + this.Element.ElementName + "?Project=" + this.SD.CurrentUser.Project + "&ForceOrig=1";
          string sUrlImage;
          if (this.Raw) {
            sUrlImage = sUrlOrigImage;
          } else {
            if (bHasImageButNoOrigImage) {
              sUrlImage = "api/photos/" + this.Element.ElementName + "?Project=" + this.SD.CurrentUser.Project + "&ForceOrig=0";
            } else {
              string sSrcFile = this.DS.GetFilePathForImage(this.SD.CurrentUser.Project, this.Element.ElementName, true);
              using (var imgSrc = Image.Load(sSrcFile)) {
                try {
                  imgSrc.Mutate(x => x.AutoOrient());
                  imgSrc.Mutate(x => x.ExtractNormalized(this.Element.MeasureData));
                } catch { }
                var bs = new System.IO.MemoryStream();
                imgSrc.SaveAsJpeg(bs);
                sUrlImage = "data:image/png;base64," + Convert.ToBase64String(bs.ToArray());
              }
            }
          }
          await this.ImageSurveyor.SetImageUrlAsync(sUrlImage, this.Raw, this.Element.MeasureData);
        }
      }
    }
    private void MeasureDataChanged() {
      Element el = this.Element;
      if (this.MeasureDataProcessing_CurrentMeasureData == null) {
        this.MeasureDataProcessing_CurrentMeasureData = el.MeasureData;
      }
      Blazor.ImageSurveyor.ImageSurveyorMeasureData md = this.MeasureDataProcessing_CurrentMeasureData;
      if (el.ElementProp.IndivData == null) {
        el.ElementProp.IndivData = new Element.IndivData_t {
          MeasuredData = new Element.IndivData_t.MeasuredData_t {
            HeadBodyLength = 0,
          },
        };
      }
      while (true) {
        if (this.Element == null) {
          lock (this.LockMeasureDataProcessing) {
            this.MeasureDataProcessing_CurrentMeasureData = null;
            this.MeasureDataProcessing_NextMeasureData = null;
          }
          break;
        } else {
          if (string.CompareOrdinal(md.normalizer.NormalizeMethod, "HeadToCloakInPetriDish") == 0) {
            el.ElementProp.IndivData.MeasuredData.HeadBodyLength = md.GetHeadBodyLengthPx(false) * md.normalizer.NormalizePixelSize;
          } else if (string.CompareOrdinal(md.normalizer.NormalizeMethod, "HeadToCloakIn50mmCuvette") == 0) {
            el.ElementProp.IndivData.MeasuredData.HeadBodyLength = md.GetHeadBodyLengthPx(false) * md.normalizer.NormalizePixelSize;
          }
          el.MeasureData = md;
          _ = this.RefreshPatternImg();
        }
        lock (this.LockMeasureDataProcessing) {
          if (this.MeasureDataProcessing_NextMeasureData == null) {
            this.MeasureDataProcessing_CurrentMeasureData = null;
            break;
          } else {
            md = this.MeasureDataProcessing_NextMeasureData;
            this.MeasureDataProcessing_NextMeasureData = null;
          }
        }
      }
    }
    private async Task RefreshPatternImg() {
      string sPatternImgSrc = "";
      try {
        Element el = this.Element;
        if (el != null && el.Classification.IsIdPrimaryPhoto()) {
          sPatternImgSrc = Utilities.GetPatternImgSource(el, this.DS, this.SD, async polyLines => await this.JSRuntime.InvokeVoidAsync("PrepPic.setPolyLines", JsonConvert.SerializeObject(this.Raw ? polyLines : null)));
        }
      } catch {
      } finally {
        this.PatternImgSrc = sPatternImgSrc;
        await this.InvokeAsync(() => this.StateHasChanged());
      }
    }
    private async Task ResetPositions_Clicked(Element el) {
      string sFilePath = this.DS.GetFilePathForImage(this.SD.CurrentUser.Project, el.ElementName, true);
      if (System.IO.File.Exists(sFilePath)) {
        using (var img = Image.Load(sFilePath)) {
          img.Mutate(x => x.AutoOrient());
          int w = img.Width;
          int h = img.Height;
          if (ElementClassification.IsNormed(this.Element?.Classification?.ClassName)) {
            Blazor.ImageSurveyor.ImageSurveyorNormalizer normalizer = this.SD.CurrentProject.ImageNormalizer;
            el.MeasureData = new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
              normalizer = normalizer,
              normalizePoints = normalizer.GetDefaultNormalizePoints(w, h).ToArray(),
              measurePoints = normalizer.GetDefaultMeasurePoints(w, h).ToArray(),
            };
          } else {
            var normalizer = new Blazor.ImageSurveyor.ImageSurveyorNormalizer("CropRectangle");
            el.MeasureData = new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
              normalizer = normalizer,
              normalizePoints = normalizer.GetDefaultNormalizePoints(w, h).ToArray(),
              measurePoints = normalizer.GetDefaultMeasurePoints(w, h).ToArray(),
            };
          }
        }
        this.disableSetImage = false;
        await this.LoadElement();
      }
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
    private TaxonDropDown taxonDropDown;
  }
}
