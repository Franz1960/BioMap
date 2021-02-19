using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

#nullable enable

namespace Blazor.ImageSurveyor
{
  public partial class ImageSurveyor : IDisposable
  {
    [Parameter]
    public bool Raw { get; set; }

    [Parameter]
    public ImageSurveyorOptions? Options { get; set; }

    public event EventHandler? AfterRender;
    public event EventHandler<ImageSurveyorMeasureData>? MeasureDataChanged;

    [Parameter]
    public string Style { get; set; }="";

    protected ElementReference divMain { get; set; }

    private bool Initialized=false;
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
        await InitAsync(Options);
      }
      {
        var handler=this.AfterRender;
        if (handler!=null) {
          handler.Invoke(this,EventArgs.Empty);
        }
      }
    }

    [Inject]
    public IJSRuntime JsRuntime { get; protected set; } = default!;

    private DotNetObjectReference<ImageSurveyor>? thisRef=null;

    public async Task InitAsync(ImageSurveyorOptions? options = null) {
      this.thisRef = DotNetObjectReference.Create(this);
      await this.JsRuntime.InvokeVoidAsync("PrepPic.init",this.thisRef);
      await this.JsRuntime.InvokeVoidAsync("PrepPic.PrepareDisplay",this.divMain);
      // Load image if already available.
      if (!string.IsNullOrEmpty(this.ImageUrl)) {
        await this.JsRuntime.InvokeVoidAsync("PrepPic.setImage",this.ImageUrl,this.Raw,this.MeasureDataJson);
      }
      this.Initialized=true;
    }
    public string ImageUrl { get; private set; }="";
    public string MeasureDataJson { get; private set; }="";
    public async Task SetImageUrlAsync(string sImageUrl,bool bRaw,ImageSurveyorMeasureData measureData) {
      this.Raw=bRaw;
      var sMeasureDataJson=JsonConvert.SerializeObject(measureData);
      if (string.CompareOrdinal(sImageUrl,this.ImageUrl)!=0 || string.CompareOrdinal(sMeasureDataJson,this.MeasureDataJson)!=0) {
        this.ImageUrl=sImageUrl;
        this.MeasureDataJson=sMeasureDataJson;
        if (this.Initialized) {
          await this.JsRuntime.InvokeVoidAsync("PrepPic.setImage",this.ImageUrl,bRaw,sMeasureDataJson);
        }
      }
    }
    [JSInvokable]
    public void MeasureData_Changed(string sJsonMeasureData) {
      var md=JsonConvert.DeserializeObject<ImageSurveyorMeasureData>(sJsonMeasureData);
      if (this.Raw) {
        if (string.CompareOrdinal(md.normalizer.NormalizeMethod,"HeadToCloakInPetriDish")==0) {
          var mNormalize=md.GetNormalizeMatrix();
          md.measurePoints[2]=System.Numerics.Vector2.Transform(md.measurePoints[0],mNormalize);
          md.measurePoints[3]=System.Numerics.Vector2.Transform(md.measurePoints[1],mNormalize);
        }
      }
      {
        var handler=this.MeasureDataChanged;
        if (handler!=null) {
          handler.Invoke(this,md);
        }
      }
    }
    public void Dispose() {
      this.thisRef?.Dispose();
    }
  }
}
