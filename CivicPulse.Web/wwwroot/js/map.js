window.CivicMap = {
    maps: {},

    initPickerMap: function (elementId, defaultLat, defaultLng, dotNetRef) {
        var map = L.map(elementId).setView([defaultLat || 33.6844, defaultLng || 73.0479], 13);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors',
            maxZoom: 19
        }).addTo(map);

        var marker = null;

        map.on('click', function (e) {
            if (marker) map.removeLayer(marker);
            marker = L.marker(e.latlng).addTo(map);

            // Notify Blazor of click location
            dotNetRef.invokeMethodAsync('OnLocationSelected', e.latlng.lat, e.latlng.lng);

            // Reverse geocode on click
            window.CivicLocation.reverseGeocode(e.latlng.lat, e.latlng.lng)
                .then(function(address) {
                    if (address) {
                        dotNetRef.invokeMethodAsync('OnAddressResolved', address);
                    }
                });
        });

        // Store dotNetRef so panMapToLocation can use it
        window.CivicMap.maps[elementId] = { map: map, marker: marker, dotNetRef: dotNetRef };
    },

    placeMarker: function (elementId, lat, lng) {
        var entry = window.CivicMap.maps[elementId];
        if (!entry) return;
        var map = entry.map;
        if (entry.marker) map.removeLayer(entry.marker);
        entry.marker = L.marker([lat, lng]).addTo(map);
        map.setView([lat, lng], 15);
    },

    initViewMap: function (elementId, lat, lng, complaintTitle) {
        var map = L.map(elementId).setView([lat, lng], 15);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        var marker = L.marker([lat, lng]).addTo(map);
        marker.bindPopup('<b>' + (complaintTitle || '') + '</b>');

        setTimeout(function () { map.invalidateSize(); }, 100);
        window.CivicMap.maps[elementId] = { map: map };
    },

    initHeatmap: function (elementId, complaints) {
        var map = L.map(elementId).setView([33.6844, 73.0479], 12);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; OpenStreetMap contributors'
        }).addTo(map);

        var colorMap = {
            'Critical': '#dc3545', 'High': '#fd7e14',
            'Medium': '#ffc107', 'Low': '#28a745'
        };

        if (complaints) {
            complaints.forEach(function (c) {
                if (c.latitude && c.longitude) {
                    var circle = L.circleMarker([c.latitude, c.longitude], {
                        radius: 8,
                        fillColor: colorMap[c.priority] || '#6c757d',
                        color: '#fff', weight: 2, fillOpacity: 0.8
                    }).addTo(map);
                    circle.bindPopup(
                        '<b>' + c.complaintNumber + '</b><br>' +
                        c.categoryName + '<br>' +
                        '<span style="color:' + (colorMap[c.priority] || '#6c757d') + '">' + c.priority + '</span><br>' +
                        c.status
                    );
                }
            });
        }

        setTimeout(function () { map.invalidateSize(); }, 100);
        window.CivicMap.maps[elementId] = { map: map };
    },

    setViewAndPin: function(elementId, lat, lng) {
        var mapObj = window.CivicMap.maps[elementId];
        if (!mapObj) return;
        var map = mapObj.map;
        map.setView([lat, lng], 16);

        if (mapObj.marker) map.removeLayer(mapObj.marker);
        mapObj.marker = L.marker([lat, lng]).addTo(map);
        mapObj.marker.bindPopup('<b>📍 Your Location</b>').openPopup();
    },

    destroyMap: function (elementId) {
        if (window.CivicMap.maps[elementId]) {
            window.CivicMap.maps[elementId].map.remove();
            delete window.CivicMap.maps[elementId];
        }
    },

    getCurrentPosition: function (dotNetRef, attempt) {
        if (!navigator.geolocation) {
            dotNetRef.invokeMethodAsync('OnGeolocationError', 'Geolocation not supported by your browser.');
            return;
        }
        attempt = attempt || 1;
        var useHigh = attempt === 1;
        var timeout = useHigh ? 20000 : 15000;
        navigator.geolocation.getCurrentPosition(
            function (pos) {
                dotNetRef.invokeMethodAsync('OnGeolocationReceived', pos.coords.latitude, pos.coords.longitude);
                fetch('https://nominatim.openstreetmap.org/reverse?lat=' + pos.coords.latitude + '&lon=' + pos.coords.longitude + '&format=json')
                    .then(function (r) { return r.json(); })
                    .then(function (data) {
                        dotNetRef.invokeMethodAsync('OnAddressResolved', data.display_name || '');
                    })
                    .catch(function () {});
            },
            function (err) {
                if (err.code === 3 && attempt < 2) {
                    // Timeout with GPS — fall back to WiFi/cell positioning
                    window.CivicMap.getCurrentPosition(dotNetRef, attempt + 1);
                    return;
                }
                var msg = 'Unable to retrieve your location.';
                if (err.code === 1) msg = 'Location access denied. Please enable location permissions in your browser.';
                else if (err.code === 2) msg = 'Location unavailable. Try again later.';
                else if (err.code === 3) msg = 'Location timed out. You can type your address manually.';
                dotNetRef.invokeMethodAsync('OnGeolocationError', msg);
            },
            { enableHighAccuracy: useHigh, timeout: timeout, maximumAge: 60000 }
        );
    }
};

