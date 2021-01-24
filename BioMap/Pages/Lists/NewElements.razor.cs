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

namespace BioMap.Pages.Lists
{
  public partial class NewElements : ComponentBase
  {
    private Element[] Elements = new Element[0];
    private PhotoPopup PhotoPopup1;
    protected Blazor.ImageSurveyor.ImageSurveyor imageSurveyor;
    private Blazor.ImageSurveyor.ImageSurveyor imageSurveyorPrev;
    private bool disableSetImage=false;
    private bool elementChanged=false;
    private Element ElementToIdentify {
      get => this._ElementToIdentify;
      set {
        if (value!=this._ElementToIdentify) {
          if (this._ElementToIdentify!=null) {
            DS.WriteElement(SD,this._ElementToIdentify);
          }
          this._ElementToIdentify=value;
        };
      }
    }
    private Element _ElementToIdentify=null;
    private Element ElementToMeasure {
      get => this._ElementToMeasure;
      set {
        if (value!=this._ElementToMeasure) {
          if (this._ElementToMeasure!=null) {
            DS.WriteElement(SD,this._ElementToMeasure);
          }
          this._ElementToMeasure=value;
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
      await RefreshData();
      NM.LocationChanged+=NM_LocationChanged;
    }
    private void NM_LocationChanged(object sender,LocationChangedEventArgs e) {
      NM.LocationChanged-=NM_LocationChanged;
      this.ElementToIdentify=null;
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
            await this.LoadElementToMeasure(this.ElementToMeasure);
          }
        }
      }
    }
    private async void imageSurveyor_AfterRenderEvent(object sender,EventArgs e) {
      if (this.ElementToMeasure!=null) {
        await this.LoadElementToMeasure(this.ElementToMeasure);
      }
    }
    private void imageSurveyor_MeasureDataChanged(object sender,Blazor.ImageSurveyor.ImageSurveyorMeasureData measureData) {
      if (this.ElementToMeasure!=null) {
        this.MeasureDataChanged(this.ElementToMeasure,measureData);
      }
    }
    private async Task RefreshData() {
      this.Elements = Elements = await DS.GetElementsAsync(SD,SD.Filters,"elements.classification LIKE '%\"ClassName\":\"New\"%'","elements.creationtime ASC");
    }
    private void OnSelectClick(Element el) {
      this.PhotoPopup1.Show(el);
    }
    private async Task Measure_Clicked(Element el) {
      this.ElementToMeasure=el;
      this.ElementToIdentify=null;
      this.disableSetImage=false;
      this.elementChanged=true;
      await this.InvokeAsync(()=>StateHasChanged());
    }
    private async Task Identify_Clicked(Element el) {
      this.ElementToMeasure=null;
      this.ElementToIdentify=el;
      this.disableSetImage=false;
      this.elementChanged=true;
      await this.InvokeAsync(()=>StateHasChanged());
    }
    private async Task LoadElementToMeasure(Element el) {
      if (!this.disableSetImage) {
        if (this.ElementToMeasure!=null) {
          this.disableSetImage=true;
          await this.imageSurveyor.SetImageUrlAsync(
            "api/photos/"+this.ElementToMeasure.ElementName+"?Project="+SD.CurrentUser.Project+"&ForceOrig="+(this.Raw?"1":"0"),
            this.Raw,
            this.ElementToMeasure.MeasureData);
        }
      }
    }
    private void MeasureDataChanged(Element el,Blazor.ImageSurveyor.ImageSurveyorMeasureData md) {
      if (this.Raw) {
        var mNormalize=md.GetNormalizeMatrix();
        md.measurePoints[2]=System.Numerics.Vector2.Transform(md.measurePoints[0],mNormalize);
        md.measurePoints[3]=System.Numerics.Vector2.Transform(md.measurePoints[1],mNormalize);
      }
      el.ElementProp.IndivData.MeasuredData.HeadBodyLength=0.1f*System.Numerics.Vector2.Distance(md.measurePoints[2],md.measurePoints[3]);
      el.MeasureData=md;
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
            },
          };
        }
        this.disableSetImage=false;
        await this.LoadElementToMeasure(el);
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