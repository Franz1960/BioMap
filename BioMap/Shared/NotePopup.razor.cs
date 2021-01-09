using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Blazorise;

namespace BioMap.Shared
{
  public partial class NotePopup : ComponentBase
  {
    [Parameter]
    public EventCallback<ProtocolEntry> Changed { get; set; }
    //
    private Modal modalRef;
    private IconName sizeButtonIconName = IconName.Expand;
    private bool centered = false;
    private ModalSize modalSize = ModalSize.Large;
    private int? maxHeight = 80;
    public ProtocolEntry ProtocolEntry { get; private set; } = null;
    public ProtocolEntry OrigProtocolEntry { get; private set; } = null;
    private Boolean _Show { get; set; }
    //
    protected override async Task OnInitializedAsync() {
      await base.OnInitializedAsync();
    }
    protected override async Task OnAfterRenderAsync(bool firstRender) {
      await base.OnAfterRenderAsync(firstRender);
      if (firstRender) {
      }
    }
    public void Show(ProtocolEntry pe) {
      this.ProtocolEntry = pe;
      this.OrigProtocolEntry = new ProtocolEntry { CreationTime=pe.CreationTime,Author=pe.Author,Text=pe.Text };
      this.modalRef.Show();
    }
    public void Hide() {
      this.modalRef.Hide();
    }
    public void Save() {
      DS.AddOrUpdateProtocolEntry(SD,ProtocolEntry);
      if (!string.IsNullOrEmpty(OrigProtocolEntry.Text) && string.CompareOrdinal(OrigProtocolEntry.Text,ProtocolEntry.Text)!=0) {
        DS.AddLogEntry(SD,"Note changed. "+ConvInvar.ToString(ProtocolEntry.CreationTime)+" / "+ProtocolEntry.Author+": "+OrigProtocolEntry.Text+" --> "+ProtocolEntry.Text);
        Changed.InvokeAsync(ProtocolEntry);
      } else if (string.IsNullOrEmpty(OrigProtocolEntry.Text) && !string.IsNullOrEmpty(ProtocolEntry.Text)) {
        Changed.InvokeAsync(ProtocolEntry);
      }
    }
    private void sizeButtonClicked() {
      if (this.modalSize==ModalSize.Large) {
        this.modalSize=ModalSize.ExtraLarge;
        this.sizeButtonIconName=IconName.Compress;
      } else {
        this.modalSize=ModalSize.Large;
        this.sizeButtonIconName=IconName.Expand;
      }
    }
  }
}
