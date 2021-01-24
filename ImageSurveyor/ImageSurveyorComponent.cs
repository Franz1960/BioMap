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

    /// <summary>
    /// Default height 500px
    /// Used as style atribute "height: {Height}"
    /// </summary>
    [Parameter]
    public string Height {
      get => _height;
      set => _height = value ?? "500px";
    }

    protected string StyleStr => $"height: {Height};";

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
      var measureData=JsonConvert.DeserializeObject<ImageSurveyorMeasureData>(sJsonMeasureData);
      {
        var handler=this.MeasureDataChanged;
        if (handler!=null) {
          handler.Invoke(this,measureData);
        }
      }
    }
    public void Dispose() {
      this.thisRef?.Dispose();
    }
  }
}
