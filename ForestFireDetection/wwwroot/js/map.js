// ═══════════════════════════════════════════
// GREEN SHIELD — Map + Sensor Sidebar Panel
// ═══════════════════════════════════════════

let map, markers = {};
const sidebarCharts = {};

// ── Initialize Map ──
document.addEventListener("DOMContentLoaded", function () {
    map = L.map("map").setView([40.7400, 31.6200], 13);
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: '&copy; OpenStreetMap',
        maxZoom: 18
    }).addTo(map);

    loadSensors();
    setupSignalR();
    checkZoomTarget();
});

function getMarkerIcon(state) {
    const colors = { green: "#22c55e", yellow: "#f59e0b", red: "#ef4444", offline: "#6b7280" };
    const icons = { green: "fa-check-circle", yellow: "fa-exclamation-circle", red: "fa-fire-alt", offline: "fa-power-off" };
    const color = colors[state] || colors.green;
    const icon = icons[state] || icons.green;
    const pulse = state === "red" ? `<div style="position:absolute;inset:-6px;border-radius:50%;border:2px solid ${color};animation:pulseDot 1.5s infinite;"></div>` : "";

    return L.divIcon({
        className: "gs-marker",
        html: `<div style="position:relative;width:36px;height:36px;">
            ${pulse}
            <div style="width:36px;height:36px;border-radius:50%;background:${color};display:flex;align-items:center;justify-content:center;box-shadow:0 3px 12px ${color}66;border:2.5px solid #fff;">
                <i class="fas ${icon}" style="color:#fff;font-size:14px;"></i>
            </div>
        </div>`,
        iconSize: [36, 36],
        iconAnchor: [18, 18],
        popupAnchor: [0, -20]
    });
}

function loadSensors() {
    fetch("/Sensors/GetSensors")
        .then(r => r.json())
        .then(sensors => sensors.forEach(s => addOrUpdateMarker(s)))
        .catch(err => console.error("Failed to load sensors:", err));
}

function addOrUpdateMarker(sensor) {
    const lat = sensor.latitude || sensor.lat || 0;
    const lng = sensor.longitude || sensor.lng || 0;
    if (lat === 0 && lng === 0) return;

    const icon = getMarkerIcon(sensor.sensorState);

    if (markers[sensor.sensorId]) {
        markers[sensor.sensorId].setLatLng([lat, lng]).setIcon(icon);
    } else {
        const marker = L.marker([lat, lng], { icon: icon }).addTo(map);
        marker.on("click", () => openSensorPanel(sensor.sensorId, sensor.sensorState));
        markers[sensor.sensorId] = marker;
    }
}

function setupSignalR() {
    const conn = new signalR.HubConnectionBuilder()
        .withUrl("/mapHub")
        .withAutomaticReconnect()
        .build();

    conn.serverTimeoutInMilliseconds = 10 * 60 * 1000;
    conn.keepAliveIntervalInMilliseconds = 30 * 1000;

    conn.on("UpdateSensor", (data) => {
        addOrUpdateMarker(data);

        // Update sidebar if this sensor is open
        const panel = document.getElementById("sensor-panel-body");
        if (panel && panel.dataset.sensorId === data.sensorId) {
            updateSidebarLive(data);
        }
    });

    conn.start().then(() => console.log("Connected to mapHub")).catch(err => console.error("mapHub error:", err));
}

function checkZoomTarget() {
    const zoomJson = sessionStorage.getItem("zoomTarget");
    if (zoomJson) {
        try {
            const z = JSON.parse(zoomJson);
            map.setView([z.latitude, z.longitude], 16);
            if (z.sensorId) openSensorPanel(z.sensorId);
            sessionStorage.removeItem("zoomTarget");
        } catch (e) { console.warn(e); }
    }
}

// ── Sensor Detail Panel ──
function openSensorPanel(sensorId, state) {
    // Open the sidebar
    $('body').addClass('control-sidebar-open control-sidebar-slide-open');

    const panelBody = document.getElementById("sensor-panel-body");
    const panelTitle = document.getElementById("sensor-title");
    if (!panelBody) return;

    panelBody.dataset.sensorId = sensorId;
    panelBody.innerHTML = `
        <div style="display:flex;align-items:center;justify-content:center;padding:40px 0;">
            <div class="spinner-border text-success" role="status" style="width:2rem;height:2rem;"></div>
        </div>
    `;
    if (panelTitle) panelTitle.style.display = "none";

    fetch(`/Sensors/GetSensorData?sensorId=${sensorId}`)
        .then(r => r.json())
        .then(data => renderSensorPanel(sensorId, state || "green", data, panelBody))
        .catch(() => { panelBody.innerHTML = `<p style="color:#ef4444;text-align:center;padding:20px;">Failed to load data</p>`; });
}

