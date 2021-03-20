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
    public partial class Identify : ComponentBase
    {
        private Element[] ElementsToCompare = new Element[0];
        private Element ElementToCompare = null;
        private string PatternImgSrc = "";
        private string PatternCompareImgSrc = "";
        private PhotoPopup PhotoPopup1;
        private Element Element
        {
            get => this._Element;
            set
            {
                if (value != this._Element)
                {
                    if (this._Element != null)
                    {
                        DS.WriteElement(SD, this._Element);
                    }
                    this._Element = value;
                    SD.SelectedElementName = this._Element?.ElementName;
                    if (this._Element != null)
                    {
                        this.PatternImgSrc = Utilities.GetPatternImgSource(this._Element, DS, SD);
                        if (this.ElementsToCompare.Length >= 2)
                        {
                            var lElements = Element.GetPrunedListSortedBySimilarity(this.ElementsToCompare, this.Element);
                            this.ElementsToCompare = lElements.ToArray();
                        }
                    }
                };
            }
        }
        private Element _Element = null;
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            SD.Filters.FilteringTarget = Filters.FilteringTargetEnum.Elements;
            SD.Filters.FilterChanged += this.Filters_FilterChanged;
            await RefreshData();
            await this.SelectElement();
            NM.LocationChanged += NM_LocationChanged;
        }
        private async void Filters_FilterChanged(object sender, EventArgs e)
        {
            await this.RefreshData();
            await this.SelectElement();
            this.StateHasChanged();
        }
        private void NM_LocationChanged(object sender, LocationChangedEventArgs e)
        {
            NM.LocationChanged -= NM_LocationChanged;
            SD.Filters.FilterChanged -= this.Filters_FilterChanged;
            this.Element = null;
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
            }
        }
        private async Task SelectElement()
        {
            this.Element = (await DS.GetElementsAsync(SD, SD.Filters, "elements.name='" + SD.SelectedElementName + "'")).FirstOrDefault();
        }
        private async Task RefreshData()
        {
            await this.RefreshElementsToCompare();
        }
        private async Task RefreshElementsToCompare()
        {
            var els = await DS.GetElementsAsync(SD, null,
              "(elements.classification LIKE '%\"ClassName\":\"ID photo\"%')",
              "elements.creationtime ASC");
            var lElements = new List<Element>();
            foreach (var el in els)
            {
                if (!string.IsNullOrEmpty(PhotoController.GetFilePathForExistingImage(SD.CurrentUser.Project, el.ElementName)))
                {
                    lElements.Add(el);
                }
            }
            this.ElementsToCompare = lElements.ToArray();
        }
        private async Task IdentifyAs(Element elementToCompare)
        {
        }
        private void ElementName_Click(Element el)
        {
            this.PhotoPopup1.Show(el);
        }
        private void ElementFromList_Clicked(Element el)
        {
            this.ElementToCompare = el;
            if (this.ElementToCompare != null)
            {
                this.PatternCompareImgSrc = Utilities.GetPatternImgSource(this.ElementToCompare, DS, SD);

                this.Element.CalcSimilarity(this.ElementToCompare);

            }
            this.StateHasChanged();
        }
    }
}
