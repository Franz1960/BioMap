
// Ein Ort mit Unken, z.B. eine Pfützengruppe.
class Place {
  constructor(placeData) {
    this.Name=(placeData)?placeData.Name:null;
    this.LatLng=(placeData)?placeData.LatLng:null;
    this.Radius=(placeData && placeData.Radius)?placeData.Radius:150;
    if (CurrentUser.Level<400) {
      this.LatLng=PicMapElement.getAlienatedPosition(this.LatLng);
    }
  }
  static RefreshPlaces() {
    return new Promise((resolve, reject) => {
      try {
        CurrentUser.ApiCall('/api/places.php',{
          method: 'GET',
          cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
          credentials: 'same-origin', // include, *same-origin, omit
        },[
          {name:'Cmd',value:'GetAllPlaces'},
        ])
        .then((response) => {
          return response.json();
        })
        .then((json)=>{
          AllPlaces.length=0;
          json.forEach((place)=>{
            AllPlaces.push(new Place(place));
          });
          if (resolve) {
            resolve();
          }
        })
        .catch((error) => {
          if (reject) {
            reject(error);
          }
        })
        ;
      } catch (ex) {
        if (reject) {
          reject("" + ex);
        }
      }
    });
  }
  static getAllNames() {
    let aNames=[];
    AllPlaces.forEach((p)=>{
      aNames.push(p.Name);
    });
  }
  static getFromName(sName) {
    let place=null;
    AllPlaces.forEach((p)=>{
      if (p.Name==sName) {
        place=p;
      }
    });
    return place;
  }
  static GetAllMonitoringEvents() {
    return new Promise((resolve, reject) => {
      try {
        CurrentUser.ApiCall('/api/places.php',{
          method: 'GET',
          cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
          credentials: 'same-origin', // include, *same-origin, omit
        },[
          {name:'Cmd',value:'GetAllMonitoringEvents'},
        ])
        .then((response) => {
          return response.json();
        })
        .then((json)=>{
          AllMonitoringEvents.length=0;
          for (let placeName in json) {
            let monEvt=json[placeName];
            AllMonitoringEvents[placeName]={
              'kw':monEvt.kw,
              'user':monEvt.user,
              'value':monEvt.value
            };
          };
          if (resolve) {
            resolve();
          }
        })
        .catch((error) => {
          if (reject) {
            reject(error);
          }
        })
        ;
      } catch (ex) {
        if (reject) {
          reject("" + ex);
        }
      }
    });
  }
  static SetMonitoringEvent(place,kw,user,value) {
    CurrentUser.ApiCall('/api/places.php',{
      method: 'GET',
      cache: 'no-cache', // *default, no-cache, reload, force-cache, only-if-cached
      credentials: 'same-origin', // include, *same-origin, omit
    },[
      {name:'Cmd',value:'SetMonitoringEvent'},
      {name:'place',value:place},
      {name:'kw',value:kw},
      {name:'user',value:user},
      {name:'value',value:value},
    ]);
  }
}
const AllPlaces=[];
const AllMonitoringEvents=[];
