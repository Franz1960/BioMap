using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using BioMap.Shared;

namespace BioMap.Pages.Workflow
{
  public partial class NewElements : ComponentBase
  {
    private Element[] Elements = new Element[0];
    private int? SelectedElementIndex=null;
    private string NewClass="";
    private PhotoPopup PhotoPopup1;
    protected Blazor.ImageSurveyor.ImageSurveyor imageSurveyor;
    private Blazor.ImageSurveyor.ImageSurveyor imageSurveyorPrev;
    private bool disableSetImage=false;
    private bool elementChanged=false;
    private bool normImageDirty=false;
    private Element ElementToMeasure {
      get => this._ElementToMeasure;
      set {
        if (value!=this._ElementToMeasure) {
          if (this._ElementToMeasure!=null && this.normImageDirty) {
            DS.WriteElement(SD,this._ElementToMeasure);
            // Bild normieren.
            if (this._ElementToMeasure.MeasureData.normalizePoints!=null) {
              var sSrcFile=DS.GetFilePathForImage(SD.CurrentUser.Project,this._ElementToMeasure.ElementName,true);
              var sDstFile=DS.GetFilePathForImage(SD.CurrentUser.Project,this._ElementToMeasure.ElementName,false);
              using (var imgSrc = Image.Load(sSrcFile)) {
                imgSrc.Mutate(x => x.AutoOrient());
                var mNormalize=this._ElementToMeasure.MeasureData.GetNormalizeMatrix();
                var atb=new AffineTransformBuilder();
                atb.AppendMatrix(mNormalize);
                imgSrc.Mutate(x => x.Transform(atb));
                imgSrc.Mutate(x => x.Crop(600,600));
                imgSrc.SaveAsJpeg(sDstFile);
              }
            }
          }
          this._ElementToMeasure=value;
          SD.SelectedElementName=this._ElementToMeasure?.ElementName;
        };
      }
    }
    private Element _ElementToMeasure=null;
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
      SD.Filters.FilterChanged+=async (sender,ev) => {
        await this.RefreshData();
        await this.SelectElement();
        this.disableSetImage=false;
        this.elementChanged=true;
        this.StateHasChanged();
      };
      await RefreshData();
      await this.SelectElement();
      NM.LocationChanged+=NM_LocationChanged;
    }
    private void NM_LocationChanged(object sender,LocationChangedEventArgs e) {
      NM.LocationChanged-=NM_LocationChanged;
      this.ElementToMeasure=null;
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
          if (this.ElementToMeasure!=null) {
            this.elementChanged=false;
            await this.LoadElementToMeasure();
          }
        }
      }
    }
    private async void imageSurveyor_AfterRenderEvent(object sender,EventArgs e) {
      if (this.ElementToMeasure!=null) {
        await this.LoadElementToMeasure();
      }
    }
    private void imageSurveyor_MeasureDataChanged(object sender,Blazor.ImageSurveyor.ImageSurveyorMeasureData measureData) {
      if (this.ElementToMeasure!=null) {
        this.MeasureDataChanged(this.ElementToMeasure,measureData);
      }
    }
    private async Task SelectElement() {
      this.ElementToMeasure=(await DS.GetElementsAsync(SD,SD.Filters,"elements.name='"+SD.SelectedElementName+"'")).FirstOrDefault();
      if (this.ElementToMeasure==null) {
        await this.OnSelectNext();
      } else {
        for (int i=0;i<this.Elements.Length;i++) {
          if (this.Elements[i].ElementName==this.ElementToMeasure?.ElementName) {
            this.SelectedElementIndex=i;
            break;
          }
        }
      }
    }
    private async Task RefreshData() {
      this.Elements = await DS.GetElementsAsync(SD,SD.Filters,"elements.classification LIKE '%\"ClassName\":\"New\"%'","elements.creationtime ASC");
    }
    private void OnSelectClick(Element el) {
      this.PhotoPopup1.Show(el);
    }
    private async Task OnSelectPrev() {
      await this.RefreshData();
      if (this.SelectedElementIndex.HasValue && this.SelectedElementIndex.Value>=1 && this.Elements.Length>=1) {
        this.SelectedElementIndex--;
      } else if (this.Elements.Length>=1) {
        this.SelectedElementIndex=this.Elements.Length-1;
      } else {
        this.SelectedElementIndex=null;
      }
      if (this.SelectedElementIndex.HasValue) {
        this.ElementToMeasure=this.Elements[this.SelectedElementIndex.Value];
      } else {
        this.ElementToMeasure=null;
      }
      this.disableSetImage=false;
      this.elementChanged=true;
      StateHasChanged();
    }
    private async Task OnSelectNext() {
      await this.RefreshData();
      if (this.SelectedElementIndex.HasValue && this.SelectedElementIndex.Value<this.Elements.Length-1) {
        this.SelectedElementIndex++;
      } else if (this.Elements.Length>=1) {
        this.SelectedElementIndex=0;
      } else {
        this.SelectedElementIndex=null;
      }
      if (this.SelectedElementIndex.HasValue) {
        this.ElementToMeasure=this.Elements[this.SelectedElementIndex.Value];
      } else {
        this.ElementToMeasure=null;
      }
      this.disableSetImage=false;
      this.elementChanged=true;
      StateHasChanged();
    }
    private async Task Measure_Clicked(Element el) {
      this.ElementToMeasure=el;
      this.disableSetImage=false;
      this.elementChanged=true;
      await this.InvokeAsync(()=>StateHasChanged());
    }
    private async Task LoadElementToMeasure() {
      if (!this.disableSetImage) {
        if (this.ElementToMeasure!=null) {
          this.disableSetImage=true;
          if (this.ElementToMeasure.MeasureData==null) {
            this.ElementToMeasure.MeasureData=new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
              method="HeadToCloakInPetriDish",
              normalizePoints=null,
              measurePoints=new[] {
                new System.Numerics.Vector2 { X=100,Y=300},
                new System.Numerics.Vector2 { X=500,Y=300 },
                new System.Numerics.Vector2 { X=300,Y=100 },
                new System.Numerics.Vector2 { X=300,Y=400 },
              },
            };
          } else if (this.ElementToMeasure.HasImageButNoOrigImage(SD)) {
            this.ElementToMeasure.MeasureData.normalizePoints=null;
          }
          bool bHasOrigImageButNoImage=this.ElementToMeasure.HasOrigImageButNoImage(SD);
          if (bHasOrigImageButNoImage) {
            this.ElementToMeasure.MeasureData.normalizePoints=new[] {
              new System.Numerics.Vector2 { X=100,Y=100},
              new System.Numerics.Vector2 { X=300,Y=300 },
              new System.Numerics.Vector2 { X=100,Y=500 },
            };
          }
          bool bHasImageButNoOrigImage=(this.ElementToMeasure.MeasureData.normalizePoints==null);
          if (bHasImageButNoOrigImage) {
            this.Raw=false;
          } else if (bHasOrigImageButNoImage) {
            this.Raw=true;
          }
          var sUrlOrigImage="api/photos/"+this.ElementToMeasure.ElementName+"?Project="+SD.CurrentUser.Project+"&ForceOrig=1";
          string sUrlImage;
          if (this.Raw) {
            sUrlImage=sUrlOrigImage;
          } else {
            if (bHasImageButNoOrigImage) {
              sUrlImage="api/photos/"+this.ElementToMeasure.ElementName+"?Project="+SD.CurrentUser.Project+"&ForceOrig=0";
            } else {
              var sSrcFile=DS.GetFilePathForImage(SD.CurrentUser.Project,this.ElementToMeasure.ElementName,true);
              using (var imgSrc = Image.Load(sSrcFile)) {
                imgSrc.Mutate(x => x.AutoOrient());
                var mNormalize=this.ElementToMeasure.MeasureData.GetNormalizeMatrix();
                var atb=new AffineTransformBuilder();
                atb.AppendMatrix(mNormalize);
                imgSrc.Mutate(x => x.Transform(atb));
                imgSrc.Mutate(x => x.Crop(600,600));
                var bs = new System.IO.MemoryStream();
                imgSrc.SaveAsJpeg(bs);
                sUrlImage="data:image/png;base64,"+Convert.ToBase64String(bs.ToArray());
              }
            }
          }
          await this.imageSurveyor.SetImageUrlAsync(sUrlImage,this.Raw,this.ElementToMeasure.MeasureData);
          this.normImageDirty=false;
        }
      }
    }
    private void MeasureDataChanged(Element el,Blazor.ImageSurveyor.ImageSurveyorMeasureData md) {
      if (this.Raw) {
        var mNormalize=md.GetNormalizeMatrix();
        md.measurePoints[2]=System.Numerics.Vector2.Transform(md.measurePoints[0],mNormalize);
        md.measurePoints[3]=System.Numerics.Vector2.Transform(md.measurePoints[1],mNormalize);
      }
      if (el.ElementProp.IndivData==null) {
        el.ElementProp.IndivData=new Element.IndivData_t {
          MeasuredData=new Element.IndivData_t.MeasuredData_t {
            HeadBodyLength=0,
          },
        };
      }
      el.ElementProp.IndivData.MeasuredData.HeadBodyLength=0.1f*System.Numerics.Vector2.Distance(md.measurePoints[2],md.measurePoints[3]);
      el.MeasureData=md;
      this.normImageDirty=true;
      this.StateHasChanged();
    }
    private async Task ResetPositions_Clicked(Element el) {
      var sFilePath = DS.GetFilePathForImage(SD.CurrentUser.Project,el.ElementName,true);
      if (System.IO.File.Exists(sFilePath)) {
        using (var img = Image.Load(sFilePath)) {
          int w=img.Width;
          int h=img.Height;
          el.MeasureData=new Blazor.ImageSurveyor.ImageSurveyorMeasureData {
            method="HeadToCloakInPetriDish",
            normalizePoints=new [] {
              new System.Numerics.Vector2 { X=w*0.25f,Y=h*0.50f },
              new System.Numerics.Vector2 { X=w*0.75f,Y=h*0.25f },
              new System.Numerics.Vector2 { X=w*0.75f,Y=h*0.75f },
            },
            measurePoints=new [] {
              new System.Numerics.Vector2 { X=w*0.50f,Y=h*0.25f },
              new System.Numerics.Vector2 { X=w*0.50f,Y=h*0.75f },
              new System.Numerics.Vector2 { X=w*0.50f,Y=h*0.25f },
              new System.Numerics.Vector2 { X=w*0.50f,Y=h*0.75f },
            },
          };
        }
        this.disableSetImage=false;
        await this.LoadElementToMeasure();
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