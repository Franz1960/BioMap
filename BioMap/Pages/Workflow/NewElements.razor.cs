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
    private bool normImageDirty = false;
    private Element Element {
      get => this._Element;
      set {
        if (value != this._Element) {
          if (this._Element != null) {
            DS.WriteElement(SD, this._Element);
            // Bild normieren.
            if (this.normImageDirty) {
              if (this._Element.MeasureData.normalizePoints != null) {
                var sSrcFile = DS.GetFilePathForImage(SD.CurrentUser.Project, this._Element.ElementName, true);
                var sDstFile = DS.GetFilePathForImage(SD.CurrentUser.Project, this._Element.ElementName, false);
                var md = this._Element.MeasureData;
                (int nWidth, int nHeight) = md.GetNormalizedSize();
                try {
                  using (var imgSrc = Image.Load(sSrcFile)) {
                    imgSrc.Mutate(x => x.AutoOrient());
                    var mNormalize = md.GetNormalizeMatrix();
                    using (var imgDst = ImageOperations.TransformAndCropOutOfImage(imgSrc, mNormalize, new Size(nWidth, nHeight))) {
                      imgDst.SaveAsJpeg(sDstFile);
                    }
                  }
                } catch { }
              }
            }
          }
          this._Element = value;
          SD.SelectedElementName = this._Element?.ElementName;
          if (this._Element != null) {
            this.SelectedElementTime = this._Element.ElementProp.CreationTime;
          }
        };
      }
    }
    private Element _Element = null;
    private bool Raw {
      get {
        return this._Raw;
      }
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
      SD.Filters.FilteringTarget = Filters.FilteringTargetEnum.Elements;
      SD.Filters.FilterChanged += this.Filters_FilterChanged;
      await RefreshData();
      await this.SelectElement();
      NM.LocationChanged += NM_LocationChanged;
    }
    private async void Filters_FilterChanged(object sender, EventArgs e) {
      await this.RefreshData();
      await this.SelectElement();
      this.disableSetImage = false;
      this.elementChanged = true;
      this.StateHasChanged();
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      NM.LocationChanged -= NM_LocationChanged;
      SD.Filters.FilterChanged -= this.Filters_FilterChanged;
      this.Element = null;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        if (IsSpecialAuto) {
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
        Utilities.CallDelayed(200, this.RefreshPatternImg);
      }
    }
    private void imageSurveyor_MeasureDataChanged(object sender, Blazor.ImageSurveyor.ImageSurveyorMeasureData measureData) {
      if (this.Element != null) {
        Utilities.CallDelayed(200, this.MeasureDataChanged, this.Element, measureData);
      }
    }
    private async Task SelectElement() {
      this.Element = (await DS.GetElementsAsync(SD, SD.Filters, "elements.name='" + SD.SelectedElementName + "'")).FirstOrDefault();
      if (this.Element == null) {
        await this.OnSelectNext(false);
      }
    }
    private bool IsSpecialAuto { get => (SD.CurrentUser.Level == 742); }
    private async Task RefreshData() {
      var lElements = new List<Element>();
      if (IsSpecialAuto) {
        var els = await DS.GetElementsAsync(SD, SD.Filters,
          "(((elements.classification LIKE '%\"ClassName\":\"ID photo\"%') AND ifnull(indivdata.stddeviation,0)=0))",
          "elements.creationtime ASC");
        foreach (var el in els) {
          if (!string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(DS, SD.CurrentUser.Project, el.ElementName))) {
            lElements.Add(el);
          }
        }
      } else {
        var els = await DS.GetElementsAsync(SD, SD.Filters,
          "((elements.classification LIKE '%\"ClassName\":\"New\"%') OR (elements.croppingconfirmed<>1) OR (elements.croppingconfirmed IS NULL))",
          "elements.creationtime ASC");
        foreach (var el in els) {
          if (!string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(DS, SD.CurrentUser.Project, el.ElementName))) {
            lElements.Add(el);
          }
        }
        if (lElements.Count < 1) {
          els = await DS.GetElementsAsync(SD, SD.Filters,
            "(((elements.classification LIKE '%\"ClassName\":\"ID photo\"%') AND ifnull(indivdata.genderfeature,'')=''))",
            "elements.creationtime ASC");
          foreach (var el in els) {
            if (!string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(DS, SD.CurrentUser.Project, el.ElementName))) {
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
        this.Element.InitMeasureData(SD, sNewClass, false);
        DS.WriteElement(SD,this.Element);
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
      await this.InvokeAsync(() => { this.StateHasChanged(); });
    }
    private async Task LoadElement() {
      if (!this.disableSetImage) {
        if (this.Element != null) {
          this.disableSetImage = true;
          if (this.Element.MeasureData?.normalizer == null) {
            this.Element.InitMeasureData(SD, this.Element.Classification.ClassName, false);
          } else if (this.Element.HasImageButNoOrigImage(SD)) {
            this.Element.InitMeasureData(SD, this.Element.Classification.ClassName, false);
          }
          bool bHasImageButNoOrigImage = (this.Element.MeasureData?.normalizePoints == null);
          if (bHasImageButNoOrigImage) {
            this.Raw = false;
          }
          var sUrlOrigImage = "api/photos/" + this.Element.ElementName + "?Project=" + SD.CurrentUser.Project + "&ForceOrig=1";
          string sUrlImage;
          if (this.Raw) {
            sUrlImage = sUrlOrigImage;
          } else {
            if (bHasImageButNoOrigImage) {
              sUrlImage = "api/photos/" + this.Element.ElementName + "?Project=" + SD.CurrentUser.Project + "&ForceOrig=0";
            } else {
              var md = this.Element.MeasureData;
              (int nWidth, int nHeight) = md.GetNormalizedSize();
              var bs = new System.IO.MemoryStream();
              try {
                var sSrcFile = DS.GetFilePathForImage(SD.CurrentUser.Project, this.Element.ElementName, true);
                using (var imgSrc = Image.Load(sSrcFile)) {
                  imgSrc.Mutate(x => x.AutoOrient());
                  var mNormalize = md.GetNormalizeMatrix();
                  using (var imgDst = ImageOperations.TransformAndCropOutOfImage(imgSrc, mNormalize, new Size(nWidth, nHeight))) {
                    imgDst.SaveAsJpeg(bs);
                  }
                }
              } catch { }
              sUrlImage = "data:image/png;base64," + Convert.ToBase64String(bs.ToArray());
            }
          }
          await this.ImageSurveyor.SetImageUrlAsync(sUrlImage, this.Raw, this.Element.MeasureData);
          this.normImageDirty = false;
        }
      }
    }
    private void MeasureDataChanged(object[] oaArgs) {
      var el = (Element)oaArgs[0];
      var md = (Blazor.ImageSurveyor.ImageSurveyorMeasureData)oaArgs[1];
      if (el.ElementProp.IndivData == null) {
        el.ElementProp.IndivData = new Element.IndivData_t {
          MeasuredData = new Element.IndivData_t.MeasuredData_t {
            HeadBodyLength = 0,
          },
        };
      }
      if (string.CompareOrdinal(md.normalizer.NormalizeMethod, "HeadToCloakInPetriDish") == 0) {
        el.ElementProp.IndivData.MeasuredData.HeadBodyLength = this.SD.CurrentProject.ImageNormalizer.NormalizePixelSize * System.Numerics.Vector2.Distance(md.measurePoints[2], md.measurePoints[3]);
      } else if (string.CompareOrdinal(md.normalizer.NormalizeMethod, "HeadToCloakIn50mmCuvette") == 0) {
        el.ElementProp.IndivData.MeasuredData.HeadBodyLength = this.SD.CurrentProject.ImageNormalizer.NormalizePixelSize * System.Numerics.Vector2.Distance(md.measurePoints[2], md.measurePoints[3]);
      }
      el.MeasureData = md;
      this.normImageDirty = true;
      Utilities.CallDelayed(200, this.RefreshPatternImg);
    }
    private async void RefreshPatternImg(object[] oaArgs) {
      string sPatternImgSrc = "";
      try {
        var el = this.Element;
        if (el != null && ElementClassification.IsNormed(el.Classification.ClassName)) {
          sPatternImgSrc = Utilities.GetPatternImgSource(el, this.DS, this.SD);
        }
      } catch {
      } finally {
        this.PatternImgSrc = sPatternImgSrc;
        await this.InvokeAsync(() => this.StateHasChanged());
      }
    }
    private async Task ResetPositions_Clicked(Element el) {
      var sFilePath = this.DS.GetFilePathForImage(this.SD.CurrentUser.Project, el.ElementName, true);
      if (System.IO.File.Exists(sFilePath)) {
        using (var img = Image.Load(sFilePath)) {
          img.Mutate(x => x.AutoOrient());
          int w = img.Width;
          int h = img.Height;
          if (ElementClassification.IsNormed(this.Element?.Classification?.ClassName)) {
            var normalizer = SD.CurrentProject.ImageNormalizer;
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
  }
}
