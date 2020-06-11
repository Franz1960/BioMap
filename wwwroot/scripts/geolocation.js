
// Live position recording.
class GeoLocation {

  constructor() {
    this.Map=null;
    this.Started=false;
    this.LivePosMinDist = 20; // Meter.
    this.LivePosMaxHistory = 20; // Number of positions.
    this.LivePosition=null;
    this.LivePosHistory = [];
    this.LiveMarker=null;
    this.LiveArrowIcon=null;
    this.LiveCircleIcon=null;
    this.LiveHeading=null;
    //
    this.watchId=null;
    this.AbsOrSensor=null;
  }

  Start(map) {
    this.Map=map;
    if (navigator.geolocation) {
      this.LiveCircleIcon = {
        path: google.maps.SymbolPath.CIRCLE,
        strokeColor: 'blue',
        strokeWeight: 1,
        fillColor: 'blue',
        fillOpacity: 0.3,
        scale: 15,
      };
      var traceIcon = {
        path: google.maps.SymbolPath.CIRCLE,
        strokeColor: 'blue',
        strokeWeight: 1,
        fillColor: 'blue',
        fillOpacity: 0.15,
        scale: 5,
      };
      this.LiveArrowIcon = {
        path: google.maps.SymbolPath.FORWARD_CLOSED_ARROW,
        rotation: null,
        strokeColor: 'blue',
        strokeWeight: 1,
        fillColor: 'blue',
        fillOpacity: 0.3,
        scale: 10,
      };
      this.LiveMarker = new google.maps.Marker({
        position: CurrentLocation.LivePosition,
        map: map,
        icon: CurrentLocation.LiveCircleIcon,
      });
      var aTraceMarkers=[];
      var bRejected = false;
      try {
        if (!this.AbsOrSensor) {
          this.AbsOrSensor=new AbsoluteOrientationSensor();
          this.AbsOrSensor.onreading = () => {
            let q = CurrentLocation.AbsOrSensor.quaternion;
            let heading = Math.atan2(2 * q[0] * q[1] + 2 * q[2] * q[3], 1 - 2 * q[1] * q[1] - 2 * q[2] * q[2]) * (180 / Math.PI);
            if (heading < 0) heading = 360 + heading;
            CurrentLocation.LiveHeading=360-heading;
            CurrentLocation.refreshLiveMarker();
          }
          this.AbsOrSensor.start();
        }
      } catch (ex) {
        //alert(ex);
      }
      if (this.watchId) {
        navigator.geolocation.clearWatch(this.watchId);
      }
      this.watchId=navigator.geolocation.watchPosition(position => {
        CurrentLocation.LivePosition = {
          lat: position.coords.latitude,
          lng: position.coords.longitude
        };
        // Display trace.
        if (CurrentLocation.LivePosHistory.length<1) {
          CurrentLocation.LivePosHistory[0]=CurrentLocation.LivePosition;
        } else {
          let dist=google.maps.geometry.spherical.computeDistanceBetween(new google.maps.LatLng(CurrentLocation.LivePosition),new google.maps.LatLng(CurrentLocation.LivePosHistory[0]));
          if (dist>CurrentLocation.LivePosMinDist) {
            CurrentLocation.LivePosHistory.splice(0,0,CurrentLocation.LivePosition);
            if (CurrentLocation.LivePosHistory.length>=CurrentLocation.LivePosMaxHistory) {
              CurrentLocation.LivePosHistory.length=CurrentLocation.LivePosMaxHistory-1;
            }
          }
        }
        for (let i=0;i<CurrentLocation.LivePosHistory.length;i++) {
          while (aTraceMarkers.length<=i) {
            aTraceMarkers.push(new google.maps.Marker({
              position: CurrentLocation.LivePosHistory[i],
              map: CurrentLocation.Map,
              icon: traceIcon,
            }));
          }
          aTraceMarkers[i].icon.scale=Math.max(3,12-i);
          aTraceMarkers[i].setPosition(CurrentLocation.LivePosHistory[i]);
        }
        CurrentLocation.refreshLiveMarker();
      }, position => { },
      { enableHighAccuracy: true });
    }
    this.Started=true;
  }
  refreshLiveMarker() {
    if (this.LiveHeading) {
      // Heading is given by the navigation device -> display arrow.
      // Unfortunately no browser does set heading, so this code is waiting for better browsers.
      CurrentLocation.LiveArrowIcon.rotation=this.LiveHeading;
      CurrentLocation.LiveMarker.setIcon(CurrentLocation.LiveArrowIcon);
    } else {
      // No heading is given by the navigation device -> display circle.
      CurrentLocation.LiveMarker.setIcon(CurrentLocation.LiveCircleIcon);
    }
    CurrentLocation.LiveMarker.setPosition(CurrentLocation.LivePosHistory[0]);
  }
}
var CurrentLocation = new GeoLocation();
