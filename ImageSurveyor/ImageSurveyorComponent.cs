using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

#nullable enable

namespace Blazor.ImageSurveyor
{
  public class ImageSurveyorComponent : ComponentBase, IDisposable
  {
    [Parameter]
    public bool Raw { get; set; }

    [Parameter]
    public ImageSurveyorOptions? Options { get; set; }

    public event EventHandler? AfterRender;
    public event EventHandler<ImageSurveyorMeasureData>? MeasureDataChanged;

    private string _height = "500px";

    [Parameter]
    public string Style { get; set; }="";

    protected ElementReference divMain { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender) {
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

    private DotNetObjectReference<ImageSurveyorComponent>? thisRef=null;

    public async Task InitAsync(ImageSurveyorOptions? options = null) {
      this.thisRef = DotNetObjectReference.Create(this);
      await this.JsRuntime.InvokeVoidAsync("PrepPic.init",this.thisRef);
      await this.JsRuntime.InvokeVoidAsync("PrepPic.PrepareDisplay",this.divMain);
    }
    public string ImageUrl { get; private set; }="";
    public string MeasureDataJson { get; private set; }="";
    public async Task SetImageUrlAsync(string sImageUrl,bool bRaw,ImageSurveyorMeasureData measureData) {
      this.Raw=bRaw;
      var sMeasureDataJson=JsonConvert.SerializeObject(measureData);
      if (string.CompareOrdinal(sImageUrl,this.ImageUrl)!=0 || string.CompareOrdinal(sMeasureDataJson,this.MeasureDataJson)!=0) {
        this.ImageUrl=sImageUrl;
        this.MeasureDataJson=sMeasureDataJson;
        await this.JsRuntime.InvokeVoidAsync("PrepPic.setImage",this.ImageUrl,bRaw,sMeasureDataJson);
        await Task.Delay(100);
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
