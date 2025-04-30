const connection = new signalR.HubConnectionBuilder()
    .withUrl("/alertHub")
    .build();

connection.on("NewAlert", function (alert) {
    showAlert(alert);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

function showAlert(alert) {
    const sound = document.getElementById("fire-sound");
    if (sound) {
        sound.play().catch(err => {
            console.warn("Autoplay prevented:", err);
        });
    }

    let popup = document.getElementById("alert-popup");
    if (!popup) {
        popup = document.createElement("div");
        popup.id = "alert-popup";
        popup.className = "alert-popup";
        document.body.appendChild(popup);
    }

    popup.innerHTML = `
        <div class="alert-text">
            <h4><i class="fas fa-fire-alt text-danger"></i> Fire detected</h4>
            <span><b>Temp:</b> ${alert.temperature}°C</span>
            <span><b>Smoke:</b> ${alert.smoke}</span>
            <span><b>Humidity:</b> ${alert.humidity}</span>
            <span><b>Location:</b> (${alert.latitude}, ${alert.longitude})</span>
        </div>
        <div class="alert-actions">
            <button onclick="clearAlert()" class="btn btn-secondary me-2">Clear Alert</button>
            <button onclick='zoomToSensorFromAlert(
                ${alert.latitude}, 
                ${alert.longitude}, 
                "${alert.sensorId}", 
                ${JSON.stringify(alert).replace(/"/g, '&quot;')}
            )' class="btn btn-primary me-2">Zoom to Sensor
            </button>
            <button onclick="acknowledge('${alert.id}')" class="btn btn-danger">Acknowledge Alert</button>
        </div>
    `;

    popup.classList.add("show");

    let overlay = document.getElementById("alert-overlay");
    if (!overlay) {
        overlay = document.createElement("div");
        overlay.id = "alert-overlay";
        document.body.appendChild(overlay);
    }
    overlay.classList.add("blink");
    overlay.style.display = "block";
}

function clearAlert() {
    const popup = document.getElementById("alert-popup");
    if (popup) popup.classList.remove("show");

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

function acknowledge(alertId) {
    console.log("Acknowledged alert:", alertId);
    alert("Alert acknowledged by user.");
    clearAlert();
}

function zoomToSensorFromAlert(latitude, longitude, sensorId, alert) {
    sessionStorage.setItem("pendingAlert", JSON.stringify(alert));
    sessionStorage.setItem("zoomTarget", JSON.stringify({ latitude, longitude, sensorId }));

    const url = `/Map/Index`;
    window.location.href = url;
}

// ✅ استرجاع الإنذار عند فتح صفحة جديدة (مثل /Map/Index)
const alertJson = sessionStorage.getItem("pendingAlert");
if (alertJson) {
    try {
        const alert = JSON.parse(alertJson);
        showAlert(alert);
        sessionStorage.removeItem("pendingAlert");
    } catch (e) {
        console.warn("❌ Failed to parse pending alert:", e);
    }
}
