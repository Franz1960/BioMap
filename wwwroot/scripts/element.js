
class PicMapElementMgr_t {
  constructor() {
    this.Map=null;
    this.PicMapElement_openInfoWindow = null;
    this.PicMapElement_activeTab = null;
    this.PicMapElement_Displayed = null;
    this.AllPicMapElements = [];
    this.FilteredPicMapElements = [];
    this.ElementsByIId = new Object();
    this.ElementConnectors = [];
    this.ElementMarkers = [];
    this.AllUploaders = [];
    this.CreateElementMarkersUpDownCounter = 0;
    this.CreateElementMarkersPicCnt = 0;
    this.RadiusFactor = 3;
  }
  setMap(map) {
    this.Map=map;
    if (map) {
      var nTriggerCnt=0;
      map.addListener('zoom_changed', function () {
        nTriggerCnt++;
        setTimeout(()=>{
          nTriggerCnt--;
          if (nTriggerCnt==0) {
            let bounds=PicMapElementMgr.Map.getBounds();
            if (bounds) {
              let fDiagonal=google.maps.geometry.spherical.computeLength([bounds.getNorthEast(),bounds.getSouthWest()]);
              // let fRadiusFactor=(fDiagonal<200?0.02:fDiagonal<500?0.05:fDiagonal<1000?0.10:fDiagonal<200?0.20:fDiagonal<5000?0.50:1);
              PicMapElementMgr.RadiusFactor=(fDiagonal*0.002);
              // Marker verteilen, wenn sie in der Zoomstufe nahe zusammen sind.
              if (true) {
                let N=PicMapElementMgr.ElementMarkers.length;
                let aOffsets=[];
                for (let i=0;i<N;i++) {
                  aOffsets[i]={lat:0,lng:0};
                }
                for (let i=0;i<N-1;i++) {
                  let elm1=PicMapElementMgr.ElementMarkers[i];
                  for (let j=i+1;j<N;j++) {
                    let elm2=PicMapElementMgr.ElementMarkers[j];
                    if (elm1.origOptions && elm2.origOptions) {
                      let path=[
                        new google.maps.LatLng(elm1.origOptions.center.lat,elm1.origOptions.center.lng),
                        new google.maps.LatLng(elm2.origOptions.center.lat,elm2.origOptions.center.lng),
                      ];
                      let distance=google.maps.geometry.spherical.computeLength(path);
                      let minDistance=PicMapElementMgr.RadiusFactor*elm1.origOptions.radius+PicMapElementMgr.RadiusFactor*elm2.origOptions.radius;
                      if (distance>0 && distance<minDistance) {
                        let force=
                        0.005
                        *
                        minDistance
                        /
                        (distance)
                        ;
                        let delta={
                          lat:elm2.origOptions.center.lat-elm1.origOptions.center.lat,
                          lng:elm2.origOptions.center.lng-elm1.origOptions.center.lng,
                        };
                        aOffsets[i]={
                          lat:aOffsets[i].lat-force*delta.lat,
                          lng:aOffsets[i].lng-force*delta.lng,
                        }
                        aOffsets[j]={
                          lat:aOffsets[j].lat+force*delta.lat,
                          lng:aOffsets[j].lng+force*delta.lng,
                        }
                      }
                    }
                  }
                }
                for (let i=0;i<N;i++) {
                  let elm=PicMapElementMgr.ElementMarkers[i];
                  elm.setCenter(
                    {
                      lat: elm.origOptions.center.lat+aOffsets[i].lat,
                      lng: elm.origOptions.center.lng+aOffsets[i].lng,
                    }
                  );
                }
              }
              PicMapElementMgr.ElementMarkers.forEach((elm)=>{
                if (elm.origOptions) {
                  elm.setRadius(PicMapElementMgr.RadiusFactor*elm.origOptions.radius);
                }
              });
            }
          }
        },800);
      });
    }
  }
  getAllElements(compare) {
    var els = [];
    this.AllPicMapElements.forEach((el, index, array) => {
      els.push(el);
    });
    if (compare) {
      els.sort(compare);
    } else {
      // Sort by date.
      els.sort(PicMapElement.compareByDateTime);
    }
    return els;
  }
  getFilteredElements(filter,compare) {
    var els = [];
    this.AllPicMapElements.forEach((el, index, array) => {
      if (filter(el)) {
        els.push(el);
      }
    });
    if (compare) {
      els.sort(compare);
    }
    return els;
  }
  getIndiviualElements(compare) {
    var els = [];
    this.AllPicMapElements.forEach((el, index, array) => {
      if (el.isIndividuum()) {
        els.push(el);
      }
    });
    if (compare) {
      els.sort(compare);
    }
    return els;
  }
  getIndiviuals(compare) {
    var aaIndisByIId = [];
    var aIndis = this.getIndiviualElements(compare);
    aIndis.forEach((el, index, array) => {
      if (el.getCategoryNum()==350) {
        let idx = el.getIId();
        if (!aaIndisByIId[idx]) {
          aaIndisByIId[idx] = { IId: idx, els: [] };
        }
        aaIndisByIId[idx].els.push(el);
      }
    });
    aaIndisByIId.forEach((indi)=>{
      if (indi && indi.els.length>=1) {
        indi.els.sort(PicMapElement.compareByDateTime);
        let el=indi.els[0];
        let yob=fhutils.getWeekNumber(el.getDate())[0];
        let g=el.getGender();
        yob-=(g=="j1"?1:g=="j2"?2:g=="j3"?3:g=="ma"?3:g=="fa"?3:0);
        indi.yob=yob;
      }
    });
    return aaIndisByIId;
  }
  getNumberOfIndiviuals() {
    let nCount=0;
    let indis = this.getIndiviuals();
    indis.forEach((indi)=>{
      if (indi && indi.els.length>=1) {
        nCount++;
      }
    });
    return nCount;
  }
  getElementByName(sElementName) {
    var elResult;
    this.AllPicMapElements.forEach((el, index, array) => {
      if (el.ElementName==sElementName) {
        elResult=el;
      }
    });
    return elResult;
  }
  hideInfoWindow() {
    let bResult=false;
    if (this.PicMapElement_openInfoWindow) {
      try {
        $("#Category").selectmenu("close");
      } catch (ex) {}
      this.PicMapElement_openInfoWindow.close();
      bResult=true;
    }
    this.PicMapElement_openInfoWindow = false;
    return bResult;
  }
  displayInfoWindow(infoWindow,imgMarker) {
    this.hideInfoWindow();
    this.PicMapElement_openInfoWindow = infoWindow;
    infoWindow.open(this.Map, imgMarker);
  }
  getAllElementsLatLngBounds() {
    let latMin, latMax;
    let lngMin, lngMax;
    if (this.ElementMarkers.length<2) {
      this.AllPicMapElements.forEach((el, index, array) => {
        let lat = el.ElementProp.MarkerInfo.position.lat;
        let lng = el.ElementProp.MarkerInfo.position.lng;
        if (!latMin) {
          latMin = latMax = lat;
          lngMin = lngMax = lng;
        } else {
          latMin = Math.min(latMin, lat);
          latMax = Math.max(latMax, lat);
          lngMin = Math.min(lngMin, lng);
          lngMax = Math.max(lngMax, lng);
        }
      });
    } else {
      this.ElementMarkers.forEach((elMarker, index, array) => {
        let lat = isNaN(elMarker.position.lat)?elMarker.position.lat():elMarker.position.lat;
        let lng = isNaN(elMarker.position.lng)?elMarker.position.lng():elMarker.position.lng;
        if (!latMin) {
          latMin = latMax = lat;
          lngMin = lngMax = lng;
        } else {
          latMin = Math.min(latMin, lat);
          latMax = Math.max(latMax, lat);
          lngMin = Math.min(lngMin, lng);
          lngMax = Math.max(lngMax, lng);
        }
      });
    }
    return new google.maps.LatLngBounds(new google.maps.LatLng(latMin, lngMin), new google.maps.LatLng(latMax, lngMax));
  }
  refreshElementMarkers() {
    // Delete old markers.
    this.ElementMarkers.forEach((elm, index, array) => {
      elm.setMap(null);
    });
    this.ElementConnectors.forEach((elc, index, array) => {
      elc.setMap(null);
    });
    for (let iid in this.ElementsByIId) {
      delete this.ElementsByIId[iid];
    }
    this.ElementMarkers.length = 0;
    this.ElementConnectors.length = 0;
    this.AllPicMapElements.length = 0;
    this.FilteredPicMapElements.length = 0;
    this.AllUploaders.length = 0;
    // Create markers from server files.
    CurrentUser.ApiCall('https://unken.itools.de/api/getelements.php',{
        method: 'GET',
        cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
        credentials: 'same-origin', // include, *same-origin, omit
        headers: {
          'Content-Type': 'application/json',
        },
      },[
      ])
      .then((response) => {
        return response.json();
      })
      .then((json)=>{
        let aElements = json;
        let aFilteredIIds=[];
        aElements.forEach((elementData) => {
          let element = new PicMapElement(elementData.ElementName, elementData.ElementProp);
          this.AllPicMapElements.push(element);
          if (element.ElementProp.UploadInfo && !this.AllUploaders.includes(element.ElementProp.UploadInfo.UserId)) {
            this.AllUploaders.push(element.ElementProp.UploadInfo.UserId);
          }
          if (PicMapElementFilterPage.Filter.accept(element)) {
            this.FilteredPicMapElements.push(element);
            this.CreateMapMarker(element);
            let iid=element.ElementProp.IndivData?element.ElementProp.IndivData.IId:null;
            if (iid) {
              let els;
              if (!(iid in this.ElementsByIId)) {
                this.ElementsByIId[iid]=[];
              }
              els=this.ElementsByIId[iid];
              els.push(element);
              aFilteredIIds.push(iid);
              els.sort(PicMapElement.compareByDateTime);
            }
          }
        });
        if (PicMapElementFilterPage.Filter.IncludeIndividualHistory) {
          aElements.forEach((elementData,index,array) => {
            let iid=elementData.ElementProp.IndivData?elementData.ElementProp.IndivData.IId:null;
            if (iid) {
              let bIsFilteredIId=false;
              aFilteredIIds.forEach((filteredIId) => {
                if (filteredIId==iid) {
                  bIsFilteredIId=true;
                }
              });
              if (bIsFilteredIId) {
                let bFound=false;
                this.FilteredPicMapElements.forEach((el) => {
                  if (el.ElementName==elementData.ElementName) {
                    bFound=true;
                  }
                });
                if (!bFound) {
                  let element = new PicMapElement(elementData.ElementName, elementData.ElementProp);
                  {
                    this.FilteredPicMapElements.push(element);
                    this.CreateMapMarker(element);
                  }
                  {
                    let els;
                    if (!(iid in this.ElementsByIId)) {
                      this.ElementsByIId[iid]=[];
                    }
                    els=this.ElementsByIId[iid];
                    els.push(element);
                    els.sort(PicMapElement.compareByDateTime);
                  }
                }
              }
            }
          });
        }
        for (let iid in this.ElementsByIId) {
          let els=this.ElementsByIId[iid];
          if (els.length>=1) {
            if (els[0].ElementProp.Computed && els[0].ElementProp.Computed.distance) {
              delete els[0].ElementProp.Computed.distance;
            }
            for (let i=1;i<els.length;i++) {
              this.ElementConnectors.push(this.CreateMapConnector(els[i-1],els[i]));
            }
          }
        }
        this.AllUploaders.sort();
      });
  }
  UpdateElementMarker(element) {
    this.ElementMarkers.forEach((elm, index, array) => {
      if (elm.ElementName == element.ElementName) {
        let symbolProps=element.getSymbolProperties();
        elm.origOptions.radius=symbolProps.radius;
        elm.origOptions.strokeColor=symbolProps.fgColor;
        elm.origOptions.fillColor=symbolProps.bgColor;
        elm.setOptions({
          radius: symbolProps.radius,
          strokeColor: symbolProps.fgColor,
          fillColor: symbolProps.bgColor,
        });
        elm.setRadius(PicMapElementMgr.RadiusFactor*elm.origOptions.radius);
      }
    });
  }
  CreateMapMarker(element) {
    var elementMarker;
    this.CreateElementMarkersUpDownCounter++;
    try {
      let lat = element.ElementProp.MarkerInfo.position.lat;
      let lng = element.ElementProp.MarkerInfo.position.lng;
      if (this.CreateElementMarkersPicCnt == 0) {
        latMin = latMax = lat;
        lngMin = lngMax = lng;
      } else {
        latMin = Math.min(latMin, lat);
        latMax = Math.max(latMax, lat);
        lngMin = Math.min(lngMin, lng);
        lngMax = Math.max(lngMax, lng);
      }
      this.CreateElementMarkersPicCnt++;
      // Render thumbnail on map.
      let sIId = element.getIId();
      let symbolProps=element.getSymbolProperties();
      let options={
        map: this.Map,
        center: { lat: lat, lng: lng },
        position: { lat: lat, lng: lng },
        radius: symbolProps.radius,
        strokeColor: symbolProps.fgColor,
        strokeOpacity: 0.60,
        strokeWeight: 2,
        fillColor: symbolProps.bgColor,
        fillOpacity: 0.35,
        zIndex: 1000000,
      };
      elementMarker = new google.maps.Circle(options);
      elementMarker.origOptions=options;
      elementMarker.ElementName = element.ElementName;
      elementMarker.addListener('click', function () {
        // InfoWindow for picture.
        element.DisplayElementWnd(this.Map, elementMarker);
      });
    } catch (ex) {
      console.log("Exception in CreateMapMarker(): " + ex);
    } finally {
      this.CreateElementMarkersUpDownCounter--;
    }
    //console.log("Image '" + imgName + "' placed on (" + lat + "," + lng + ").");
    if (this.CreateElementMarkersUpDownCounter < 1 && this.CreateElementMarkersPicCnt >= 1) {
      this.Map.fitBounds(new google.maps.LatLngBounds(new google.maps.LatLng(latMin, lngMin), new google.maps.LatLng(latMax, lngMax)));
    }
    if (elementMarker) {
      this.ElementMarkers.push(elementMarker);
    }
    return elementMarker;
  }
  CreateMapConnector(el1,el2) {
    let mapConnector = new google.maps.Polyline({
      map: this.Map,
      geodesic: true,
      strokeColor: '#50D020',
      strokeOpacity: 0.7,
      strokeWeight: 2,
      icons: [
        {
          icon: {
            path: google.maps.SymbolPath.FORWARD_CLOSED_ARROW,
          },
          offset: '100%',
        },
        {
          icon: {
            path: google.maps.SymbolPath.FORWARD_OPEN_ARROW,
          },
          offset: '66%',
        },
        {
          icon: {
            path: google.maps.SymbolPath.FORWARD_OPEN_ARROW,
          },
          offset: '33%',
        },
      ],
      path: [
        { lat: el1.ElementProp.MarkerInfo.position.lat, lng: el1.ElementProp.MarkerInfo.position.lng },
        { lat: el2.ElementProp.MarkerInfo.position.lat, lng: el2.ElementProp.MarkerInfo.position.lng },
      ],
    });
    let distance=google.maps.geometry.spherical.computeLength(mapConnector.getPath());
    el2.ElementProp.Computed={
      "distance": distance,
    }
    //this.ElementConnectors.push(mapConnector);
    return mapConnector;
  }
  SortBySimilarity(els,sample) {
    els.sort((a, b)=>{
      let simA=PicMapElement.calcSimilarity(a,sample);
      let simB=PicMapElement.calcSimilarity(b,sample);
      if (simA>simB) {
        return -1;
      } else if (simA<simB) {
        return 1;
      } else {
        return PicMapElement.compareByIId(a,b);
      }
    });
  }
}
const PicMapElementMgr=new PicMapElementMgr_t(); 

