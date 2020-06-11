
class Identify_t {
  constructor() {
    this.DivMain=null;
    this.Header=null;
    this.Content=null;
    this.IndivDataEdit=new IndivDataEdit();
    this.LabelHeadBodyLength=null;
    this.Element=null;
    this.ImageSize=200;
    this.SampleSize=300;
  }
  PrepareDisplay(divMain) {
    this.DivMain = divMain;
    this.DivMain.CodeBehind = this;
    //
    this.DivMain.appendChild(this.Header = document.createElement('h1'));
    this.DivMain.appendChild(this.Content = document.createElement('div'));
    this.Header.innerText = 'Unke identifizieren';
    let sContent = '' +
      '<div class="w3-container w3-padding">' +
      '  <div class="w3-row-padding">'+
      '    <div class="w3-half" id="Identify_Selection">Bilder</div>'+
      '    <div class="w3-half">'+
      '      <div class="w3-container w3-padding">' +
      '        <div class="w3-row-padding">'+
      '          <div class="w3-twothird" id="Identify_Sample">Sample</div>'+
      '          <div class="w3-third">'+
      '            <div id="Identify_Traits_Nav">' +
      '            </div>' +
      '          </div>'+
      '        </div>'+
      '      </div>' +
      '      <div class="w3-container w3-padding">' +
      '        <span id="IndivDataEdit"></span>' +
      '      </div>' +
      '      <div class="w3-container w3-padding">' +
      '        <div class="w3-row-padding">'+
      '          <div class="w3-third">'+
      '            <h3>Merkmale</h3>' +
      '          </div>' +
      '        </div>' +
      '      </div>' +
      '      <ul class="w3-ul w3-card-4" id="Identify_Traits_ul">' +
      '      </ul>' +
      '      <div class="w3-container w3-padding">' +
      '        <div class="w3-row-padding">'+
      '          <div class="w3-full" id="ElementDescription">'+
      '          </div>' +
      '        </div>' +
      '      </div>' +
      '    </div>'+
      '  </div>'+
      '</div>' +
      '';
    this.Content.innerHTML = sContent;
    //
    let spanIndivDataEdit=document.getElementById("IndivDataEdit");
    this.IndivDataEdit.DomElements.forEach((ce)=>{
      spanIndivDataEdit.appendChild(ce);
    });
    this.LabelHeadBodyLength=document.createElement('label');
    spanIndivDataEdit.appendChild(this.LabelHeadBodyLength);
    //
    let divNav=document.getElementById("Identify_Traits_Nav");
    let buttonNext=document.createElement('button');
    buttonNext.className="w3-btn w3-round w3-block w3-green w3-padding-small w3-margin-bottom";
    divNav.appendChild(buttonNext);
    buttonNext.innerHTML="Nächstes Passbild";
    buttonNext.onclick=(ev)=>{
      let sCurrentElementName=this.Element.ElementName;
      let aElements=PicMapElementMgr.getFilteredElements(
        (el)=>{return (el.getCategoryNum()==350);},
        PicMapElement.compareByIId);
      if (aElements.length>0) {
        let idx=0;
        let nFindCnt=0;
        let nLoopCnt=0;
        while (true) {
          if (aElements[idx].ElementName==sCurrentElementName) {
            nFindCnt++;
            if (nFindCnt>2) {
              break;
            }
          }
          if ((++idx)>=aElements.length) {
            idx=0;
            nLoopCnt++;
            if (nLoopCnt>3) {
              break;
            }
          }
          if (nFindCnt>0 && (aElements[idx].getOpenTraitsCount()>0 || !aElements[idx].getIId() || nFindCnt>1)) {
            this.setElement(aElements[idx]);
            break;
          }
        }
      }
    }
    let buttonNewIId=document.createElement('button');
    buttonNewIId.className="w3-btn w3-round w3-block w3-green w3-padding-small w3-margin-bottom";
    divNav.appendChild(buttonNewIId);
    buttonNewIId.innerHTML="Neuer IId";
    buttonNewIId.onclick=(ev)=>{
      let nCurrentIId=parseInt(this.Element.getIId());
      if (isNaN(nCurrentIId)) {
        let aIndis=PicMapElementMgr.getIndiviuals();
        let nNewIId=1;
        while (nNewIId<aIndis.length && aIndis[nNewIId] && aIndis[nNewIId].els.length>=1) {
          nNewIId++;
        }
        this.IndivDataEdit.setIId(nNewIId);
        this.refreshPicsToCompare();
      } else {
        alert("IId ist bereits gesetzt.");
      }
    }
  }
  refreshSample() {
    this.IndivDataEdit.setElement(this.Element);
    // Zu vergleichendes Bild anzeigen.
    let imgURL = this.Element.getFileUrl();
    let divSample=document.getElementById('Identify_Sample');
    divSample.innerHTML=
      '<a href="' + imgURL + '" target="_blank"><img src="' + imgURL + '" style="align:center;max-width:100%;"></a>' +
      '';
    this.LabelHeadBodyLength.innerHTML="Länge: "+this.Element.getHeadBodyLengthNice();
    let divElementDescription=document.getElementById('ElementDescription');
    divElementDescription.innerHTML=this.Element.getDescriptiveHtml();
      '<a href="' + imgURL + '" target="_blank"><img src="' + imgURL + '" style="align:center;max-width:100%;"></a>' +
      '';
    // Merkmale auflisten.
    let ulTraits=document.getElementById("Identify_Traits_ul");
    ulTraits.innerHTML='';
    Trait.GetAllTraits().forEach((trait)=>{
      let li=document.createElement('li');
      li.class="w3-bar";
      ulTraits.appendChild(li);
      let divTrait=document.createElement('div');
      li.appendChild(divTrait);
      let button=document.createElement('button');
      button.className="w3-btn w3-block w3-amber w3-left-align";
      divTrait.appendChild(button);
      let traitValue=this.Element.getTraitValue(trait.Id);
      let sButtonText=trait.Label+(traitValue?(": <b>"+trait.GetValueNice(traitValue)+"</b>"):"");
      button.innerHTML=sButtonText;
      let traitEdit=new TraitEdit(trait.Id,traitValue);
      traitEdit.OnValueChange=(te)=>{
        this.Element.setTraitValue(te.Id,te.Value);
        uploadElement(this.Element);
        let traitValue=this.Element.getTraitValue(trait.Id);
        let sButtonText=trait.Label+(traitValue?(": <b>"+trait.GetValueNice(traitValue)+"</b>"):"");
        button.innerHTML=sButtonText;
        this.refreshPicsToCompare();
      }
      let divTraitEdit=document.createElement('div');
      divTraitEdit.className="w3-container w3-hide";
      divTrait.appendChild(divTraitEdit);
      button.Content=divTraitEdit;
      traitEdit.DomElements.forEach((ce)=>{
        divTraitEdit.appendChild(ce);
      });
      button.onclick=(ev)=>{
        let btn=ev.currentTarget;
        let content=btn.Content;
        fhutils.toggleClassElement(content,"w3-show");
      }
    });
  }
  refreshPicsToCompare() {
    var els = PicMapElementMgr.getIndiviualElements(PicMapElement.compareByIId);
    PicMapElementMgr.SortBySimilarity(els,this.Element);
    let sCards = '';
    // TODO: Besser implementieren!
    let divSelection=document.getElementById('Identify_Selection');
    let w=Math.max(200,divSelection.getBoundingClientRect().width/3.25);
    let sWidth = w+'px';
    let sCardWidth = w+'px';
    let sHeight= w+'px';
    let sCardHeight= (w+50)+'px';
    els.forEach((el, index, array) => {
      let imgURL=el.getFileUrl();
      let sTraits="";
      Trait.GetAllTraits().forEach((trait)=>{
        if (sTraits.length>0) {
          sTraits+=",";
        }
        sTraits+=trait.GetValueShort(el.getTraitValue(trait.Id));
      });
      let sSimilarity='';
      sSimilarity=': '+PicMapElement.calcSimilarity(el,this.Element).toFixed(2);
      sCards +=
        '        <div style="text-align:center;width:'+sCardWidth+';height:'+sCardHeight+';">' +
        '          <img src="' + imgURL + '" style="align:center;height:'+sHeight+';max-width:'+sWidth+';max-height:'+sHeight+';" onClick="Identify.setElement(PicMapElementMgr.getElementByName(\''+el.ElementName+'\'))">' +
        '          <p style="text-align:center;font-size: 80%;padding-bottom: 4px;"><b>' + el.getIId() + '</b> (' + el.getGender() + '/' + sTraits + ')'+sSimilarity+'</p>' +
        '        </div>' +
        '';
    });
    let selectionHeight=document.documentElement.clientHeight*0.80;
    let sContent = '' +
      '<div class="flex-container" style="display:flex;flex-wrap:wrap;max-height:'+selectionHeight+'px;overflow:auto;">' +
      sCards +
      '</div>' +
      '';
      divSelection.innerHTML = sContent;
  }
  setElement(element) {
    this.Element=element;
    if (this.Element && this.Element.isIndividuum()) {
      this.Element.InitIndivData();
      // Zu vergleichendes Bild anzeigen.
      this.refreshSample();
      // Vergleichsbilder anzeigen.
      this.refreshPicsToCompare();
    } else {
      // throw "Es ist kein Passbild ausgewählt.";
    }
  }
  BeforeOpen() {
    let element=PicMapElementMgr.PicMapElement_Displayed;
    if (!element) {
      element=PicMapElementMgr.getIndiviualElements(PicMapElement.compareByIId)[0];
    }
    this.setElement(element);
  }
  BeforeClose() {
    PicMapElementMgr.refreshElementMarkers();
  }
}
const Identify=new Identify_t();