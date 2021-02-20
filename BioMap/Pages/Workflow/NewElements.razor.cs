using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Convolution;
using BioMap.ImageProc;
using BioMap.Shared;

namespace BioMap.Pages.Workflow
{
  public partial class NewElements : ComponentBase
  {
    private Element[] Elements = new Element[0];
    private int? SelectedElementIndex=null;
    private PhotoPopup PhotoPopup1;
    protected Blazor.ImageSurveyor.ImageSurveyor imageSurveyor;
    private Blazor.ImageSurveyor.ImageSurveyor imageSurveyorPrev;
    private string PatternImgSrc="";
    private float ShareOfYellow=0;
    private float AsymmetryOfYellow=0;
    private float Entropy=0;
    private bool disableSetImage=false;
    private bool elementChanged=false;
    private bool normImageDirty=false;
    private Element Element {
      get => this._Element;
      set {
        if (value!=this._Element) {
          if (this._Element!=null) {
            DS.WriteElement(SD,this._Element);
            // Bild normieren.
            if (this.normImageDirty) {
              if (this._Element.MeasureData.normalizePoints!=null) {
                var sSrcFile=DS.GetFilePathForImage(SD.CurrentUser.Project,this._Element.ElementName,true);
                var sDstFile=DS.GetFilePathForImage(SD.CurrentUser.Project,this._Element.ElementName,false);
                var md=this._Element.MeasureData;
                (int nWidth, int nHeight)=md.GetNormalizedSize();
                using (var imgDst = new Image<Rgb24>(nWidth,nHeight,Color.Gray)) {
                  try {
                    using (var imgSrc = Image.Load(sSrcFile)) {
                      imgSrc.Mutate(x => x.AutoOrient());
                      var mNormalize = md.GetNormalizeMatrix();
                      var atb = new AffineTransformBuilder();
                      atb.AppendMatrix(mNormalize);
                      imgSrc.Mutate(x => x.Transform(atb));
                      imgDst.Mutate(x => x.DrawImage(imgSrc,1f));
                    }
                  } catch { }
                  imgDst.SaveAsJpeg(sDstFile);
                }
              }
            }
          }
          this._Element=value;
          SD.SelectedElementName=this._Element?.ElementName;
        };
      }
    }
    private Element _Element=null;
    private bool Raw {
      get {
        return this._Raw;
      }
      set {
        if (value!=this._Raw) {
          this._Raw=value;
          this.disableSetImage=false;
          this.elementChanged=true;
          this.StateHasChanged();
        }
      }
    }
    private bool _Raw=true;
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
      SD.Filters.FilteringTarget=Filters.FilteringTargetEnum.Elements;
      SD.Filters.FilterChanged+=this.Filters_FilterChanged;
      await RefreshData();
      await this.SelectElement();
      NM.LocationChanged+=NM_LocationChanged;
    }
    private async void Filters_FilterChanged(object sender,EventArgs e) {
      await this.RefreshData();
      await this.SelectElement();
      this.disableSetImage=false;
      this.elementChanged=true;
      this.StateHasChanged();
    }
    private void NM_LocationChanged(object sender,LocationChangedEventArgs e) {
      NM.LocationChanged-=NM_LocationChanged;
      SD.Filters.FilterChanged-=this.Filters_FilterChanged;
      this.Element=null;
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
      if (this.imageSurveyor!=this.imageSurveyorPrev) {
        if (this.imageSurveyorPrev!=null) {
          this.imageSurveyorPrev.AfterRender-=this.imageSurveyor_AfterRenderEvent;
          this.imageSurveyorPrev.MeasureDataChanged-=this.imageSurveyor_MeasureDataChanged;
        }
        if (this.imageSurveyor!=null) {
          this.imageSurveyor.AfterRender+=this.imageSurveyor_AfterRenderEvent;
          this.imageSurveyor.MeasureDataChanged+=this.imageSurveyor_MeasureDataChanged;
        }
        this.imageSurveyorPrev=this.imageSurveyor;
      } else {
        if (this.elementChanged) {
          if (this.Element!=null) {
            this.elementChanged=false;
            await this.LoadElement();
          }
        }
      }
    }
    private async void imageSurveyor_AfterRenderEvent(object sender,EventArgs e) {
      if (this.Element!=null) {
        await this.LoadElement();
        Utilities.CallDelayed(200,this.RefreshPatternImg);
      }
    }
    private void imageSurveyor_MeasureDataChanged(object sender,Blazor.ImageSurveyor.ImageSurveyorMeasureData measureData) {
      if (this.Element!=null) {
        Utilities.CallDelayed(200,this.MeasureDataChanged,this.Element,measureData);
      }
    }
    private async Task SelectElement() {
      this.Element=(await DS.GetElementsAsync(SD,SD.Filters,"elements.name='"+SD.SelectedElementName+"'")).FirstOrDefault();
      if (this.Element==null) {
        await this.OnSelectNext();
      } else {
        for (int i=0;i<this.Elements.Length;i++) {
          if (this.Elements[i].ElementName==this.Element?.ElementName) {
            this.SelectedElementIndex=i;
            break;
          }
        }
      }
    }
    private async Task RefreshData() {
      this.Elements = await DS.GetElementsAsync(SD,SD.Filters,"elements.classification LIKE '%\"ClassName\":\"New\"%'","elements.creationtime ASC");
    }
    private async Task newClass_Selected(string sNewClass) {
      if (string.CompareOrdinal(sNewClass,this.Element?.Classification?.ClassName)!=0) {
        bool bPrevNormed=ElementClassification.IsNormed(this.Element?.Classification?.ClassName);
        bool bNewNormed=ElementClassification.IsNormed(sNewClass);
        if (bNewNormed!=bPrevNormed) {
          if (this.Element!=null) {
            this.Element.MeasureData.normalizer=
              bNewNormed
              ?
              SD.CurrentProject.ImageNormalizer
              :
              new Blazor.ImageSurveyor.ImageSurveyorNormalizer() { NormalizeMethod="CropRectangle", }
              ;
            this.Element.InitMeasureData(SD,false);
          }
        }
        this.Element.Classification.ClassName=sNewClass;
        this.disableSetImage=false;
        await this.LoadElement();
      }
    }
    private void OnSelectClick(Element el) {
      this.PhotoPopup1.Show(el);
    }
    private async Task OnSelectPrev() {
      this.Element=null;
      await this.RefreshData();
      if (this.SelectedElementIndex.HasValue && this.SelectedElementIndex.Value>=1 && this.Elements.Length>=1) {
        this.SelectedElementIndex--;
      } else if (this.Elements.Length>=1) {
        this.SelectedElementIndex=this.Elements.Length-1;
      } else {
        this.SelectedElementIndex=null;
      }
      if (this.SelectedElementIndex.HasValue) {
        this.Element=this.Elements[this.SelectedElementIndex.Value];
      } else {
        this.Element=null;
      }
      this.disableSetImage=false;
      this.elementChanged=true;
      StateHasChanged();
    }
    private async Task OnSelectNext() {
      this.Element=null;
      await this.RefreshData();
      if (this.SelectedElementIndex.HasValue && this.SelectedElementIndex.Value<this.Elements.Length-1) {
        this.SelectedElementIndex++;
      } else if (this.Elements.Length>=1) {
        this.SelectedElementIndex=0;
      } else {
        this.SelectedElementIndex=null;
      }
      if (this.SelectedElementIndex.HasValue) {
        this.Element=this.Elements[this.SelectedElementIndex.Value];
      } else {
        this.Element=null;
      }
      this.disableSetImage=false;
      this.elementChanged=true;
      StateHasChanged();
    }
    private async Task Measure_Clicked(Element el) {
      this.Element=el;
      this.disableSetImage=false;
      this.elementChanged=true;
      await this.InvokeAsync(()=>StateHasChanged());
    }
    private async Task LoadElement() {
      if (!this.disableSetImage) {
        if (this.Element!=null) {
          this.disableSetImage=true;
          if (this.Element.MeasureData==null) {
            this.Element.InitMeasureData(SD,false);
          } else if (this.Element.HasImageButNoOrigImage(SD)) {
            this.Element.MeasureData.normalizePoints=null;
          }
          this.Element.InitMeasureData(SD,true);
          bool bHasImageButNoOrigImage=(this.Element.MeasureData.normalizePoints==null);
          if (bHasImageButNoOrigImage) {
            this.Raw=false;
          }
          var sUrlOrigImage="api/photos/"+this.Element.ElementName+"?Project="+SD.CurrentUser.Project+"&ForceOrig=1";
          string sUrlImage;
          if (this.Raw) {
            sUrlImage=sUrlOrigImage;
          } else {
            if (bHasImageButNoOrigImage) {
              sUrlImage="api/photos/"+this.Element.ElementName+"?Project="+SD.CurrentUser.Project+"&ForceOrig=0";
            } else {
              var md = this.Element.MeasureData;
              (int nWidth, int nHeight)=md.GetNormalizedSize();
              using (var imgDst = new Image<Rgb24>(nWidth,nHeight,Color.Gray)) {
                try {
                  var sSrcFile = DS.GetFilePathForImage(SD.CurrentUser.Project,this.Element.ElementName,true);
                  using (var imgSrc = Image.Load(sSrcFile)) {
                    imgSrc.Mutate(x => x.AutoOrient());
                    var mNormalize = md.GetNormalizeMatrix();
                    var atb = new AffineTransformBuilder();
                    atb.AppendMatrix(mNormalize);
                    imgSrc.Mutate(x => x.Transform(atb));
                    imgDst.Mutate(x => x.DrawImage(imgSrc,1f));
                  }
                } catch { }
                var bs = new System.IO.MemoryStream();
                imgDst.SaveAsJpeg(bs);
                sUrlImage="data:image/png;base64,"+Convert.ToBase64String(bs.ToArray());
              }
            }
          }
          await this.imageSurveyor.SetImageUrlAsync(sUrlImage,this.Raw,this.Element.MeasureData);
          this.normImageDirty=false;
        }
      }
    }
    private void MeasureDataChanged(object[] oaArgs) {
      var el=(Element)oaArgs[0];
      var md=(Blazor.ImageSurveyor.ImageSurveyorMeasureData)oaArgs[1];
      if (el.ElementProp.IndivData==null) {
        el.ElementProp.IndivData=new Element.IndivData_t {
          MeasuredData=new Element.IndivData_t.MeasuredData_t {
            HeadBodyLength=0,
          },
        };
      }
      if (string.CompareOrdinal(md.normalizer.NormalizeMethod,"HeadToCloakInPetriDish")==0) {
        el.ElementProp.IndivData.MeasuredData.HeadBodyLength=0.1f*System.Numerics.Vector2.Distance(md.measurePoints[2],md.measurePoints[3]);
      }
      el.MeasureData=md;
      this.normImageDirty=true;
      //Utilities.CallDelayed(200,this.RefreshPatternImg);
    }
    private async void RefreshPatternImg(object[] oaArgs) {
      try {
        var el=this.Element;
        if (el!=null) {
          var sSrcFile=DS.GetFilePathForImage(SD.CurrentUser.Project,this.Element.ElementName,true);
          var analyseYellowShare = new AnalyseProcessor();
          var analyseEntropy = new AnalyseProcessor();
          using (var imgSrc = Image.Load(sSrcFile)) {
            imgSrc.Mutate(x => x.AutoOrient());
            var md = this.Element.MeasureData;
            (int nWidth, int nHeight)=md.GetPatternSize(300);
            var mPattern = md.GetPatternMatrix(nHeight);
            var atb = new AffineTransformBuilder();
            atb.AppendMatrix(mPattern);
            imgSrc.Mutate(x => x.Transform(atb));
            //imgSrc.Mutate(x => x.SafeCrop(nWidth,nHeight));
            //imgSrc.Mutate(x => x.MaxChroma(0.05f,new[] { new System.Numerics.Vector2(1,100) }));
            //imgSrc.Mutate(x => x.ApplyProcessor(analyseYellowShare));
            //var imgEdges = imgSrc.Clone(x => x.DetectEdges());
            //imgEdges.Mutate(x => x.ApplyProcessor(analyseEntropy));
            //var bs = new System.IO.MemoryStream();
            //imgSrc.SaveAsJpeg(bs);
            //this.PatternImgSrc="data:image/png;base64,"+Convert.ToBase64String(bs.ToArray());
            //this.ShareOfYellow=(float)analyseYellowShare.AnalyseData.ShareOfWhite;
            //this.AsymmetryOfYellow=(float)((analyseYellowShare.AnalyseData.LowerShareOfWhite-analyseYellowShare.AnalyseData.UpperShareOfWhite)/Math.Max(0.01,analyseYellowShare.AnalyseData.ShareOfWhite));
            //this.Entropy=(float)analyseEntropy.AnalyseData.ShareOfWhite;
          }
          await this.InvokeAsync(()=>StateHasChanged());
        }
      } catch { }
    }
    private async Task ResetPositions_Clicked(Element el) {
      var sFilePath = DS.GetFilePathForImage(SD.CurrentUser.Project,el.ElementName,true);
      if (System.IO.File.Exists(sFilePath)) {
        using (var img = Image.Load(sFilePath)) {
          int w=img.Width;
          int h=img.Height;
          if (ElementClassification.IsNormed(this.Element?.Classification?.ClassName)) {
            var normalizer=SD.CurrentProject.ImageNormalizer;
            el.MeasureData=new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
              normalizer=normalizer,
              normalizePoints=normalizer.GetDefaultNormalizePoints(w,h).ToArray(),
              measurePoints=normalizer.GetDefaultMeasurePoints(w,h).ToArray(),
            };
          } else {
            var normalizer = new Blazor.ImageSurveyor.ImageSurveyorNormalizer() {
              NormalizeMethod="CropRectangle",
            };
            el.MeasureData=new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
              normalizer=normalizer,
              normalizePoints=normalizer.GetDefaultNormalizePoints(w,h).ToArray(),
              measurePoints=normalizer.GetDefaultMeasurePoints(w,h).ToArray(),
            };
          }
        }
        this.disableSetImage=false;
        await this.LoadElement();
      }
    }
    private async Task DeleteElement(Element el) {
      DS.DeleteElement(SD,el);
      DS.AddLogEntry(SD,"Deleted element "+el.ElementName);
      await RefreshData();
      await this.InvokeAsync(()=>StateHasChanged());
    }
  }
}