class Trait {
  constructor(sId,sLabel,sDescription,valueType="YNU") {
    this.Id=sId;
    this.Label=sLabel;
    this.Description=sDescription;
    this.ValueType=valueType;
  }
  static GetAllTraits() {
    return [
      new Trait("YellowDominance","Gelb überwiegt","Die gelben Anteile überwiegen deutlich"),
      new Trait("BlackDominance","Schwarz überwiegt","Die schwarzen Anteile überwiegen deutlich"),
      new Trait("VertBlackBreastCenterStrip","Vert. Steg in Brustmitte","Durchgehender senkrechter schwarzer Steg in der Brustmitte"),
      new Trait("HorizBlackBreastBellyStrip","Horiz. Steg zwischen Brust und Bauch","Durchgehender waagrechter schwarzer Steg zwischen Brust und Bauch"),
      //new Trait("YellowBreastArmConnection","Gelbe Brust-Arm-Verbindung","Gelbe Verbindung zwischen Brust und Oberarmen"),
      new Trait("ManyIsolatedBlackBellyDots","Viele schwarze Punkte","Bauchmuster wird überwiegend von isolierten schwarzen, annähernd kreisförmigen Flecken gebildet"),
    ];
  }
  static GetTrait(sId) {
    let trait=null;
    this.GetAllTraits().forEach((t)=>{
      if (t.Id==sId) {
        trait=t;
      }
    });
    return trait;
  }
  GetValueNice(oValue) {
    if (this.ValueType=="YNU") {
      return (oValue==0)?"offen":(oValue==1)?"Ja":(oValue==2)?"Nein":(oValue==3)?"unklar":oValue.toString();
    }
    return oValue.toString();
  }
  GetValueShort(oValue) {
    if (this.ValueType=="YNU") {
      return (!oValue || oValue==0)?"o":(oValue==1)?"J":(oValue==2)?"N":(oValue==3)?"u":oValue.toString();
    }
    return oValue.toString();
  }
  GetValueSimilarity(v1,v2) {
    if (this.ValueType=="YNU") {
      if (v1 && v2) {
        if (v1==v2) {
          if (v1==3) {
            return 0.5;
          } else {
            return 1;
          }
        } else if (v1==3 || v2==3) {
          return 0.1;
        } else {
          return -1;
        }
      }
    }
    return 0;
  }
}
class TraitEdit {
  constructor(sId,oValue,onValueChange) {
    this.Id=sId;
    this.Value=oValue;
    this.OnValueChange=onValueChange;
    this.DomElements=[];
    this.ChoiceButtons=[];
    let trait=Trait.GetTrait(sId);
    let divHeader=document.createElement('div');
    this.Description_Label=document.createElement('label');
    this.Description_Label=document.createElement('label');
    this.Description_Label.innerHTML=trait.Description;
    divHeader.appendChild(this.Description_Label);
    this.DomElements.push(divHeader);
    let divSelect=document.createElement('div');
    divSelect.className="w3-row-padding";
    this.DomElements.push(divSelect);
    [2,3,1].forEach((nValue)=>{
      let divChoice=document.createElement('div');
      divChoice.className="w3-third";
      divSelect.appendChild(divChoice);
      let buttonChoice=document.createElement('button');
      divChoice.appendChild(buttonChoice);
      buttonChoice.className="w3-btn w3-round w3-block";
      buttonChoice.innerHTML=trait.GetValueNice(nValue);
      buttonChoice.TraitValue=nValue;
      buttonChoice.onclick=(ev)=>{
        let btn=ev.currentTarget;
        this.Value=btn.TraitValue;
        this.refresh();
        if (this.OnValueChange) {
          this.OnValueChange(this);
        }
      }
      this.ChoiceButtons.push(buttonChoice);
      let imgChoice=document.createElement('img');
      divChoice.appendChild(imgChoice);
      let sImgFileName="TRAIT_"+sId+"_"+nValue+".jpg";
      imgChoice.alt=sImgFileName;
      imgChoice.src="data/conf/traits/"+sImgFileName;
      imgChoice.style.maxWidth="100%";
      imgChoice.onclick=(ev)=>{
        buttonChoice.click();
      }
      this.refresh();
    });
  }
  refresh() {
    this.ChoiceButtons.forEach((btn)=>{
      fhutils.setClassElement(btn,"w3-green",this.Value==btn.TraitValue);
    });
  }
}

