using System;
using System.Collections.Generic;
using System.Linq;
using BioMap.Shared;
using Microsoft.AspNetCore.Components;

namespace BioMap.Pages.Lists
{
  public partial class Notes : ComponentBase
  {
    [Inject]
    protected NavigationManager NM { get; set; }
    [Inject]
    protected DataService DS { get; set; }
    [Inject]
    protected SessionData SD { get; set; }
    //
    private NotePopup refNotePopup;
    private ProtocolEntry[] ProtocolEntries = new ProtocolEntry[0];
    protected override void OnInitialized() {
      base.OnInitialized();
      this.SD.Filters.FilterChanged += (sender, ev) => {
        this.RefreshData();
        base.InvokeAsync(this.StateHasChanged);
      };
      this.RefreshData();
    }
    private void RefreshData() {
      this.ProtocolEntries = this.DS.GetProtocolEntries(this.SD, this.SD.Filters, "", "notes.dt DESC");
    }
    private void AddProtocolEntry() {
      var pe = new ProtocolEntry {
        CreationTime = DateTime.Now,
        Author = this.SD.CurrentUser.EMail,
        Text = "",
      };
      this.refNotePopup.Show(pe);
    }
  }
}
