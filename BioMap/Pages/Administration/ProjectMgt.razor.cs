using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BioMap.Shared;
using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BioMap.Pages.Administration
{
  public partial class ProjectMgt : ComponentBase
  {
    [Inject]
    protected NavigationManager NM { get; set; }
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private Modal progressModalRef;
    private int progressCompletion = 0;
    private readonly List<string> messages = new();
    //
    protected override void OnInitialized() {
      base.OnInitialized();
      this.NM.LocationChanged += this.NM_LocationChanged;
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      this.NM.LocationChanged -= this.NM_LocationChanged;
      this.DS.WriteProject(this.SD, this.SD.CurrentProject);
    }
    private async Task SaveGpsDataInOriginalImages_Clicked(ChangeEventArgs e) {
      this.SD.CurrentProject.SaveGpsDataInOriginalImages = bool.Parse(e.Value.ToString());
      this.SD.DS.WriteProject(this.SD, this.SD.CurrentProject);
      {
        this.progressCompletion = 0;
        await this.progressModalRef.Show();
        try {
          await this.DS.AddGpsDataToImages(this.SD.CurrentProject.SaveGpsDataInOriginalImages, this.SD, (completion) => {
            this.progressCompletion = completion;
            this.InvokeAsync(() => this.StateHasChanged());
          });
        } finally {
          await this.InvokeAsync(async () => { await this.progressModalRef.Hide(); this.StateHasChanged(); });
        }
      }
    }
    private async Task MigratePhotoTimes_Clicked() {
      await Task.Run(() => {
        Element[] els = this.DS.GetElements(this.SD);
        foreach (Element el in els.ToArray()) {
          if (el.HasPhotoData()) {
            el.AdjustTimeFromPhoto(this.SD);
            this.DS.WriteElement(this.SD, el);
          }
        }
      });
    }
    private async Task MigrateGenders_Clicked() {
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      try {
        await Migration.MigrateGenders(this.SD, (completion) => {
          this.progressCompletion = completion;
          this.InvokeAsync(() => this.StateHasChanged());
        });
      } finally {
        await this.InvokeAsync(async () => { await this.progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task MigrateGenderFeatures_Clicked() {
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      try {
        await Migration.MigrateGenderFeatures(this.SD, (completion) => {
          this.progressCompletion = completion;
          this.InvokeAsync(() => this.StateHasChanged());
        });
      } finally {
        await this.InvokeAsync(async () => { await this.progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task MigrateImageSize_Clicked() {
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      try {
        await Migration.MigrateImageSize(this.SD, (completion) => {
          this.progressCompletion = completion;
          this.InvokeAsync(() => this.StateHasChanged());
        });
      } finally {
        await this.InvokeAsync(async () => { await this.progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task ReadMassFromComments_Clicked() {
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      try {
        Element[] aElements = this.DS.GetElements(this.SD);
        var regex = new Regex("\\s*(\\d*[,.]?\\d*)\\s?g");
        foreach (Element aElement in aElements) {
          if (aElement.GetMass() == 0) {
            var match = regex.Match(aElement.ElementProp.UploadInfo.Comment);
            if (match.Success && match.Groups.Count >= 1) {
              string sValue = match.Groups[1].Value.Replace(',', '.');
              if (double.TryParse(sValue, System.Globalization.NumberStyles.Float, System.Globalization.NumberFormatInfo.InvariantInfo, out double mass)) {
                aElement.ElementProp.IndivData.MeasuredData.Mass = mass;
                this.DS.WriteElement(this.SD, aElement);
              }
            }
          }
        }
      } finally {
        await this.InvokeAsync(async () => { await this.progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private void EditAoi_Clicked() {
      this.NM.NavigateTo("/Maps/AoiEdit");
    }
    private async Task RecalculateAll_Clicked() {
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      try {
        Element[] aElements = this.DS.GetElements(this.SD);
        foreach (Element aElement in aElements) {
          this.DS.WriteElement(this.SD, aElement);
        }
      } finally {
        await this.InvokeAsync(async () => { await this.progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task OnInputFileChange(InputFileChangeEventArgs e, bool bConf_not_Docs) {
      int maxAllowedFiles = 30;
      int nUploadedFiles = 0;
      this.messages.Clear();
      this.progressCompletion = 0;
      await this.progressModalRef.Show();
      try {
        IReadOnlyList<IBrowserFile> files = e.GetMultipleFiles(maxAllowedFiles);
        for (int idxFile = 0; idxFile < files.Count; idxFile++) {
          IBrowserFile docFile = files[idxFile];
          this.progressCompletion = (int)Math.Round(((idxFile + 0.5) * 100) / files.Count);
          await this.InvokeAsync(() => this.StateHasChanged());
          try {
            System.IO.Stream docStream = docFile.OpenReadStream(40000000);
            string sDestFilePath = System.IO.Path.Combine(
              bConf_not_Docs ? this.DS.GetConfDir(this.SD.CurrentUser.Project) : this.DS.GetDocsDir(this.SD.CurrentUser.Project),
              docFile.Name);
            using (var destStream = new System.IO.FileStream(sDestFilePath, System.IO.FileMode.Create)) {
              await docStream.CopyToAsync(destStream);
              destStream.Close();
            }
            nUploadedFiles++;
          } catch (Exception ex) {
            this.messages.Add(ex.ToString());
            await this.InvokeAsync(() => this.StateHasChanged());
          }
          this.progressCompletion = (int)Math.Round(((idxFile + 1.0) * 100) / files.Count);
          await this.InvokeAsync(() => this.StateHasChanged());
        }
        this.messages.Add(string.Format(this.Localize["{0} files uploaded."], nUploadedFiles.ToString()));
        await this.InvokeAsync(() => this.StateHasChanged());
      } finally {
        if (this.messages.Count < 1) {
          await this.InvokeAsync(async () => { await this.progressModalRef.Hide(); this.StateHasChanged(); });
        }
      }
    }
  }
}