function renderSensorPanel(sensorId, state, data, container) {
    const latest = data.length > 0 ? data[data.length - 1] : null;
    const temp = latest ? parseFloat(latest.temperature).toFixed(1) : "--";
    const hum = latest ? parseFloat(latest.humidity).toFixed(1) : "--";
    const smoke = latest ? parseFloat(latest.smoke).toFixed(1) : "--";
    const score = latest ? parseFloat(latest.fireScore || 0).toFixed(1) : "0";

    const stateLabel = { green: "Normal", yellow: "Warning", red: "Critical", offline: "Offline" };
    const stateColor = { green: "#22c55e", yellow: "#f59e0b", red: "#ef4444", offline: "#6b7280" };
    const scoreColor = score >= 75 ? "#ef4444" : score >= 50 ? "#f59e0b" : "#22c55e";

    container.innerHTML = `
        <div class="gs-panel-header">
            <div class="gs-panel-sensor-icon ${state}">
                <i class="fas ${state === 'red' ? 'fa-fire-alt' : state === 'offline' ? 'fa-power-off' : 'fa-microchip'}"></i>
            </div>
            <div class="gs-panel-title">
                <h3>${sensorId}</h3>
                <span class="gs-panel-status" style="color:${stateColor[state] || stateColor.green}">
                    <i class="fas fa-circle" style="font-size:.4rem;vertical-align:middle;"></i>
                    ${stateLabel[state] || 'Unknown'}
                </span>
            </div>
        </div>

        <div class="gs-panel-section">
            <div class="gs-panel-section-title">Fire Risk Score</div>
            <div class="gs-score-container">
                <div class="gs-score-ring" id="score-ring-${sensorId}" style="background:conic-gradient(${scoreColor} ${score * 3.6}deg, rgba(255,255,255,.06) 0);">
                    <span class="gs-score-value" id="score-value-${sensorId}">${score}<small>%</small></span>
                </div>
            </div>
        </div>

        <div class="gs-panel-section">
            <div class="gs-panel-section-title">Current Readings</div>
            <div class="gs-panel-stat-grid">
                <div class="gs-panel-stat">
                    <i class="fas fa-thermometer-half" style="color:#ef4444"></i>
                    <span class="gs-stat-value" id="live-temp-${sensorId}">${temp}°</span>
                    <span class="gs-stat-label">Temperature</span>
                </div>
                <div class="gs-panel-stat">
                    <i class="fas fa-smog" style="color:#f59e0b"></i>
                    <span class="gs-stat-value" id="live-smoke-${sensorId}">${smoke}</span>
                    <span class="gs-stat-label">Smoke</span>
                </div>
                <div class="gs-panel-stat">
                    <i class="fas fa-tint" style="color:#3b82f6"></i>
                    <span class="gs-stat-value" id="live-hum-${sensorId}">${hum}%</span>
                    <span class="gs-stat-label">Humidity</span>
                </div>
                <div class="gs-panel-stat">
                    <i class="fas fa-fire" style="color:${scoreColor}"></i>
                    <span class="gs-stat-value" id="live-score-${sensorId}" style="color:${scoreColor}">${score}%</span>
                    <span class="gs-stat-label">Fire Score</span>
                </div>
            </div>
        </div>

        <div class="gs-panel-section">
            <div class="gs-panel-section-title">Live Charts</div>
            <div class="gs-panel-chart-container">
                <div class="gs-panel-chart-label"><i class="fas fa-thermometer-half" style="color:#ef4444"></i> Temperature</div>
                <canvas id="sidebar-temp-${sensorId}" height="120"></canvas>
            </div>
            <div class="gs-panel-chart-container">
                <div class="gs-panel-chart-label"><i class="fas fa-smog" style="color:#f59e0b"></i> Smoke</div>
                <canvas id="sidebar-smoke-${sensorId}" height="120"></canvas>
            </div>
            <div class="gs-panel-chart-container">
                <div class="gs-panel-chart-label"><i class="fas fa-tint" style="color:#3b82f6"></i> Humidity</div>
                <canvas id="sidebar-hum-${sensorId}" height="120"></canvas>
            </div>
        </div>
    `;

    // Draw charts
    const labels = data.map(d => new Date(d.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));
    drawSidebarChart(`sidebar-temp-${sensorId}`, labels, data.map(d => d.temperature), "#ef4444", "rgba(239,68,68,.1)");
    drawSidebarChart(`sidebar-smoke-${sensorId}`, labels, data.map(d => d.smoke), "#f59e0b", "rgba(245,158,11,.1)");
    drawSidebarChart(`sidebar-hum-${sensorId}`, labels, data.map(d => d.humidity), "#3b82f6", "rgba(59,130,246,.1)");
}