class IndivDataEdit {
  constructor(element,disabled=false) {
    this.Element=element;
    this.DomElements=[];
    this.IId_Label=document.createElement('label');
    this.IId_Input=document.createElement('input');
    this.IId_Label.innerHTML="IId:";
    this.Gender_Select=document.createElement('select');
    this.IId_Input.type="number";
    this.IId_Input.min="1";
    this.Gender_Select.innerHTML=
      '<option value=""></option>'+
      '<option value="fa">fa</option>'+
      '<option value="ma">ma</option>'+
      '<option value="j0">j0</option>'+
      '<option value="j1">j1</option>'+
      '<option value="j2">j2</option>'+
      '';
    //
    this.DomElements.push(this.IId_Label);
    this.DomElements.push(this.IId_Input);
    this.DomElements.push(this.Gender_Select);
    //
    this.setElement(element);
    this.setDisabled(disabled);
  }
  setElement(element) {
    this.Element=element;
    if (this.Element) {
      this.Gender_Select.value=this.Element.getGender();
      this.Gender_Select.addEventListener('change', (event, ui) => {
        this.Element.ElementProp.IndivData.Gender=this.Gender_Select.value;
        uploadElement(this.Element);
        PicMapElementMgr.UpdateElementMarker(this.Element);
      }, false);
      //
      this.IId_Input.value = this.Element.getIId();
      this.IId_Input.addEventListener('change', (event, ui) => {
        this.refreshIId();
      }, false);
    } else {
      this.Gender_Select.value="";
      this.IId_Input.value="";
    }
  }
  refreshIId() {
    this.Element.ElementProp.IndivData.IId=this.IId_Input.value;
    uploadElement(this.Element);
    PicMapElementMgr.UpdateElementMarker(this.Element);
  }
  setIId(nNewIId) {
    this.IId_Input.value=nNewIId;
    this.refreshIId();
  }
  setDisabled(bDisabled) {
    this.IId_Input.disabled=bDisabled;
    this.Gender_Select.disabled=bDisabled;
  }
  setVisible(bVisible) {
    this.IId_Input.style.display=bVisible?'inline':'none';
    this.Gender_Select.style.display=bVisible?'inline':'none';
  }
}

