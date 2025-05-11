// ✅ سكريبت الخريطة المحدث مع تحديث القيم فقط دون إعادة فتح اللوحة
window.onload = function () {
    const mapContainer = document.getElementById('map');
    if (!mapContainer) return;

    var map = L.map('map').setView([40.7423, 30.3338], 15);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    window.sensorMarkers = window.sensorMarkers || {};

    let latitude, longitude, sensorId;
    const zoomJson = sessionStorage.getItem("zoomTarget");
    if (zoomJson) {
        try {
            const zoomData = JSON.parse(zoomJson);
            latitude = zoomData.latitude;
            longitude = zoomData.longitude;
            sensorId = zoomData.sensorId;
            sessionStorage.removeItem("zoomTarget");
        } catch (e) {
            console.warn("Failed to parse zoomTarget:", e);
        }
    }

    const mapHubConnection = new signalR.HubConnectionBuilder()
        .withUrl("/mapHub")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    mapHubConnection.serverTimeoutInMilliseconds = 10 * 60 * 1000;
    mapHubConnection.keepAliveIntervalInMilliseconds = 30 * 1000;


    mapHubConnection.on("UpdateSensor", function (data) {
        updateSensorOnMap(data, map);
    });

    mapHubConnection.start().catch(function (err) {
        console.error("SignalR connection failed:", err.toString());
    });

    window.updateSensorOnMap = function (data, mapRef = map) {
        const sensorId = data.sensorId;
        const sensorState = data.sensorState?.toLowerCase() || "green";
        const iconPath = `/icons/sensor-${sensorState}.png`;

        const updatedIcon = L.icon({
            iconUrl: iconPath,
            iconSize: [32, 32],
            iconAnchor: [16, 32],
            popupAnchor: [0, -32]
        });

        if (sensorMarkers[sensorId]) {
            const marker = sensorMarkers[sensorId];
            marker.setLatLng([data.latitude, data.longitude]);
            marker.setIcon(updatedIcon);
        } else {
            const marker = L.marker([data.latitude, data.longitude], { icon: updatedIcon }).addTo(map);
            marker.on('click', function () {
                showSensorPanel(sensorId);
            });
            sensorMarkers[sensorId] = marker;
        }

        if (latitude && longitude && sensorId) {
            map.setView([parseFloat(latitude), parseFloat(longitude)], 15);
            showSensorPanel(sensorId);
        }

        // ✅ تحديث القيم على الخط البياني فقط دون إعادة فتح اللوحة
        if (window.currentOpenSensorId === sensorId) {
            updateSensorPanelCharts(sensorId, data);
        }
    };
};

function showSensorPanel(sensorId) {
    window.currentOpenSensorId = sensorId;
    document.getElementById("sensor-title").innerText = `Sensor: ${sensorId}`;
    document.querySelector('[data-widget="control-sidebar"]').click();

    fetch(`/Sensors/GetSensorData?sensorId=${sensorId}`)
        .then(res => res.json())
        .then(data => {
            drawSensorPanelCharts(sensorId, data);
        });
}

function drawSensorPanelCharts(sensorId, data) {
    document.getElementById("sensor-panel-body").innerHTML = `
        <div class="mb-4">
            <h6><i class="fas fa-thermometer-half text-danger"></i> Temperature (°C)</h6>
            <canvas id="tempChart-${sensorId}" height="200"></canvas>
        </div>
        <div class="mb-4">
            <h6><i class="fas fa-smog text-warning"></i> Smoke</h6>
            <canvas id="smokeChart-${sensorId}" height="200"></canvas>
        </div>
        <div class="mb-4">
            <h6><i class="fas fa-tint text-info"></i> Humidity (%)</h6>
            <canvas id="humidityChart-${sensorId}" height="200"></canvas>
        </div>
    `;

    renderLineChart(`tempChart-${sensorId}`, "Temperature (°C)", data.map(d => ({
        timestamp: d.timestamp,
        value: d.temperature
    })), "rgba(255, 99, 132, 1)");

    renderLineChart(`humidityChart-${sensorId}`, "Humidity (%)", data.map(d => ({
        timestamp: d.timestamp,
        value: d.humidity
    })), "rgba(54, 162, 235, 1)");

    renderLineChart(`smokeChart-${sensorId}`, "Smoke", data.map(d => ({
        timestamp: d.timestamp,
        value: d.smoke
    })), "rgba(255, 206, 86, 1)");
}

function updateSensorPanelCharts(sensorId, latestData) {
    const charts = [
        { id: `tempChart-${sensorId}`, value: latestData.temperature },
        { id: `humidityChart-${sensorId}`, value: latestData.humidity },
        { id: `smokeChart-${sensorId}`, value: latestData.smoke }
    ];

    charts.forEach(({ id, value }) => {
        const canvas = document.getElementById(id);
        if (canvas) {
            const chart = Chart.getChart(id);
            if (chart) {
                const time = new Date(latestData.timestamp).toLocaleTimeString();
                chart.data.labels.push(time);
                chart.data.datasets[0].data.push(value);

                if (chart.data.labels.length > 10) {
                    chart.data.labels.shift();
                    chart.data.datasets[0].data.shift();
                }
                chart.update();
            }
        }
    });
}
