const connection = new signalR.HubConnectionBuilder()
    .withUrl("/alertHub")
    .build();

connection.on("NewAlarm", function (alert) {
    showAlert(alert);
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

function showAlert(alert) {
    // تشغيل صوت الإنذار
    const sound = document.getElementById("fire-sound");
    if (sound) {
        sound.play().catch(err => {
            console.warn("Autoplay prevented:", err);
        });
    }

    // إنشاء عنصر الإنذار إذا غير موجود
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
            <button onclick="zoomToSensor(${alert.latitude}, ${alert.longitude})" class="btn btn-primary me-2">Zoom to Sensor</button>
            <button onclick="acknowledge('${alert.id}')" class="btn btn-danger">Acknowledge Alert</button>
        </div>
    `;

    // عرض الإنذار
    popup.classList.add("show");

    // عرض الخلفية المظللة
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

    // إيقاف الصوت
    const sound = document.getElementById("fire-sound");
    if (sound) {
        sound.pause();
        sound.currentTime = 0;
    }
}

function acknowledge(alertId) {
    // فيك تبعت AJAX لو بدك تحدث الحالة من "NotReviewed" لـ "UnderReview"
    console.log("Acknowledged alert:", alertId);
    alert("Alert acknowledged by user.");

    clearAlert();
}
