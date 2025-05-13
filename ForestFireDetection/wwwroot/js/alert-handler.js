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

alertConnection.on("UpdateAlertCount", function (count) {
    const badge = document.getElementById("alert-count-badge");
    if (badge) {
        badge.textContent = count;
        badge.style.display = count > 0 ? "inline-block" : "none";
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
    fetch(`/Alerts/Acknowledge/${alertId}`, { method: "POST" })
        .then(res => {
            if (res.ok) {
                clearAlert(alertId);
                showStackedToast("✅ Fire alert acknowledged successfully!");
            } else {
                showStackedToast("❌ Failed to acknowledge alert.", "fa-times-circle", "bg-danger");
            }
        })
        .catch(err => {
            showStackedToast("Error acknowledging alert.", "fa-exclamation-circle", "bg-danger");
        });
}

function zoomToSensorFromAlert(latitude, longitude, sensorId, alert) {
    sessionStorage.setItem("pendingAlert", JSON.stringify(alert));
    sessionStorage.setItem("zoomTarget", JSON.stringify({ latitude, longitude, sensorId }));
    window.location.href = `/Map/Index`;
}

function showStackedToast(message, iconClass = "fa-check-circle", bg = "bg-success") {
    const toastArea = document.getElementById("toastArea");

    const toastId = `toast-${Date.now()}`;
    const toastHTML = `
        <div id="${toastId}" class="toast text-white ${bg}" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="3000">
            <div class="toast-header">
                <i class="fas ${iconClass} me-2 text-${bg.includes("danger") ? "danger" : "success"}"></i>
                <strong class="me-auto">Green Shield</strong>
                <small class="text-muted">just now</small>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;

    toastArea.insertAdjacentHTML("beforeend", toastHTML);

    const toastEl = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastEl, {
        delay: 3000,
        autohide: true
    });
    toast.show();

    // حذف التوست من DOM بعد اختفائه
    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
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

alertConnection.on("KeepAlive", (timestamp) => {
    console.log("KeepAlive from server:", timestamp);
});