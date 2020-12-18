var map;
var AoiCenterLat, AoiCenterLng, AoiRadiusMax;
const AoiRadiusTolerance = 0.50;
var AoiBounds;
var AoiAreaHa;
var Elements;
function InitMap() {
    var mapProp = {
        center: new google.maps.LatLng(48.994249, 12.190451),
        zoom: 12,
        mapTypeControlOptions: {
            mapTypeIds: ['roadmap', 'satellite', 'terrain', 'empty']
        },
        streetViewControl: false,
    };
    map = new google.maps.Map(document.getElementById("googleMap"), mapProp);
    var emptyMapType = new google.maps.ImageMapType({
        getTileUrl: function (coord, zoom) {
            return null;
        },
        tileSize: new google.maps.Size(256, 256),
        maxZoom: 24,
        minZoom: 12,
        radius: 6300000,
        name: 'Gebiet'
    });
    map.mapTypes.set('empty', emptyMapType);
}
function InitPlaces(sPlaces) {
    let json = JSON.parse(sPlaces);
    json.forEach((place) => {
        let circle = new google.maps.Circle({
            center: place.LatLng,
            radius: place.Radius,
            strokeColor: 'ORANGE',
            strokeOpacity: 0.8,
            strokeWeight: 3,
            fillColor: 'ORANGE',
            fillOpacity: 0.02
        });
        circle.setMap(map);
        circle.addListener('click', function () {
            PicMapElementMgr.hideInfoWindow();
        });
        let marker = new google.maps.Marker({
            map: map,
            // position: {lat:place.LatLng.lat,lng:place.LatLng.lng},
            position: new google.maps.LatLng(place.LatLng.lat - 0.0000096 * place.Radius, place.LatLng.lng),
            icon: {
                path: 'M -15,10 L 15,10 z',
                strokeColor: 'DARKORANGE',
            },
        });
        marker.setLabel({ text: place.Name, fontSize: '18px', fontWeight: 'bold', color: 'DARKORANGE', });
    });
}
function InitAoiBounds(sAoiBounds) {
    // Draw bounds of area of interest.
    let json = JSON.parse(sAoiBounds);
    var aVertices = json.vertices;
    if (aVertices && aVertices.length >= 1) {
        // Calculate center and radius.
        {
            let clat = 0, clng = 0;
            aVertices.forEach((v, index, array) => {
                clat += v.lat;
                clng += v.lng;
            });
            AoiCenterLat = clat / aVertices.length;
            AoiCenterLng = clng / aVertices.length;
            let rSqMax = 0;
            aVertices.forEach((v, index, array) => {
                let rSq = (v.lat - AoiCenterLat) * (v.lat - AoiCenterLat);
                rSqMax = Math.max(rSqMax, rSq);
            });
            AoiRadiusMax = Math.sqrt(rSqMax);
        }
        // Draw onto map.
        // Polygon.
        AoiBounds = new google.maps.Polygon({
            path: aVertices,
            editable: false,
            strokeColor: '#FF0000',
            strokeOpacity: 0.8,
            strokeWeight: 3,
            fillColor: '#FF0000',
            fillOpacity: 0.02,
            zIndex: -1000000,
        });
        AoiBounds.setMap(map);
        AoiAreaHa = google.maps.geometry.spherical.computeArea(AoiBounds.getPath()) / 10000;
        AoiBounds.addListener('click', function (event) {
            PicMapElementMgr.hideInfoWindow();
        });
    }
}
function ToggleEditAoiBounds() {
    if (AoiBounds) {
        if (!AoiBounds.getEditable()) {
            AoiBounds.setEditable(true);
        } else {
            AoiBounds.setEditable(false);
            let aVerticesLatLng = AoiBounds.getPath().getArray();
            let aVertices = [];
            for (var i = 0; i < aVerticesLatLng.length; i++) {
                aVertices.push({
                    "lat": aVerticesLatLng[i].lat(),
                    "lng": aVerticesLatLng[i].lng(),
                });
            }
            let element = {
                vertices: aVertices,
            };
            return JSON.stringify(element);
        }
    }
    return null;
}
function SetElements(aElements) {
    Elements = aElements;
}
