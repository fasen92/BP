import { fetchPoints } from "./api.js";
import { fetchLocationsByBssid } from "./api.js";

const HoverCircleMarker = L.CircleMarker.extend({
    bindPopup: function (htmlContent, options) {
        L.CircleMarker.prototype.bindPopup.call(this, htmlContent, options);
        if (options && options.showOnHover) {
            this.off("click", this.openPopup, this);
            this.on("mouseover", () => this.openPopup(), this);
            this.on("mouseout", () => {
                const popupContainer = this._popup && this._popup._container;
                if (popupContainer && popupContainer.matches(":hover")) {
                    popupContainer.addEventListener("mouseleave", function onPopupLeave() {
                        this.closePopup();
                        popupContainer.removeEventListener("mouseleave", onPopupLeave);
                    }.bind(this));
                } else {
                    this.closePopup();
                }
            }, this);
        }
        return this;
    }
});

export default class MapHandler {
    constructor(mapId, latitude, longitude, zoom) {
        // Initialize map
        this.map = L.map(mapId, {
            zoomAnimation: true,
            zoomSnap: 0,
            zoomDelta: 0.3,
            preferCanvas: true,
            minZoom: 8,
        }).setView([latitude, longitude], zoom);

        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: "Map data © OpenStreetMap contributors",
            maxZoom: 21,
            maxNativeZoom: 19
        }).addTo(this.map);

       
        this.visiblePoints = new Map(); // key = bssid, value marker
        this.spatialIndex = new RBush(); 
        this.markerGroup = L.layerGroup().addTo(this.map);
        this.heatLayer = null;

        this.locationGroup = L.layerGroup().addTo(this.map);
        this.focusedPoint = null;
        this.currentRangeCircle = null;
        this.filters = {
            set: false, // filter flag
            ssid: null,
            bssid: null,
            dateStart: null,
            dateEnd: null,
            latitude1: null,
            latitude2: null,
            longitude1: null,
            longitude2: null,
            limitedBounds: false
        };

        // set initial bounds
        const bounds = this.map.getBounds();
        this.loadedBounds = {
            latitude1: parseFloat(bounds.getSouthWest().lat.toFixed(6)),
            longitude1: parseFloat(bounds.getSouthWest().lng.toFixed(6)),
            latitude2: parseFloat(bounds.getNorthEast().lat.toFixed(6)),
            longitude2: parseFloat(bounds.getNorthEast().lng.toFixed(6))
        };
        

        // points in limited bounds should be fetched only once
        this.limitedBoundsFetched = false; 

        // callback for Blazor interop
        this.searchPointsCallback = null;

        // Debounce properties
        this.lastFetchTime = 0;
        this.fetchDelay = 300;
        this.suppressMapEvents = false;

        // map events
        this.map.on("moveend zoomend", () => {
            if (this.map.getZoom() < 8.5) {
                this.map.setZoom(8.5);
            }
            this.updateMapBoundsDisplay();

            //if (this.suppressMapEvents) return;

            this.debouncedLoadNewPoints();
        });
        this.map.on("click", () => this.clearPointFocusItems());
    }

    debouncedLoadNewPoints() {
        const now = Date.now();
        if (now - this.lastFetchTime < this.fetchDelay) {
            return;
        }
        this.lastFetchTime = now;
        this.loadNewPoints();
    }

    insertPoint(model) {
        const pointObj = {
            minX: model.approximatedLongitude,
            minY: model.approximatedLatitude,
            maxX: model.approximatedLongitude,
            maxY: model.approximatedLatitude,
            ssid: model.ssid,
            bssid: model.bssid,
            encryption: model.encryption,
            firstSeen: model.firstSeen,
            lastSeen: model.lastSeen,
            channel: model.channel,
            address: model.address,
            uncertaintyRadius: model.uncertaintyRadius
        };
        this.spatialIndex.insert(pointObj);
    }

    addWifiModels(wifiModels) {
        wifiModels.forEach(model => this.insertPoint(model));
        this.updateVisiblePoints();
        this.updateSearchPointList();
    }

    updateVisiblePoints() {
        const currentZoom = this.map.getZoom();
        const bounds = this.map.getBounds();
        const threshold = 17;
        const searchBBox = {
            minX: bounds.getWest(),
            minY: bounds.getSouth(),
            maxX: bounds.getEast(),
            maxY: bounds.getNorth()
        };

        const visibleData = this.spatialIndex.search(searchBBox);

        this.clearRendered(false);

        if (currentZoom >= threshold) {
            this.setFocusVisibility(true);
            visibleData.forEach(point => {
                const marker = new HoverCircleMarker([point.minY, point.minX], {
                    radius: 3.5,
                    color: "blue",
                    fillColor: "#00f",
                    fillOpacity: 0.5
                });
                marker.bssid = point.bssid;
                marker.bindPopup(
                    `<strong>SSID:</strong> ${point.ssid}<br>
                    <strong>BSSID:</strong> ${point.bssid}<br>
                    <strong>Latitude:</strong> ${point.minY.toFixed(6)}<br>
                    <strong>Longitude:</strong> ${point.minX.toFixed(6)}<br>
                    <strong>Encryption:</strong> ${point.encryption}<br>
                    <strong>First Seen:</strong> ${new Date(point.firstSeen).toLocaleDateString("en-GB")}<br>
                    <strong>Last Seen:</strong> ${new Date(point.lastSeen).toLocaleDateString("en-GB")}<br>
                    <strong>Channel:</strong> ${point.channel}<br>`
                    ,
                    { showOnHover: true }
                );
                marker.on("click", () => {
                    this.setFocusPoint(point.minY, point.minX);
                    this.showRangeCircle(point.minY, point.minX, point.uncertaintyRadius, "skyblue");
                    this.showLocationsForPoint(point.bssid, point.minY, point.minX);
                });
                this.markerGroup.addLayer(marker); // for rendering
                this.visiblePoints.set(point.bssid, marker); // reference to marker
            });
        } else {
            this.setFocusVisibility(false);
            this.locationGroup.clearLayers();

            // Build heatmap data.
            const heatData = visibleData.map(pt => [pt.minY, pt.minX, 1]);
            if (!this.heatLayer) {
                const heatmapConfig = {
                    radius: 18,
                    blur: 7,
                    maxOpacity: 0.5,
                    scaleRadius: true,
                    useLocalExtrema: true,
                    latField: "lat",
                    lngField: "lng",
                    valueField: "count"
                };
                this.heatLayer = L.heatLayer(heatData, heatmapConfig);
                this.map.addLayer(this.heatLayer);
            } else {
                this.heatLayer.setLatLngs(heatData);
            }
        }
    }

    async loadNewPoints() {
        if (!this.map) return;

        let outerBounds, innerBounds;
        const bounds = this.map.getBounds();

        if (this.filters.limitedBounds) {
            if (this.limitedBoundsFetched) {
                this.updateVisiblePoints();
                return;
            }
            outerBounds = {
                latitude1: parseFloat(this.filters.latitude1.toFixed(6)),
                longitude1: parseFloat(this.filters.longitude1.toFixed(6)),
                latitude2: parseFloat(this.filters.latitude2.toFixed(6)),
                longitude2: parseFloat(this.filters.longitude2.toFixed(6))
            };
            innerBounds = null;
            this.limitedBoundsFetched = true;
        } else {
            outerBounds = {
                latitude1: parseFloat(bounds.getSouthWest().lat.toFixed(6)),
                longitude1: parseFloat(bounds.getSouthWest().lng.toFixed(6)),
                latitude2: parseFloat(bounds.getNorthEast().lat.toFixed(6)),
                longitude2: parseFloat(bounds.getNorthEast().lng.toFixed(6))
            };
            this.invalidateCache(false);
            innerBounds = this.loadedBounds;
        }

        if (!this.filters.set) {
            const parameters = {
                outerBounds: outerBounds,
                innerBounds: innerBounds
            };

            const queryParams = this.buildQueryParams(parameters);

            try {
                const newPoints = await fetchPoints("range", queryParams);
                newPoints.forEach(pt => {
                    this.insertPoint(pt);
                });
                this.updateVisiblePoints();
            } catch (error) {
                console.error("Error fetching new points:", error);
                return;
            }
        } else {
            const parameters = {
                outerBounds: outerBounds,
                innerBounds: innerBounds,
                ssid: this.filters.ssid || null,
                bssid: this.filters.bssid || null,
                dateStart: this.filters.dateStart || null,
                dateEnd: this.filters.dateEnd || null
            };
            const queryParams = this.buildQueryParams(parameters);
            try {
                const newPoints = await fetchPoints("filter", queryParams);
                newPoints.forEach(pt => {
                    this.insertPoint(pt);
                });
                this.updateVisiblePoints();
            } catch (error) {
                console.error("Error fetching filtered points:", error);
                return;
            }
        }

        this.loadedBounds = outerBounds;
        this.updateSearchPointList();
    }

    buildQueryParams(parameters) {
        const query = {
            latitude1: parameters.outerBounds.latitude1,
            longitude1: parameters.outerBounds.longitude1,
            latitude2: parameters.outerBounds.latitude2,
            longitude2: parameters.outerBounds.longitude2,
        };

        if (parameters.innerBounds) {
            query.innerLatitude1 = parameters.innerBounds.latitude1;
            query.innerLongitude1 = parameters.innerBounds.longitude1;
            query.innerLatitude2 = parameters.innerBounds.latitude2;
            query.innerLongitude2 = parameters.innerBounds.longitude2;
        }

        if (parameters.ssid) query.ssid = parameters.ssid;
        if (parameters.bssid) query.bssid = parameters.bssid;
        if (parameters.dateStart) query.dateStart = parameters.dateStart;
        if (parameters.dateEnd) query.dateEnd = parameters.dateEnd;

        return query;
    }

    // Display a range circle around a point
    showRangeCircle(latitude, longitude, radiusInMeters, color) {
        this.currentRangeCircle = L.circle([latitude, longitude], {
            radius: radiusInMeters,
            color: color,
            fillColor: color,
            fillOpacity: 0.2,
            interactive: false
        });
        this.currentRangeCircle.addTo(this.map);
    }

    async showLocationsForPoint(bssid, pointLatitude, pointLongitude) {
        const locations = await fetchLocationsByBssid(bssid);

        locations.forEach(location => {
            const locationMarker = this.createLocationMarker(location);
            const locationLine = this.createDashedLine(
                pointLatitude,
                pointLongitude,
                location.latitude,
                location.longitude,
                "green"
            );
            this.locationGroup.addLayer(locationMarker);
            this.locationGroup.addLayer(locationLine);
        });

        this.setFocusVisibility(true);
    }ň

    createLocationMarker(location) {
        const marker = new HoverCircleMarker([location.latitude, location.longitude], {
            radius: 3,
            color: "green",
            fillColor: "green",
            fillOpacity: 0.7,
            interactive: true
        });
        marker.bindPopup(
            `<strong>Seen:</strong> ${new Date(location.seen).toLocaleDateString("en-GB")}<br>
               <strong>Accuracy:</strong> ${location.accuracy} m<br>
               <strong>Altitude:</strong> ${location.altitude} m<br>
               <strong>Signal:</strong> ${location.signaldBm} dBm<br>
               <strong>Frequency:</strong> ${location.frequencyMHz} MHz<br>
               <strong>Encryption:</strong> ${location.encryptionValue}`,
            { showOnHover: true }
        );
        return marker;
    }

    createDashedLine(fromLat, fromLng, toLat, toLng, color) {
        return L.polyline([[fromLat, fromLng], [toLat, toLng]], {
            color: color,
            dashArray: "5, 10"
        });
    }

    // Invalidates cache, if full is set to true it removes everything from cache
    // if false, it only removes the points outside the bounds
    invalidateCache(full) {
        
        if (full) {
            this.spatialIndex.clear();
            this.resetFilters();
        } else {
            const bounds = this.map.getBounds();
            const bbox = {
                minX: bounds.getWest(),
                minY: bounds.getSouth(),
                maxX: bounds.getEast(),
                maxY: bounds.getNorth()
            };
            const inBoundPoints = this.spatialIndex.search(bbox);
            const newSpatialIndex = new RBush();
            newSpatialIndex.load(inBoundPoints);
            this.spatialIndex = newSpatialIndex;
        }
    }

    // Apply filters and load new points
    filterWifiPoints(ssid, bssid, dateStart, dateEnd, latitude1, latitude2, longitude1, longitude2) {
        const searchWorldwide = document.getElementById("searchWorldwide");
        if (searchWorldwide && searchWorldwide.checked) {
            this.clearRendered(true);
            this.invalidateCache(true);
            this.limitedBoundsFetched = false;
            this.filters = {
                latitude1, latitude2, longitude1, longitude2,
                limitedBounds: true,
                ssid,
                bssid,
                dateStart,
                dateEnd,
                set: true
            };
            this.loadNewPoints();
        } else if (ssid || bssid || dateStart || dateEnd) {
            this.clearRendered(true);
            this.invalidateCache(true);
            this.filters = {
                ...this.filters,
                ssid,
                bssid,
                dateStart,
                dateEnd,
                set: true
            };
            this.loadNewPoints();
        }
    }

    resetFilters() {
        this.filters = {
            ssid: null,
            bssid: null,
            dateStart: null,
            dateEnd: null,
            latitude1: null,
            latitude2: null,
            longitude1: null,
            longitude2: null,
            limitedBounds: false,
            set: false
        };
    }

    // Clear rendered markers 
    // If clearAll is true, also clear search points, loaded bounds, and focus items
    clearRendered(clearAll) {
        if (this.heatLayer && this.map.hasLayer(this.heatLayer)) {
            this.map.removeLayer(this.heatLayer);
            this.heatLayer = null;
        }
        this.markerGroup.clearLayers();
        this.visiblePoints.clear();
        if (clearAll) {
            this.loadedBounds = null;
            this.clearPointFocusItems();
        }
    }

    clearPointFocusItems() {
        this.focusedPoint = null;
        this.locationGroup.clearLayers();
        if (this.currentRangeCircle && this.map.hasLayer(this.currentRangeCircle)) {
            this.map.removeLayer(this.currentRangeCircle);
        }
        this.currentRangeCircle = null;
    }

    setFocusPoint(pointLatitude, pointLongitude) {
        if (this.focusedPoint) {
            this.clearPointFocusItems();
        }
        this.focusedPoint = [pointLatitude, pointLongitude];
    }

    setFocusVisibility(visibility) {
        const bounds = this.map.getBounds();
        if (visibility && this.focusedPoint && bounds.contains(L.latLng(this.focusedPoint[0], this.focusedPoint[1]))) {
            if (!this.map.hasLayer(this.locationGroup)) {
                this.map.addLayer(this.locationGroup);
            }
            if (this.currentRangeCircle && !this.map.hasLayer(this.currentRangeCircle)) {
                this.map.addLayer(this.currentRangeCircle);
            }
        } else {
            if (this.map.hasLayer(this.locationGroup)) {
                this.map.removeLayer(this.locationGroup);
            }
            if (this.currentRangeCircle && this.map.hasLayer(this.currentRangeCircle)) {
                this.map.removeLayer(this.currentRangeCircle);
            }
        }
    }

    refreshMap() {
        this.clearRendered(true);
        this.invalidateCache(true);
        this.updateMapBoundsDisplay();
        this.loadNewPoints();
    }

    // Update bounds showed on page
    updateMapBoundsDisplay() {
        const searchWorldwide = document.getElementById("searchWorldwide");
        if (searchWorldwide && searchWorldwide.checked) {
            return;
        }

        const bounds = this.map.getBounds();
        const lat1El = document.getElementById("latitude1");
        const lat2El = document.getElementById("latitude2");
        const lng1El = document.getElementById("longitude1");
        const lng2El = document.getElementById("longitude2");
        if (lat1El) {
            lat1El.value = bounds.getSouthWest().lat.toFixed(4);
            lat1El.dispatchEvent(new Event("change", { bubbles: true }));
        }
        if (lat2El) {
            lat2El.value = bounds.getNorthEast().lat.toFixed(4);
            lat2El.dispatchEvent(new Event("change", { bubbles: true }));
        }
        if (lng1El) {
            lng1El.value = bounds.getSouthWest().lng.toFixed(4);
            lng1El.dispatchEvent(new Event("change", { bubbles: true }));
        }
        if (lng2El) {
            lng2El.value = bounds.getNorthEast().lng.toFixed(4);
            lng2El.dispatchEvent(new Event("change", { bubbles: true }));
        }
    }

    registerSearchPointsCallback(dotNetObjRef) {
        this.searchPointsCallback = dotNetObjRef;
    }

    updateSearchPointList() {
        if (this.searchPointsCallback) {
            const points = this.spatialIndex.all().map(pt => ({
                Ssid: pt.ssid,
                Bssid: pt.bssid,
                Address: pt.address || "Unknown"
            }));
            this.searchPointsCallback.invokeMethodAsync("UpdateSearchList", points);
        }
    }

    setMapToPointByBssid(bssid) {
        const allPoints = this.spatialIndex.all();
        const point = allPoints.find(pt => pt.bssid === bssid);
        if (!point) {
            console.log("No point found for bssid: " + bssid);
            return;
        }
        const targetCenter = L.latLng(point.minY, point.minX);
        const updateAndShowPopup = () => {
            this.updateVisiblePoints();
            this.clearPointFocusItems();
            setTimeout(() => {
                const marker = this.visiblePoints.get(bssid);
                if (marker && marker._map) {
                    try {
                        marker.openPopup();
                    } catch (err) {
                        console.error("Error opening popup:", err);
                    }
                } else {
                    console.warn("Marker is not on the map; cannot open popup.");
                }
            }, 100);
        };
        if (this.map.getZoom() >= 17) {
            this.forceCenter(targetCenter, this.map.getZoom());
            updateAndShowPopup();
        } else {
            this.map.setView([point.minY, point.minX], 17);
            this.map.once("zoomend", updateAndShowPopup);
        }
    }

    // force center after clicking on point in the list
    forceCenter(targetLatLng, zoom) {
        this.suppressMapEvents = true;

        if (this.map.getCenter().equals(targetLatLng)) {
            const offsetPoint = L.point(1, 1);
            const offsetLatLng = this.map.containerPointToLatLng(offsetPoint);
            this.map.setView(offsetLatLng, zoom, { animate: false });
            setTimeout(() => {
                this.map.setView(targetLatLng, zoom, { animate: false });
                this.suppressMapEvents = false;
            }, 1);
        } else {
            this.map.setView(targetLatLng, zoom, { animate: false });
            this.suppressMapEvents = false;
        }
    }
}
