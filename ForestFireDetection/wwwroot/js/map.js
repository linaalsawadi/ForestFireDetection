window.onload = function () {
    // إنشاء الخريطة بعد التأكد من وجود العنصر
    const mapContainer = document.getElementById('map');
    if (!mapContainer) {
        console.error("❌ Map container not found.");
        return;
    }

    // إنشاء الخريطة
    var map = L.map('map').setView([40.7423, 30.3338], 15);


    // إضافة طبقة OpenStreetMap
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // ✅ كائن لتخزين الماركرات حسب sensorId
    window.sensorMarkers = window.sensorMarkers || {};

    // استخدم قيم URL إن وجدت للزوم على حساس معين
    let latitude, longitude, sensorId;

    const zoomJson = sessionStorage.getItem("zoomTarget");
    if (zoomJson) {
        try {
            const zoomData = JSON.parse(zoomJson);
            latitude = zoomData.latitude;
            longitude = zoomData.longitude;
            sensorId = zoomData.sensorId;

            sessionStorage.removeItem("zoomTarget"); // نزيلها بعد الاستخدام
        } catch (e) {
            console.warn("Failed to parse zoomTarget:", e);
        }
    }


    // اتصال مع SignalR MapHub
    const mapHubConnection = new signalR.HubConnectionBuilder()
        .withUrl("/mapHub")
        .build();

    mapHubConnection.on("UpdateSensor", function (data) {
        console.log("Update received:", data);
        updateSensorOnMap(data, map);
    });

    mapHubConnection.start().catch(function (err) {
        console.error("SignalR connection failed:", err.toString());
    });

    // ✅ دالة تحديث أو إنشاء الماركر
    window.updateSensorOnMap = function (data, mapRef = map) {
        const sensorId = data.sensorId;
        const sensorState = data.sensorState?.toLowerCase() || "green"; // default fallback
        const iconPath = `/icons/sensor-${sensorState}.png`;

        // إنشاء الأيقونة الجديدة حسب الحالة
        const updatedIcon = L.icon({
            iconUrl: iconPath,
            iconSize: [32, 32],
            iconAnchor: [16, 32],
            popupAnchor: [0, -32]
        });

        const popupContent = `
            <b>Sensor ID:</b> ${sensorId}<br/>
            <b>Temp:</b> ${data.temperature} °C<br/>
            <b>Smoke:</b> ${data.smoke}<br/>
            <b>Humidity:</b> ${data.humidity} %<br/>
            <b>Updated:</b> ${new Date(data.timestamp).toLocaleTimeString()}
        `;

        if (sensorMarkers[sensorId]) {
            // ✅ تحديث الموقع، المحتوى، والأيقونة
            const marker = sensorMarkers[sensorId];
            marker.setLatLng([data.latitude, data.longitude]);
            marker.setPopupContent(popupContent);
            marker.setIcon(updatedIcon); // ✅ تغيير الأيقونة حسب الحالة
        } else {
            // إذا لم يكن موجودًا، ننشئه لأول مرة
            const marker = L.marker([data.latitude, data.longitude], { icon: updatedIcon }).addTo(map);
            marker.bindPopup(popupContent);
            sensorMarkers[sensorId] = marker;
        }
        if (latitude && longitude && sensorId) {
            map.setView([parseFloat(latitude), parseFloat(longitude)], 15);
            sensorMarkers[sensorId].openPopup();
        }
    }

};
