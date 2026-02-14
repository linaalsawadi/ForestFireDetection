const alertConnection = new signalR.HubConnectionBuilder()
    .withUrl("/alertHub")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

alertConnection.serverTimeoutInMilliseconds = 10 * 60 * 1000;
alertConnection.keepAliveIntervalInMilliseconds = 30 * 1000;

const activeAlerts = [];

alertConnection.on("NewAlert", function (alert) {
    console.log("Received NewAlert:", alert);
    if (!alert.id) alert.id = alert.sensorId + "_" + Date.now();
    if (!activeAlerts.some(a => a.id === alert.id)) {
        activeAlerts.push(alert);
        renderAlerts();
    }
});

alertConnection.on("UpdateAlertCount", function (count) {
    const badge = document.getElementById("alert-count-badge");
    if (badge) {
        badge.textContent = count;
        badge.style.display = count > 0 ? "inline-block" : "none";
    }
});

alertConnection.on("KeepAlive", (timestamp) => {
    console.log("KeepAlive from server:", timestamp);
});

alertConnection.start()
    .then(() => console.log("Connected to alertHub"))
    .catch(err => console.error("alertHub connection failed:", err));

function renderAlerts() {
    // Remove old popups
    document.querySelectorAll(".gs-alert-container").forEach(p => p.remove());

    if (activeAlerts.length === 0) {
        const overlay = document.getElementById("alert-overlay");
        if (overlay) { overlay.classList.remove("active"); overlay.style.display = "none"; }
        const sound = document.getElementById("fire-sound");
        if (sound) { sound.pause(); sound.currentTime = 0; }
        return;
    }

    // Show overlay
    const overlay = document.getElementById("alert-overlay");
    if (overlay) { overlay.classList.add("active"); overlay.style.display = "block"; }

    // Build alert panel
    const container = document.createElement("div");
    container.className = "gs-alert-container";

    let alertsHtml = "";
    activeAlerts.forEach((alert, i) => {
        const score = Math.round(alert.fireScore ?? 0);
        const severity = score >= 75 ? "critical" : score >= 50 ? "high" : "medium";
        const severityLabel = score >= 75 ? "CRITICAL" : score >= 50 ? "HIGH" : "MEDIUM";
        const severityColor = score >= 75 ? "#ef4444" : score >= 50 ? "#f59e0b" : "#f97316";

        alertsHtml += `
        <div class="gs-alert-card gs-alert-${severity}" data-alert-index="${i}">
            <div class="gs-alert-header">
                <div class="gs-alert-severity">
                    <span class="gs-severity-dot"></span>
                    <span class="gs-severity-label">${severityLabel}</span>
                </div>
                <div class="gs-alert-score" style="color:${severityColor}">${score}%</div>
            </div>
            <div class="gs-alert-title">
                <i class="fas fa-fire-alt"></i>
                <span>Fire Detected — ${alert.sensorId}</span>
            </div>
            <div class="gs-alert-metrics">
                <div class="gs-metric">
                    <i class="fas fa-thermometer-half" style="color:#ef4444"></i>
                    <span class="gs-metric-value">${parseFloat(alert.temperature).toFixed(1)}°C</span>
                    <span class="gs-metric-label">Temp</span>
                </div>
                <div class="gs-metric">
                    <i class="fas fa-smog" style="color:#f59e0b"></i>
                    <span class="gs-metric-value">${parseFloat(alert.smoke).toFixed(1)}</span>
                    <span class="gs-metric-label">Smoke</span>
                </div>
                <div class="gs-metric">
                    <i class="fas fa-tint" style="color:#3b82f6"></i>
                    <span class="gs-metric-value">${parseFloat(alert.humidity).toFixed(1)}%</span>
                    <span class="gs-metric-label">Humidity</span>
                </div>
            </div>
            <div class="gs-alert-location">
                <i class="fas fa-map-marker-alt"></i>
                <span>${parseFloat(alert.latitude).toFixed(4)}, ${parseFloat(alert.longitude).toFixed(4)}</span>
            </div>
            <div class="gs-alert-actions">
                <button class="gs-btn gs-btn-dismiss" onclick="clearAlert('${alert.id}')">
                    <i class="fas fa-times"></i> Dismiss
                </button>
                <button class="gs-btn gs-btn-zoom" onclick='zoomToSensorFromAlert(${alert.latitude},${alert.longitude},"${alert.sensorId}",${JSON.stringify(alert).replace(/'/g, "\\'")})'>
                    <i class="fas fa-crosshairs"></i> Locate
                </button>
                <button class="gs-btn gs-btn-ack" onclick="acknowledge('${alert.id}')">
                    <i class="fas fa-check-double"></i> Acknowledge
                </button>
            </div>
        </div>`;
    });

    container.innerHTML = `
        <div class="gs-alert-panel">
            <div class="gs-alert-panel-header">
                <div class="gs-alert-icon-pulse"></div>
                <h4><i class="fas fa-exclamation-triangle"></i> ${activeAlerts.length} Active Alert${activeAlerts.length > 1 ? 's' : ''}</h4>
                <button class="gs-btn-close-all" onclick="clearAllAlerts()" title="Dismiss All">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            <div class="gs-alert-list">${alertsHtml}</div>
        </div>
    `;

    document.body.appendChild(container);

    // Play sound
    const sound = document.getElementById("fire-sound");
    if (sound) sound.play().catch(() => {});
}

function clearAlert(alertId) {
    const index = activeAlerts.findIndex(a => a.id === alertId);
    if (index !== -1) activeAlerts.splice(index, 1);
    renderAlerts();
}

function clearAllAlerts() {
    activeAlerts.length = 0;
    renderAlerts();
}

function acknowledge(alertId) {
    fetch(`/Alerts/Acknowledge/${alertId}`, { method: "POST" })
        .then(res => {
            if (res.ok) {
                clearAlert(alertId);
                showToast("Alert acknowledged successfully!", "success");
            } else {
                showToast("Failed to acknowledge alert.", "error");
            }
        })
        .catch(() => showToast("Network error.", "error"));
}

function zoomToSensorFromAlert(latitude, longitude, sensorId, alert) {
    sessionStorage.setItem("pendingAlert", JSON.stringify(alert));
    sessionStorage.setItem("zoomTarget", JSON.stringify({ latitude, longitude, sensorId }));
    window.location.href = `/Map/Index`;
}

function showToast(message, type) {
    const toastArea = document.getElementById("toastArea");
    if (!toastArea) return;

    const id = `toast-${Date.now()}`;
    const isError = type === "error";
    const icon = isError ? "fa-exclamation-circle" : "fa-check-circle";
    const bg = isError ? "#ef4444" : "#22c55e";

    toastArea.insertAdjacentHTML("beforeend", `
        <div id="${id}" class="gs-toast" style="border-left:4px solid ${bg}">
            <i class="fas ${icon}" style="color:${bg};font-size:1.2rem;"></i>
            <span>${message}</span>
            <button onclick="this.parentElement.remove()" style="background:none;border:none;color:#9ca3af;cursor:pointer;font-size:1.1rem;">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `);

    setTimeout(() => document.getElementById(id)?.remove(), 4000);
}

// Restore alert from session
const alertJson = sessionStorage.getItem("pendingAlert");
if (alertJson) {
    try {
        const alert = JSON.parse(alertJson);
        if (!alert.id) alert.id = alert.sensorId + "_" + Date.now();
        activeAlerts.push(alert);
        renderAlerts();
        sessionStorage.removeItem("pendingAlert");
    } catch (e) { console.warn("Failed to parse stored alert:", e); }
}