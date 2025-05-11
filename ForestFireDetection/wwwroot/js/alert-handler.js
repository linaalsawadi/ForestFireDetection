const alertConnection = new signalR.HubConnectionBuilder()
    .withUrl("/alertHub")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

alertConnection.serverTimeoutInMilliseconds = 10 * 60 * 1000;
alertConnection.keepAliveIntervalInMilliseconds = 30 * 1000;

const activeAlerts = [];

alertConnection.on("NewAlert", function (alert) {
    console.log("✅ Received NewAlert:", alert);

    // ضمان وجود id فريد
    if (!alert.id) {
        alert.id = alert.sensorId + "_" + Date.now();
    }

    // تجاهل التكرارات
    if (!activeAlerts.some(a => a.id === alert.id)) {
        activeAlerts.push(alert);
        renderAlerts();
    }
});

alertConnection.start()
    .then(() => console.log("✅ Connected to alertHub"))
    .catch(err => console.error("❌ alertHub connection failed:", err));

function renderAlerts() {
    console.log("🔊 Rendering alerts:", activeAlerts.length);

    // حذف التنبيهات القديمة من الشاشة
    document.querySelectorAll(".alert-popup").forEach(p => p.remove());

    // إنشاء خلفية وميض إذا غير موجودة
    const overlay = document.getElementById("alert-overlay") || (() => {
        const div = document.createElement("div");
        div.id = "alert-overlay";
        document.body.appendChild(div);
        return div;
    })();

    overlay.classList.add("blink");
    overlay.style.display = "block";

    // إنشاء كل تنبيه على شكل شريط مستقل
    activeAlerts.forEach((alert, index) => {
        const popup = document.createElement("div");
        popup.className = "alert-popup";
        popup.style.bottom = `${10 + index * 60}px`;

        popup.innerHTML = `
            <div class="alert-text">
                <h4><i class="fas fa-fire-alt text-danger"></i> Fire detected</h4>
                <span><b>Temp:</b> ${alert.temperature}°C</span>
                <span><b>Smoke:</b> ${alert.smoke}</span>
                <span><b>Humidity:</b> ${alert.humidity}</span>
                <span><b>Fire Score:</b> ${Math.round(alert.fireScore ?? 0)}</span>
                <span><b>Duration:</b> ${alert.duration ?? "-"}</span>
                <span><b>Location:</b> (${alert.latitude}, ${alert.longitude})</span>
            </div>
            <div class="alert-actions">
                <button onclick="clearAlert('${alert.id}')" class="btn btn-secondary me-2">Clear</button>
                <button onclick='zoomToSensorFromAlert(
                    ${alert.latitude}, 
                    ${alert.longitude}, 
                    "${alert.sensorId}", 
                    ${JSON.stringify(alert).replace(/"/g, '&quot;')}
                )' class="btn btn-primary me-2">Zoom</button>
                <button onclick="acknowledge('${alert.id}')" class="btn btn-danger">Acknowledge</button>
            </div>
        `;

        document.body.appendChild(popup);
    });

    // تشغيل الصوت
    const sound = document.getElementById("fire-sound");
    if (sound) {
        sound.play().catch(err => {
            console.warn("🔇 Autoplay prevented:", err);
        });
    }
}


function clearAlert(alertId) {
    const index = activeAlerts.findIndex(a => a.id === alertId);
    if (index !== -1) {
        activeAlerts.splice(index, 1);
        renderAlerts();
    }

    // إذا لم يتبقَ أي إنذار، أخفِ الوميض وأوقف الصوت
    if (activeAlerts.length === 0) {
        const overlay = document.getElementById("alert-overlay");
        if (overlay) {
            overlay.classList.remove("blink");
            overlay.style.display = "none";
        }

        const sound = document.getElementById("fire-sound");
        if (sound) {
            sound.pause();
            sound.currentTime = 0;
        }
    }
}

function acknowledge(alertId) {
    console.log("Acknowledged alert:", alertId);
    alert("Alert acknowledged.");
    clearAlert(alertId);
}

function zoomToSensorFromAlert(latitude, longitude, sensorId, alert) {
    sessionStorage.setItem("pendingAlert", JSON.stringify(alert));
    sessionStorage.setItem("zoomTarget", JSON.stringify({ latitude, longitude, sensorId }));
    window.location.href = `/Map/Index`;
}

// ✅ إعادة عرض التنبيه المخزن عند التنقل بين الصفحات
const alertJson = sessionStorage.getItem("pendingAlert");
if (alertJson) {
    try {
        const alert = JSON.parse(alertJson);
        if (!alert.id) alert.id = alert.sensorId + "_" + Date.now();
        activeAlerts.push(alert);
        renderAlerts();
        sessionStorage.removeItem("pendingAlert");
    } catch (e) {
        console.warn("Failed to parse stored alert:", e);
    }
}