class PicMapElement {
    // ElementWnd.
  constructor(elementName,elementProp) {
    this.ElementName=elementName;
    this.ElementProp=elementProp;
    if (CurrentUser.Level<400 && this.ElementProp.MarkerInfo && (!this.ElementProp.UploadInfo || this.ElementProp.UploadInfo.UserId!=CurrentUser.UserId)) {
      this.ElementProp.MarkerInfo.position=PicMapElement.getAlienatedPosition(this.ElementProp.MarkerInfo.position);
    }
  }
  static getAlienatedPosition(position) {
    let lat0=Math.round(position.lat*113)/113;
    let lng0=Math.round(position.lng*131)/131;
    return {
      lat: position.lat+(position.lat-lat0),
      lng: position.lng+(position.lng-lng0),
    };
  }
  getFileUrl(bPreferOriginal=false) {
    if (bPreferOriginal) {
      if (this.ElementProp.OrigFileName) {
        return this.ElementProp.OrigFileName;
      } else if (this.ElementProp.FileName) {
        return this.ElementProp.FileName;
      }
    } else {
      if (this.ElementProp.FileName) {
        return this.ElementProp.FileName;
      } else if (this.ElementProp.OrigFileName) {
        return this.ElementProp.OrigFileName;
      }
    }
    return null;
  }
  InitIndivData() {
    let id=this.ElementProp.IndivData;
    if (!id) {
      this.ElementProp.IndivData=id={IId:"",Gender:""};
    }
    if (!id.TraitValues || Array.isArray(id.TraitValues)) {
      id.TraitValues={};
    }
    if (!id.MeasuredData) {
      id.MeasuredData={
        HeadPosition:{x:300,y:0},
        BackPosition:{x:300,y:600},
      };
    }
  }
  static getCategoryNice(sCat) {
    let elCat=ElementCategories[sCat];
    if (elCat) {
      return sCat+" - "+elCat.name;
    }
    return sCat;
  }
  getCategoryNum() {
    let nCat = parseInt(this.ElementProp.MarkerInfo.category);
    if (!isNaN(nCat)) {
      return nCat;
    }
    return 0;
  }
  canBeIndividuum() {
    let nCat = this.getCategoryNum();
    if (nCat >= 300) {
      return true;
    }
    return false;
  }
  isIndividuum() {
    let nCat = this.getCategoryNum();
    if (this.canBeIndividuum() && this.ElementProp.IndivData && this.ElementProp.IndivData.IId) {
      return true;
    } else if (nCat >= 350) {
      return true;
    }
    return false;
  }
  getMonitoringIndivCount() {
    let nCat=this.getCategoryNum();
    if (nCat==320 || nCat==322) {
      return 0;
    } else if (nCat==350) {
      return 1;
    }
    return false;
  }
  getIId() {
    if ((this.getCategoryNum()>=300 && this.getCategoryNum()<400 ) && this.ElementProp.IndivData && this.ElementProp.IndivData.IId) {
      return this.ElementProp.IndivData.IId;
    }
    return "";
  }
  setIId(nNewIId) {
    this.InitIndivData();
    this.ElementProp.IndivData.IId=nNewIId;
  }
  getSymbolFilename() {
    let sAppendix = '';
    let sGender = this.getGender();
    if (sGender && this.getCategoryNum()==350) {
      sAppendix += '_' + sGender.toLowerCase();
    }
    return "Cat" + this.ElementProp.MarkerInfo.category + sAppendix + ".png";
  }
  getSymbolProperties() {
    let sCat=this.getCategoryNum();
    let sGender=this.getGender();
    let nHBL=this.getHeadBodyLengthMm();
    let bgColor=(ElementCategories[sCat] && ElementCategories[sCat].bgColor) || "#777777";
    let fgColor=bgColor;
    let props={
      radius: (nHBL?(nHBL*0.10):3.00),
      bgColor: bgColor,
      fgColor: fgColor,
    };
    return props;
  }
  getGender() {
    if (this.ElementProp.IndivData && this.ElementProp.IndivData.Gender) {
      let sGender = this.ElementProp.IndivData.Gender.toLowerCase();
      if (sGender == 'ma' || sGender == 'fa' || sGender == 'j0' || sGender == 'j1' || sGender == 'j2') {
        return sGender;
      }
    }
    return null;
  }
  static getGenderNice(sGender) {
    let sGenderNice = '?';
    if (sGender) {
      if (sGender == 'ma') {
        sGenderNice = sGender + ' (adultes Männchen)';
      } else if (sGender == 'fa') {
        sGenderNice = sGender + ' (adultes Weibchen)';
      } else if (sGender.startsWith('j')) {
        let sYears = sGender.substring(1,2);
        sGenderNice = sGender + ' (' + sYears + '-jähriges Jungtier) ';
      }
    }
    return sGenderNice;
  }
  getGenderNice() {
    return PicMapElement.getGenderNice(this.getGender());
  }
  getHeadBodyLengthMm() {
    if (this.ElementProp.IndivData && this.ElementProp.IndivData.MeasuredData && ('HeadBodyLength' in this.ElementProp.IndivData.MeasuredData)) {
      return this.ElementProp.IndivData.MeasuredData.HeadBodyLength;
    }
    return null;
  }
  getHeadBodyLengthNice() {
    let mm=this.getHeadBodyLengthMm();
    if (mm) {
      return mm.toFixed(1)+' mm';
    }
    return '';
  }
  getDate() {
    if (this.ElementProp.ExifData && this.ElementProp.ExifData.DateTimeOriginal) {
      return fhutils.parseExifDateTime(this.ElementProp.ExifData.DateTimeOriginal);
    } else if (this.ElementProp.UploadInfo) {
      return new Date(Date.parse(this.ElementProp.UploadInfo.Timestamp));
    }
    return null;
  }
  getIsoDateTime(what) {
    let date=this.getDate();
    if (date) {
      return fhutils.toModifiedISO(date,what);
    }
    return "";
  }
  setTraitValue(sId,oValue) {
    this.InitIndivData();
    if (sId) {
      this.ElementProp.IndivData.TraitValues[sId]=oValue;
    }
  }
  getTraitValue(sId) {
    if (this.ElementProp.IndivData.TraitValues) {
      return this.ElementProp.IndivData.TraitValues[sId];
    }
    return null;
  }
  getOpenTraitsCount() {
    let nCount=0;
    Trait.GetAllTraits().forEach((trait)=>{
      if (!this.getTraitValue(trait.Id)) {
        nCount++;
      }
    });
    return nCount;
  }
  getPlace() {
    let elPlace=null;
    var el=this;
    AllPlaces.forEach((place, index, array) => {
      let path=[
        new google.maps.LatLng(place.LatLng.lat,place.LatLng.lng),
        new google.maps.LatLng(el.ElementProp.MarkerInfo.position.lat,el.ElementProp.MarkerInfo.position.lng),
      ];
      let distance=google.maps.geometry.spherical.computeLength(path);
      if (distance<=place.Radius) {
        elPlace=place;
      }
    });
    return elPlace;
  }
  getDetails() {
    let place=this.getPlace();
    let sText=place?(place.Name):"";
    if (this.isIndividuum()) {
      sText+=", #"+this.getIId()+", "+this.getGender()+", "+this.getHeadBodyLengthNice();
    }
    return sText;
  }
  getDescriptiveHtml() {
    let sText=this.ElementName;
    sText+="<br/>"+PicMapElement.getCategoryNice(this.ElementProp.MarkerInfo.category);
    sText+="<br/>"+this.getIsoDateTime("d")+" / "+this.ElementProp.UploadInfo.UserId;
    if (this.ElementProp.UploadInfo && this.ElementProp.UploadInfo.Comment && this.ElementProp.UploadInfo.Comment.length>=1) {
      sText+="<br/>"+this.ElementProp.UploadInfo.Comment;
    }
    if (this.isIndividuum()) {
      sText+="<br/>IId: "+this.getIId();
      sText+=": "+this.getGenderNice();
    }
    return sText;
  }
  static compareByDateTime(a, b) {
    let ad = (a) ? a.getIsoDateTime() : "";
    let bd = (b) ? b.getIsoDateTime() : "";
    return (ad > bd) ? 1 :(ad < bd) ? -1 : 0;
  }
  static compareByIId(a, b) {
    let ai=parseInt((a && a.ElementProp && a.ElementProp.IndivData && a.ElementProp.IndivData.IId) ? a.ElementProp.IndivData.IId : null);
    let bi=parseInt((b && b.ElementProp && b.ElementProp.IndivData && b.ElementProp.IndivData.IId) ? b.ElementProp.IndivData.IId : null);
    if (Number.isInteger(ai)) {
      if (Number.isInteger(bi)) {
        if (ai==bi) {
          return PicMapElement.compareByDateTime(a, b);
        } else {
          return ai-bi;
        }
      } else {
        return 1;
      }
    } else {
      if (Number.isInteger(bi)) {
        return -1;
      } else {
        return 0;
      }
    }
  }
  static calcSimilarity(el1,el2) {
    let fSimilarity=0;
    // Zeitlich sortieren (y(oung) / o(ld)).
    let date1=el1.getDate();
    let date2=el2.getDate();
    let nAgeDiffDays=0;
    let ely=el1;
    let elo=el2;
    if (date1 && date2) {
      nAgeDiffDays=(date2.getTime()-date1.getTime())/(24*3600000);
      if (nAgeDiffDays<0) {
        ely=el2;
        elo=el1;
        nAgeDiffDays=-nAgeDiffDays;
      }
    }
    // Örtlicher Abstand.
    let distance=200;
    try {
      let mapConnector=new google.maps.Polyline({
        map: null,
        geodesic: true,
        path: [
          { lat: el1.ElementProp.MarkerInfo.position.lat, lng: el1.ElementProp.MarkerInfo.position.lng },
          { lat: el2.ElementProp.MarkerInfo.position.lat, lng: el2.ElementProp.MarkerInfo.position.lng },
        ],
      });
      distance=google.maps.geometry.spherical.computeLength(mapConnector.getPath());
    } catch(ex) {}
    if (distance<50 && el1.getIsoDateTime("d")==el2.getIsoDateTime("d")) {
      // Am selben Tag am selben Ort -> müssen unterschiedlich sein.
      fSimilarity-=3;
    } else {
      fSimilarity+=Math.min(3,50/(distance+10));
    }
    // Geschlecht.
    let g1=el1.getGender();
    let g2=el2.getGender();
    if (g1=="ma" && g2=="ma") {
      fSimilarity+=1;
    } else if ((g1=="ma" && g2=="fa") || (g1=="fa" && g2=="ma")) {
      fSimilarity+=-0.5;
    }
    // Kopf-Rumpf-Länge.
    {
      let ly=ely.getHeadBodyLengthMm();
      let lo=elo.getHeadBodyLengthMm();
      if (ly && lo) {
        let expectedGrowthMm=Math.min(30,(4*nAgeDiffDays/30)*(20/ly));
        let relGrowth=(lo-ly)/Math.min(ly,lo);
        if (relGrowth<-0.20) {
          fSimilarity-=Math.min(3,-relGrowth*3);
        }
        fSimilarity+=Math.min(3,4/((Math.abs(lo-ly-expectedGrowthMm))+1));
      }
    }
    // Merkmalsabstand.
    Trait.GetAllTraits().forEach((trait)=>{
      let v1=el1.getTraitValue(trait.Id);
      let v2=el2.getTraitValue(trait.Id);
      fSimilarity+=trait.GetValueSimilarity(v1,v2);
    });
    return fSimilarity;
  }
  refreshAccessibility() {
    let bWithImage = (!this.ElementName.startsWith("LOC_"));
    let bCanBeIndividuum = this.canBeIndividuum();
    let spanIndivDataEdit=document.getElementById("IndivDataEdit");
    if (spanIndivDataEdit && spanIndivDataEdit.IndivDataEdit) {
      let indivDataEdit=spanIndivDataEdit.IndivDataEdit;
      indivDataEdit.setDisabled(!(CurrentUser.MayChangeCategory && bCanBeIndividuum));
      indivDataEdit.setVisible(this.canBeIndividuum());
    }
    let elementImage=document.getElementById("ElementImage");
    if (elementImage) {
      elementImage.style.display=bWithImage?'inline':'none';
    }
  }
  addInfoTableRow(table,label,text) {
    table.innerHTML+=
    '      <tr>' +
    '        <td style="text-align: right;">'+label+'</td>' +
    '        <td>'+text+'</td>' +
    '      </tr>';
  }
  DisplayElementWnd() {
    // InfoWindow for picture.
    PicMapElementMgr.PicMapElement_Displayed = this;
    if (this.ElementProp.ExifData) {
      delete this.ElementProp.ExifData.MakerNote;
      delete this.ElementProp.ExifData.undefined;
      delete this.ElementProp.ExifData.UserComment;
    }
    this.InitIndivData();
    let imgURL = this.getFileUrl();
    let imgName = this.ElementName;
    let bWithImage = (!this.ElementName.startsWith("LOC_"));
    let sCatOptions = '';
    let sDisabled = (CurrentUser.MayChangeCategory) ? '' : ' disabled';
    for (let sCat in ElementCategories) {
      let sSelected = (sCat == this.ElementProp.MarkerInfo.category) ? " selected" : "";
      sCatOptions += '<option value="' + sCat + '"' + sSelected + sDisabled + '>' + ElementCategories[sCat].name + '</option>';
    }
    let sContent =
    '<div class="w3-container" style="width:128dp;height:128dp;overflow:auto;">' +
    '  <div id="Bild" style="width:96dp;height:96dp;">' +
    '    <form>' +
    '      <label for="InputCategory">Kategorie:</label>' +
    '      <select id="InputCategory">' +
    '        <optgroup label="Kategorien">' +
    sCatOptions +
    '        </optgroup>' +
    '        <optgroup label="Aktionen">' +
    ((CurrentUser.Level < 500)?'':'          <option value="A200_PrepPic">Bild vermessen</option>') +
    '          <option value="A100_delete">löschen</option>' +
    '          <option value="A300_filter">Filtern (nur dieses Individuum)</option>' +
    '        </optgroup>' +
    '      </select>' +
    '      <span id="IndivDataEdit"></span>' +
    '      <br/>' +
    '    </form>' +
    (
      (CurrentUser.Level>=400)
      ?('        <a href="' + imgURL + '" target="_blank" id="ElementImage">' + '<img src="' + imgURL + '" style="max-width:100%;max-height:100%;margin-top:6px;">' + '</a>')
      :('        <img src="' + imgURL + '" id="ElementImage" onContextMenu="return false;" alt="' + imgName + '" style="max-width:100%;max-height:100%;margin-top:6px;"/>')
    ) +
    '  </div>' +
    '  <div id="Info" style="height:86%;">' +
    '    <table class="w3-table-all" style="width:100%" id="InfoTable">' +
    '    </table >' +
    '  </div>' +
    '  <div id="AllProperties" style="height:86%;">' +
    '    <pre><code><p id="AllPropertiesContent"/></code></pre>' +
    '  </div>' +
    '</div>' +
    '';
    let infoWindow = new google.maps.InfoWindow({
      content: sContent
    });
    infoWindow.addListener('domready', () => {
      let infoTable=document.getElementById("InfoTable");
      // Add info table rows.
      this.addInfoTableRow(infoTable,'Dateiname',this.ElementName);
      if (this.ElementProp.ExifData && this.ElementProp.ExifData.Make && this.ElementProp.ExifData.Model) {
        this.addInfoTableRow(infoTable,'Kamera',this.ElementProp.ExifData.Make + ' / ' + this.ElementProp.ExifData.Model);
      }
      if (this.ElementProp.ExifData && this.ElementProp.ExifData.DateTimeOriginal) {
        this.addInfoTableRow(infoTable,'Aufgenommen',fhutils.toModifiedISO(fhutils.parseExifDateTime(this.ElementProp.ExifData.DateTimeOriginal)));
      }
      if (this.ElementProp.UploadInfo) {
        this.addInfoTableRow(infoTable,'Hochgeladen',fhutils.toModifiedISO(new Date(Date.parse(this.ElementProp.UploadInfo.Timestamp))));
        this.addInfoTableRow(infoTable,'von',this.ElementProp.UploadInfo.UserId);
      }
      if (this.ElementProp.UploadInfo && this.ElementProp.UploadInfo.Comment && this.ElementProp.UploadInfo.Comment.length>=1) {
        this.addInfoTableRow(infoTable,'Kommentar',this.ElementProp.UploadInfo.Comment);
      }
      if (this.ElementProp.UploadInfo && this.ElementProp.UploadInfo.CoOwner && this.ElementProp.UploadInfo.CoOwner.length>=1) {
        this.addInfoTableRow(infoTable,'Entdecker',this.ElementProp.UploadInfo.CoOwner);
      }
      if (this.isIndividuum()) {
        this.addInfoTableRow(infoTable,'Individuum Nr.',this.getIId());
        this.addInfoTableRow(infoTable,'Geschlecht',this.getGenderNice()+', '+this.getHeadBodyLengthNice());
        if (this.ElementProp.Computed && this.ElementProp.Computed.distance) {
          this.addInfoTableRow(infoTable,'Letzte Wanderung',this.ElementProp.Computed.distance.toFixed(0)+" m");
        }
      }
      this.refreshAccessibility();
      // Interactive elements.
      let inputCategory = document.getElementById("InputCategory");
      inputCategory.addEventListener('change', (event) => {
        let inputCategory = document.getElementById("InputCategory");
        if (event.target==inputCategory) {
          let selectedValue = inputCategory.value;
          let bIsOwner = (PicMapElementMgr.PicMapElement_Displayed.ElementProp.UploadInfo.UserId == CurrentUser.UserId) ? true : false;
          if (parseInt(selectedValue)) {
            // Change category.
            if (CurrentUser.MayChangeCategory) {
              PicMapElementMgr.PicMapElement_Displayed.ElementProp.MarkerInfo.category = selectedValue;
              uploadElement(PicMapElementMgr.PicMapElement_Displayed);
              PicMapElementMgr.UpdateElementMarker(PicMapElementMgr.PicMapElement_Displayed);
              PicMapElementMgr.PicMapElement_Displayed.refreshAccessibility();
            } else {
              fhutils.ShowDenial();
            }
          } else if (selectedValue == "A100_delete") {
            // Delete element.
            if (bIsOwner || CurrentUser.MayChangeCategory) {
              let elm=PicMapElementMgr.ElementMarkers;
              for (let i = 0; i < elm.length; i++) {
                if (elm[i]) {
                  if (elm[i].ElementName == PicMapElementMgr.PicMapElement_Displayed.ElementName) {
                    elm[i].setMap(null);
                    delete elm[i];
                  }
                }
              }
              CurrentUser.ApiCall('/api/deleteelement.php',{
                method: 'POST',
                headers: {
                  "Content-Type": "application/json",
                },
                body: JSON.stringify(PicMapElementMgr.PicMapElement_Displayed)
              },[
              ])
              .then((response)=>{
                response.text().then(function (text) {
                  console.log(text);
                });
              });
            } else {
              fhutils.ShowDenial();
            }
          } else if (selectedValue == "A200_PrepPic") {
            // Passbild erstellen.
            if (CurrentUser.MayChangeCategory) {
              PrepPic.setElement(PicMapElementMgr.PicMapElement_Displayed,true);
              selectMainTab('PrepPic');
              PicMapElementMgr.hideInfoWindow();
            } else {
              fhutils.ShowDenial();
            }
          } else if (selectedValue == "A300_filter") {
            // Filtern (nur dieses Individuum).
            PicMapElementFilterPage.ClearAllFilters();
            PicMapElementFilterPage.Filter.IId=PicMapElementMgr.PicMapElement_Displayed.getIId();
            PicMapElementMgr.refreshElementMarkers();
            PicMapElementMgr.hideInfoWindow();
            // selectMainTab('None');
            // let latLngBounds = PicMapElementMgr.getAllElementsLatLngBounds();
            // map.fitBounds(latLngBounds);
          }
        }
      }, false);
      //
      let indivDataEdit=new IndivDataEdit(PicMapElementMgr.PicMapElement_Displayed,(CurrentUser.MayChangeCategory));
      let spanIndivDataEdit=document.getElementById("IndivDataEdit");
      spanIndivDataEdit.IndivDataEdit=indivDataEdit;
      spanIndivDataEdit.innerHTML="";
      indivDataEdit.DomElements.forEach((ce)=>{
        spanIndivDataEdit.appendChild(ce);
      });
      //
      if (CurrentUser.Level>=400) {
        document.getElementById("AllPropertiesContent").innerHTML = JSON.stringify(PicMapElementMgr.PicMapElement_Displayed.ElementProp, null, 2);
      }
      //
      this.refreshAccessibility();
    });
    let marker=null;
    PicMapElementMgr.ElementMarkers.forEach((elm)=>{
      if (elm.ElementName==this.ElementName) {
        marker=elm;
      }
    });
    PicMapElementMgr.displayInfoWindow(infoWindow,marker);
    // Set below lowest Z index.
    let minZIndex;
    PicMapElementMgr.ElementMarkers.forEach((elm) => {
      if (elm) {
        let zIndex = elm.zIndex;
        if (zIndex && (!minZIndex || zIndex<minZIndex)) {
          minZIndex = zIndex;
        }
      }
    });
    if (minZIndex) {
      marker.setOptions({zIndex: minZIndex - 1});
    }
  }
}

