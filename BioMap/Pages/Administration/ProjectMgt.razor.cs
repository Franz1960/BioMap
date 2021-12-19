using System;
using System.Collections.Generic;
using System.Linq;
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
    private List<string> messages = new List<string>();
    //
    protected override void OnInitialized() {
      base.OnInitialized();
      NM.LocationChanged += NM_LocationChanged;
    }
    private string NormalizeMethod {
      get {
        return SD.CurrentProject.ImageNormalizer.NormalizeMethod;
      }
      set {
        if (value != SD.CurrentProject.ImageNormalizer.NormalizeMethod) {
          SD.CurrentProject.ImageNormalizer.NormalizeMethod = value;
        }
      }
    }
    private void NM_LocationChanged(object sender, LocationChangedEventArgs e) {
      NM.LocationChanged -= NM_LocationChanged;
      DS.WriteProject(SD, SD.CurrentProject);
    }
    private async Task Import_Clicked() {
      progressCompletion = 0;
      progressModalRef.Show();
      try {
        await Migration.MigrateData(SD, (completion) =>
        {
          progressCompletion = completion;
          this.InvokeAsync(() => { this.StateHasChanged(); });
        });
      } finally {
        await this.InvokeAsync(() => { progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task MigratePhotoTimes_Clicked() {
      var els = this.DS.GetElements(this.SD);
      foreach (var el in els.ToArray()) {
        if (el.HasPhotoData()) {
          el.AdjustTimeFromPhoto(this.SD);
          this.DS.WriteElement(this.SD, el);
        }
      }
    }
    private async Task MigrateCategories_Clicked() {
      progressCompletion = 0;
      progressModalRef.Show();
      try {
        await Migration.MigrateCategories(SD, (completion) =>
        {
          progressCompletion = completion;
          this.InvokeAsync(() => { this.StateHasChanged(); });
        });
      } finally {
        await this.InvokeAsync(() => { progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task MigrateGenders_Clicked() {
      progressCompletion = 0;
      progressModalRef.Show();
      try {
        await Migration.MigrateGenders(SD, (completion) =>
        {
          progressCompletion = completion;
          this.InvokeAsync(() => { this.StateHasChanged(); });
        });
      } finally {
        await this.InvokeAsync(() => { progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task MigrateGenderFeatures_Clicked() {
      progressCompletion = 0;
      progressModalRef.Show();
      try {
        await Migration.MigrateGenderFeatures(SD, (completion) =>
        {
          progressCompletion = completion;
          this.InvokeAsync(() => { this.StateHasChanged(); });
        });
      } finally {
        await this.InvokeAsync(() => { progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task MigrateImageSize_Clicked() {
      progressCompletion = 0;
      progressModalRef.Show();
      try {
        await Migration.MigrateImageSize(SD, (completion) =>
        {
          progressCompletion = completion;
          this.InvokeAsync(() => { this.StateHasChanged(); });
        });
      } finally {
        await this.InvokeAsync(() => { progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private void EditAoi_Clicked() {
      NM.NavigateTo("/Maps/AoiEdit");
    }
    private async Task RecalculateAll_Clicked() {
      progressCompletion = 0;
      progressModalRef.Show();
      try {
      } finally {
        await this.InvokeAsync(() => { progressModalRef.Hide(); this.StateHasChanged(); });
      }
    }
    private async Task OnInputFileChange(InputFileChangeEventArgs e, bool bConf_not_Docs) {
      var maxAllowedFiles = 30;
      int nUploadedFiles = 0;
      messages.Clear();
      progressCompletion = 0;
      progressModalRef.Show();
      try {
        var files = e.GetMultipleFiles(maxAllowedFiles);
        for (int idxFile = 0; idxFile < files.Count; idxFile++) {
          var docFile = files[idxFile];
          progressCompletion = (int)Math.Round(((idxFile + 0.5) * 100) / files.Count);
          await this.InvokeAsync(() => { this.StateHasChanged(); });
          try {
            var docStream = docFile.OpenReadStream(20000000);
            var sDestFilePath = System.IO.Path.Combine(
              bConf_not_Docs ?DS.GetConfDir(SD.CurrentUser.Project) : DS.GetDocsDir(SD.CurrentUser.Project),
              docFile.Name);
            using (var destStream = new System.IO.FileStream(sDestFilePath, System.IO.FileMode.Create)) {
              await docStream.CopyToAsync(destStream);
              destStream.Close();
            }
            nUploadedFiles++;
          } catch (Exception ex) {
            messages.Add(ex.ToString());
            await this.InvokeAsync(() => { this.StateHasChanged(); });
          }
          progressCompletion = (int)Math.Round(((idxFile + 1.0) * 100) / files.Count);
          await this.InvokeAsync(() => { this.StateHasChanged(); });
        }
        messages.Add(string.Format(Localize["{0} files uploaded."], nUploadedFiles.ToString()));
        await this.InvokeAsync(() => { this.StateHasChanged(); });
      } finally {
        if (messages.Count < 1) {
          await this.InvokeAsync(() => { progressModalRef.Hide(); this.StateHasChanged(); });
        }
      }
    }
  }
}