function drawSidebarChart(canvasId, labels, data, color, bgColor) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;
    if (sidebarCharts[canvasId]) sidebarCharts[canvasId].destroy();

    sidebarCharts[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels.slice(-15),
            datasets: [{
                data: data.slice(-15),
                borderColor: color,
                backgroundColor: bgColor,
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: 0,
                pointHoverRadius: 4,
                pointHoverBackgroundColor: color,
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 300 },
            plugins: { legend: { display: false }, tooltip: { mode: 'index', intersect: false, backgroundColor: '#1f2937', titleFont: { size: 11 }, bodyFont: { size: 11 }, padding: 8, cornerRadius: 8 } },
            scales: {
                x: { display: true, ticks: { maxTicksLimit: 5, font: { size: 9 }, color: 'rgba(255,255,255,.3)' }, grid: { display: false } },
                y: { beginAtZero: true, ticks: { maxTicksLimit: 4, font: { size: 9 }, color: 'rgba(255,255,255,.3)' }, grid: { color: 'rgba(255,255,255,.04)' } }
            }
        }
    });
}

function updateSidebarLive(data) {
    const sid = data.sensorId;

    // Update stat values
    const tempEl = document.getElementById(`live-temp-${sid}`);
    const smokeEl = document.getElementById(`live-smoke-${sid}`);
    const humEl = document.getElementById(`live-hum-${sid}`);
    const scoreEl = document.getElementById(`live-score-${sid}`);
    const scoreValEl = document.getElementById(`score-value-${sid}`);
    const scoreRingEl = document.getElementById(`score-ring-${sid}`);

    if (tempEl) tempEl.textContent = parseFloat(data.temperature).toFixed(1) + "°";
    if (smokeEl) smokeEl.textContent = parseFloat(data.smoke).toFixed(1);
    if (humEl) humEl.textContent = parseFloat(data.humidity).toFixed(1) + "%";

    const fs = data.fireScore ? parseFloat(data.fireScore).toFixed(1) : "0";
    const sc = parseFloat(fs);
    const sColor = sc >= 75 ? "#ef4444" : sc >= 50 ? "#f59e0b" : "#22c55e";

    if (scoreEl) { scoreEl.textContent = fs + "%"; scoreEl.style.color = sColor; }
    if (scoreValEl) scoreValEl.innerHTML = `${fs}<small>%</small>`;
    if (scoreRingEl) scoreRingEl.style.background = `conic-gradient(${sColor} ${sc * 3.6}deg, rgba(255,255,255,.06) 0)`;

    // Update live charts
    const time = new Date(data.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    pushToChart(`sidebar-temp-${sid}`, time, data.temperature);
    pushToChart(`sidebar-smoke-${sid}`, time, data.smoke);
    pushToChart(`sidebar-hum-${sid}`, time, data.humidity);
}

function pushToChart(chartId, label, value) {
    const chart = sidebarCharts[chartId];
    if (!chart) return;
    chart.data.labels.push(label);
    chart.data.datasets[0].data.push(value);
    if (chart.data.labels.length > 20) {
        chart.data.labels.shift();
        chart.data.datasets[0].data.shift();
    }
    chart.update('none');
}

// ── Search ──
document.getElementById("sensorSearch")?.addEventListener("keyup", function () {
    const val = this.value.trim().toUpperCase();
    if (markers[val]) {
        map.setView(markers[val].getLatLng(), 16);
        openSensorPanel(val);
    }
});