class PicMapElementFilter_t {
  constructor() {
    this.Category=null;
    this.UploadedBy=null;
    this.DateFrom=null;
    this.DateTo=null;
    this.IId=null;
    this.Gender=null;
    this.HeadBodyLengthFrom=null;
    this.HeadBodyLengthTo=null;
    this.IncludeIndividualHistory=null;
  }
  loadFromCookie() {
    let sData=UserMgt.getCookie('PicMapElementFilter');
    if (sData) {
      let oData=JSON.parse(sData);
      for (let key in oData) {
        if (key in this) {
          let value=oData[key];
          if (value) {
            if (key=='DateFrom' || key=='DateTo') {
              this[key]=new Date(value);
            } else {
              this[key]=value;
            }
          } else {
            this[key]=null;
          }
        }
      }
    }
  }
  saveToCookie() {
    let oData={};
    for (let key in this) {
      oData[key]=this[key];
    }
    UserMgt.setCookie('PicMapElementFilter',JSON.stringify(oData));
  }
  accept(element) {
    if (
      (!this.Category || this.Category==element.ElementProp.MarkerInfo.category)
      &&
      (!this.UploadedBy || this.UploadedBy==element.ElementProp.UploadInfo.UserId)
      &&
      (!(this.DateFrom && !isNaN(this.DateFrom)) || element.getIsoDateTime("d")>=fhutils.toModifiedISO(this.DateFrom,"d"))
      &&
      (!(this.DateTo && !isNaN(this.DateTo)) || element.getIsoDateTime("d")<=fhutils.toModifiedISO(this.DateTo,"d"))
      &&
      (!this.IId || (this.IId==element.getIId()))
      &&
      (!this.Gender || (this.Gender==element.getGender()))
      &&
      (!this.HeadBodyLengthFrom || element.getHeadBodyLengthMm()>=this.HeadBodyLengthFrom)
      &&
      (!this.HeadBodyLengthTo || element.getHeadBodyLengthMm()<=this.HeadBodyLengthTo)
    ) {
      return true;
    }
    return false;
  }
}

