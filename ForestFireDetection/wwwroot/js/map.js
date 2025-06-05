// ✅ سكريبت الخريطة المحدث مع تحديث القيم فقط دون إعادة فتح اللوحة

const charts = {};
window.onload = function () {
    const mapContainer = document.getElementById('map');
    if (!mapContainer) return;

    var map = L.map('map').setView([40.7423, 30.3338], 15);
    window.map = map;

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

    window.mapHubConnection = mapHubConnection;
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

        if (window.currentOpenSensorId === sensorId) {
            updateSensorPanelCharts(sensorId, data);
        }
    };
};

function showSensorPanel(sensorId) {
    window.currentOpenSensorId = sensorId;
    const title = document.getElementById("sensor-title");
    if (title) {
        title.innerText = `Sensor: ${sensorId}`;
    }
    const controlSidebar = document.querySelector('[data-widget="control-sidebar"]');
    if (controlSidebar) {
        controlSidebar.click();
    }

    fetch(`/Sensors/GetSensorData?sensorId=${sensorId}`)
        .then(res => res.json())
        .then(data => {
            drawSensorPanelCharts(sensorId, data);
        });
}

function drawSensorPanelCharts(sensorId, data) {
    const panelBody = document.getElementById("sensor-panel-body");
    if (!panelBody) return;
    panelBody.innerHTML = `
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

    renderChart(`tempChart-${sensorId}`, "Temperature (°C)", data.map(d => ({
        timestamp: d.timestamp,
        value: d.temperature
    })), "rgba(255, 99, 132, 1)");

    renderChart(`humidityChart-${sensorId}`, "Humidity (%)", data.map(d => ({
        timestamp: d.timestamp,
        value: d.humidity
    })), "rgba(54, 162, 235, 1)");

    renderChart(`smokeChart-${sensorId}`, "Smoke", data.map(d => ({
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

if (typeof mapHubConnection !== 'undefined') {
    mapHubConnection.on("KeepAlive", (timestamp) => {
        console.log("KeepAlive from server:", timestamp);
    });
}

function renderChart(canvasId, label, data, color) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;

    if (charts[canvasId] && typeof charts[canvasId].destroy === "function") {
        charts[canvasId].destroy();
    }

    charts[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => new Date(d.timestamp).toLocaleTimeString()),
            datasets: [{
                label: label,
                data: data.map(d => d.value),
                borderColor: color,
                backgroundColor: color.replace("1)", "0.1)"),
                fill: true,
                tension: 0.3,
                pointRadius: 2
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: true, position: 'top' }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    suggestedMax: Math.max(...data.map(d => d.value)) + 20
                }
            }
        }
    });
}