// ─────────────────────────────────────────────
// CivicLocation — GPS + Geocoding helpers
// ─────────────────────────────────────────────
window.CivicLocation = {

    // API key set from Blazor config
    _ipInfoToken: '',

    setApiKey: function(token) {
        window.CivicLocation._ipInfoToken = token;
    },

    // Returns a JSON STRING — avoids Blazor interop serialization issues
    getCurrentPositionJson: function () {

        var IPINFO_TOKEN = window.CivicLocation._ipInfoToken || '';

        return new Promise(function (resolve) {

            // ── STEP 1: Browser GPS — try high accuracy with generous timeout ──
            if (navigator.geolocation) {

                // First attempt: high accuracy (real GPS)
                navigator.geolocation.getCurrentPosition(
                    function (pos) {
                        resolve(JSON.stringify({
                            lat: pos.coords.latitude,
                            lng: pos.coords.longitude,
                            accuracy: pos.coords.accuracy,
                            source: 'gps',
                            error: null
                        }));
                    },
                    function (err) {
                        // High accuracy failed — try low accuracy (WiFi triangulation)
                        navigator.geolocation.getCurrentPosition(
                            function (pos) {
                                resolve(JSON.stringify({
                                    lat: pos.coords.latitude,
                                    lng: pos.coords.longitude,
                                    accuracy: pos.coords.accuracy,
                                    source: 'wifi',
                                    error: null
                                }));
                            },
                            function () {
                                // Both GPS attempts failed — use IPInfo as last resort
                                getByIpInfo(resolve, IPINFO_TOKEN);
                            },
                            {
                                enableHighAccuracy: false,
                                timeout: 10000,
                                maximumAge: 30000
                            }
                        );
                    },
                    {
                        enableHighAccuracy: true,
                        timeout: 15000,
                        maximumAge: 0
                    }
                );

            } else {
                getByIpInfo(resolve, IPINFO_TOKEN);
            }
        });

        function getByIpInfo(resolve, token) {
            if (!token) { useDefault(resolve); return; }

            fetch('https://ipinfo.io/json?token=' + token)
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (data && data.loc) {
                        var parts = data.loc.split(',');
                        var lat = parseFloat(parts[0]);
                        var lng = parseFloat(parts[1]);
                        if (!isNaN(lat) && !isNaN(lng)) {
                            resolve(JSON.stringify({
                                lat: lat,
                                lng: lng,
                                city: data.city || '',
                                region: data.region || '',
                                source: 'ip',
                                error: null
                            }));
                            return;
                        }
                    }
                    useDefault(resolve);
                })
                .catch(function () { useDefault(resolve); });
        }

        function useDefault(resolve) {
            resolve(JSON.stringify({
                lat: 33.6844,
                lng: 73.0479,
                city: 'Islamabad',
                region: 'Islamabad',
                source: 'default',
                error: null
            }));
        }
    },

    // Pan the Leaflet map to given coordinates and drop a marker
    panMapToLocation: function (lat, lng) {
        // Find the active map instance — try known element IDs
        var mapKeys = Object.keys(window.CivicMap.maps);
        if (mapKeys.length === 0) return;

        var mapObj = window.CivicMap.maps[mapKeys[0]];
        if (!mapObj || !mapObj.map) return;

        // Pan and zoom to location
        mapObj.map.setView([lat, lng], 16, { animate: true });

        // Remove old marker if exists
        if (mapObj.marker) {
            mapObj.map.removeLayer(mapObj.marker);
        }

        // Drop new marker
        mapObj.marker = L.marker([lat, lng]).addTo(mapObj.map);
        mapObj.marker.bindPopup('<b>📍 Your Location</b><br>Lat: ' +
            lat.toFixed(5) + '<br>Lng: ' + lng.toFixed(5)).openPopup();

        // Also notify Blazor via the existing DotNetRef if available
        if (mapObj.dotNetRef) {
            mapObj.dotNetRef.invokeMethodAsync('OnLocationSelected', lat, lng);
        }
    },

    // Nominatim reverse geocoding — free, no API key needed
    reverseGeocode: function (lat, lng) {
        return fetch(
            'https://nominatim.openstreetmap.org/reverse?lat=' + lat +
            '&lon=' + lng + '&format=json&accept-language=en',
            { headers: { 'User-Agent': 'CivicPulse/1.0' } }
        )
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (!data || !data.display_name) return '';
            // Return shortened address (first 4 parts)
            var parts = data.display_name.split(',');
            return parts.slice(0, Math.min(4, parts.length))
                        .map(function(p){ return p.trim(); })
                        .filter(Boolean)
                        .join(', ');
        })
        .catch(function () { return ''; });
    }
};