class PicMapElementFilterPage_t {
  constructor() {
    this.DivMain = null;
    this.Filter=new PicMapElementFilter_t();
    this.Filter.loadFromCookie();
  }
  PrepareDisplay(divMain) {
    this.DivMain = divMain;
    this.DivMain.CodeBehind = this;
    //
    this.DivMain.appendChild(this.Header = document.createElement('h1'));
    this.DivMain.appendChild(this.Content = document.createElement('div'));
    this.Header.innerText = 'Filter';
    let sContent = '' +
      '<div class="w3-container">' +
      '  <ul class="w3-ul w3-card-4" id="PicMapElementFilterPage_ul">' +
      '  </ul>' +
      '</div>' +
      '<div class="w3-container" id="PicMapElementFilterPage_bottom">' +
      '</div>' +
      '';
    this.Content.innerHTML = sContent;
    let ul=document.getElementById("PicMapElementFilterPage_ul");
    //
    {
      let li=document.createElement('li');
      li.class="w3-bar";
      ul.appendChild(li);
      li.innerHTML=''+
        '<div class="w3-row-padding">'+
        '  <div class="w3-quarter">Kategorie</div>'+
        '  <div class="w3-half">'+
        '    <select id="PicMapElementFilterPage_Category">'+
        '    </select>'+
        '  </div>'+
        '  <div class="w3-quarter"><button type="button" onclick="document.getElementById(\'PicMapElementFilterPage_Category\').value=null;">X</button></div>'+
        '</div>'+
        '';
    }
    {
      let li=document.createElement('li');
      li.class="w3-bar";
      ul.appendChild(li);
      li.innerHTML=''+
        '<div class="w3-row-padding">'+
        '  <div class="w3-quarter">Hochgeladen von</div>'+
        '  <div class="w3-half">'+
        '    <select id="PicMapElementFilterPage_UploadedBy">'+
        '    </select>'+
        '  </div>'+
        '  <div class="w3-quarter"><button type="button" onclick="document.getElementById(\'PicMapElementFilterPage_UploadedBy\').value=null;">X</button></div>'+
        '</div>'+
        '';
    }
    {
      let li=document.createElement('li');
      li.class="w3-bar";
      ul.appendChild(li);
      li.innerHTML=''+
        '<div class="w3-row-padding">'+
        '  <div class="w3-quarter">Datum</div>'+
        '  <div class="w3-quarter"><label>von </label><input type="date" id="PicMapElementFilterPage_DateFrom"/></div>'+
        '  <div class="w3-quarter"><label>bis </label><input type="date" id="PicMapElementFilterPage_DateTo"/></div>'+
        '  <div class="w3-quarter"><button type="button" onclick="document.getElementById(\'PicMapElementFilterPage_DateFrom\').value=null;document.getElementById(\'PicMapElementFilterPage_DateTo\').value=null;">X</button></div>'+
        '</div>'+
        '';
    }
    {
      let li=document.createElement('li');
      li.class="w3-bar";
      ul.appendChild(li);
      li.innerHTML=''+
        '<div class="w3-row-padding">'+
        '  <div class="w3-quarter">Indiv. ID</div>'+
        '  <div class="w3-half"><input class="w3-input w3-border" type="number" id="PicMapElementFilterPage_IId"/></div>'+
        '  <div class="w3-quarter"><button type="button" onclick="document.getElementById(\'PicMapElementFilterPage_IId\').value=null;">X</button></div>'+
        '</div>'+
        '';
    }
    {
      let li=document.createElement('li');
      li.class="w3-bar";
      ul.appendChild(li);
      li.innerHTML=''+
        '<div class="w3-row-padding">'+
        '  <div class="w3-quarter">Geschlecht</div>'+
        '  <div class="w3-half">'+
        '    <select id="PicMapElementFilterPage_Gender">'+
        '      <option value=""></option>'+
        '      <option value="fa">fa</option>'+
        '      <option value="ma">ma</option>'+
        '      <option value="j0">j0</option>'+
        '      <option value="j1">j1</option>'+
        '      <option value="j2">j2</option>'+
        '    </select>'+
        '  </div>'+
        '  <div class="w3-quarter"><button type="button" onclick="document.getElementById(\'PicMapElementFilterPage_Gender\').value=null;">X</button></div>'+
        '</div>'+
        '';
    }
    {
      let li=document.createElement('li');
      li.class="w3-bar";
      ul.appendChild(li);
      li.innerHTML=''+
        '<div class="w3-row-padding">'+
        '  <div class="w3-quarter">Kopf-Rumpf-Länge</div>'+
        '  <div class="w3-quarter"><label>von </label><input type="number" id="PicMapElementFilterPage_HeadBodyLengthFrom"/></div>'+
        '  <div class="w3-quarter"><label>bis </label><input type="number" id="PicMapElementFilterPage_HeadBodyLengthTo"/></div>'+
        '  <div class="w3-quarter"><button type="button" onclick="document.getElementById(\'PicMapElementFilterPage_HeadBodyLengthFrom\').value=null;document.getElementById(\'PicMapElementFilterPage_HeadBodyLengthTo\').value=null;">X</button></div>'+
        '</div>'+
        '';
    }
    {
      let li=document.createElement('li');
      li.class="w3-bar";
      ul.appendChild(li);
      li.innerHTML=''+
        '<div class="w3-row-padding">'+
        '  <div class="w3-quarter">Alle Passbilder eines Individuums einschließen</div>'+
        '  <div class="w3-half"><input class="w3-input w3-border" type="checkbox" id="PicMapElementFilterPage_IncludeIndividualHistory"/></div>'+
        '  <div class="w3-quarter"><button type="button" onclick="document.getElementById(\'PicMapElementFilterPage_IncludeIndividualHistory\').checked=null;">X</button></div>'+
        '</div>'+
        '';
    }
    let divBottom=document.getElementById("PicMapElementFilterPage_bottom");
    let buttonApply=document.createElement('button');
    buttonApply.className="w3-btn w3-round w3-green w3-padding w3-margin";
    divBottom.appendChild(buttonApply);
    buttonApply.innerHTML="Anwenden";
    buttonApply.onclick=(ev)=>{
      selectMainTab('Home');
    }
    let buttonReset=document.createElement('button');
    buttonReset.className="w3-btn w3-round w3-green w3-padding w3-margin";
    divBottom.appendChild(buttonReset);
    buttonReset.innerHTML="X - Alle zurücksetzen";
    buttonReset.onclick=(ev)=>{
      PicMapElementFilterPage.ClearAllFilters();
    }
  }
  ClearAllFilters() {
    document.getElementById('PicMapElementFilterPage_Category').value=null;
    document.getElementById('PicMapElementFilterPage_UploadedBy').value=null;
    document.getElementById('PicMapElementFilterPage_DateFrom').value=null;
    document.getElementById('PicMapElementFilterPage_DateTo').value=null;
    document.getElementById('PicMapElementFilterPage_IId').value=null;
    document.getElementById('PicMapElementFilterPage_Gender').value=null;
    document.getElementById('PicMapElementFilterPage_HeadBodyLengthFrom').value=null;
    document.getElementById('PicMapElementFilterPage_HeadBodyLengthTo').value=null;
  }
  BeforeOpen() {
    {
      let select=document.getElementById("PicMapElementFilterPage_Category");
      let sCurrentValue=this.Filter.Category;
      select.innerHTML='';
      for (let sCat in ElementCategories) {
        let elCat=ElementCategories[sCat];
        let option=document.createElement('option');
        option.value=sCat;
        option.innerHTML=(elCat)?(sCat+" - "+elCat.name):"";
        select.appendChild(option);
      };
      select.value=sCurrentValue;
    }
    {
      let selectUploadedBy=document.getElementById("PicMapElementFilterPage_UploadedBy");
      let sCurrentValue=this.Filter.UploadedBy;
      selectUploadedBy.innerHTML='';
      let aOptions=[""].concat(PicMapElementMgr.AllUploaders);
      aOptions.forEach((sEmailAddr, index, array) => {
        let option=document.createElement('option');
        option.value=option.innerHTML=sEmailAddr;
        selectUploadedBy.appendChild(option);
      });
      selectUploadedBy.value=sCurrentValue;
    }
    document.getElementById("PicMapElementFilterPage_DateFrom").value=fhutils.dateToInput(this.Filter.DateFrom);
    document.getElementById("PicMapElementFilterPage_DateTo").value=fhutils.dateToInput(this.Filter.DateTo);
    document.getElementById("PicMapElementFilterPage_IId").value=this.Filter.IId;
    document.getElementById("PicMapElementFilterPage_Gender").value=this.Filter.Gender;
    document.getElementById("PicMapElementFilterPage_HeadBodyLengthFrom").value=this.Filter.HeadBodyLengthFrom;
    document.getElementById("PicMapElementFilterPage_HeadBodyLengthTo").value=this.Filter.HeadBodyLengthTo;
    document.getElementById("PicMapElementFilterPage_IncludeIndividualHistory").checked=this.Filter.IncludeIndividualHistory;
  }
  BeforeClose() {
    this.Filter.Category=document.getElementById("PicMapElementFilterPage_Category").value;
    this.Filter.UploadedBy=document.getElementById("PicMapElementFilterPage_UploadedBy").value;
    this.Filter.DateFrom=new Date(document.getElementById("PicMapElementFilterPage_DateFrom").value);
    this.Filter.DateTo=new Date(document.getElementById("PicMapElementFilterPage_DateTo").value);
    this.Filter.IId=document.getElementById("PicMapElementFilterPage_IId").value;
    this.Filter.Gender=document.getElementById("PicMapElementFilterPage_Gender").value;
    this.Filter.HeadBodyLengthFrom=document.getElementById("PicMapElementFilterPage_HeadBodyLengthFrom").value;
    this.Filter.HeadBodyLengthTo=document.getElementById("PicMapElementFilterPage_HeadBodyLengthTo").value;
    this.Filter.IncludeIndividualHistory=document.getElementById("PicMapElementFilterPage_IncludeIndividualHistory").checked;
    this.Filter.saveToCookie();
    PicMapElementMgr.refreshElementMarkers();
  }
}
const PicMapElementFilterPage=new PicMapElementFilterPage_t();
