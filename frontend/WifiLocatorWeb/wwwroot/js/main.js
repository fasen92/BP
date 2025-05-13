import MapHandler from "./mapHandler.js";

// Global instance
window.mapHandler = null;

// Functions that need to be exposed to .NET

window.initializeMap = function (mapId, lat, lng, zoom) {
    window.mapHandler = new MapHandler(mapId, lat, lng, zoom);
};

window.registerSearchPointsCallback = function (dotNetObjRef) {
    if (window.mapHandler) {
        window.mapHandler.registerSearchPointsCallback(dotNetObjRef);
    }
};

window.setApiBaseUrl = function (url) {
    window.apiBaseUrl = url;
    console.log("API Base URL set to:", url);
};

window.addWifiModels = function (models) {
    if (window.mapHandler) {
        window.mapHandler.addWifiModels(models);
    }
};

window.refreshMap = function () {
    if (window.mapHandler) {
        window.mapHandler.refreshMap();
    }
};

window.filterWifiPoints = function (ssid, bssid, dateStart, dateEnd, lat1, lat2, lng1, lng2) {
    if (window.mapHandler) {
        window.mapHandler.filterWifiPoints(ssid, bssid, dateStart, dateEnd, lat1, lat2, lng1, lng2);
    }
};

window.setMapToPointByBssid = function (bssid) {
    if (window.mapHandler) {
        window.mapHandler.setMapToPointByBssid(bssid);
    }
};
