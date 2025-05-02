const charts = {};
const refreshIntervals = {};
let expandedSensorIds = new Set();

// تحميل الرسوم البيانية لحساس
function loadSensorCharts(sensorId, container) {
    fetch(`/Sensors/GetSensorData?sensorId=${sensorId}`)
        .then(res => res.json())
        .then(data => {
            container.innerHTML = `
                <div class="row">
                    <div class="col-md-4">
                        <canvas id="tempChart-${sensorId}" height="200"></canvas>
                    </div>
                    <div class="col-md-4">
                        <canvas id="humidityChart-${sensorId}" height="200"></canvas>
                    </div>
                    <div class="col-md-4">
                        <canvas id="smokeChart-${sensorId}" height="200"></canvas>
                    </div>
                </div>
            `;
            drawAllCharts(sensorId, data);
        })
        .catch(err => {
            console.error("Chart error:", err);
            container.innerHTML = `<p class="text-danger">Failed to load chart data.</p>`;
        });
}

// رسم جميع الرسوم البيانية
function drawAllCharts(sensorId, data) {
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

// رسم رسم بياني فردي
function renderLineChart(canvasId, label, data, color) {
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
                fill: false,
                tension: 0.3
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: true, position: 'top' }
            },
            scales: {
                y: { beginAtZero: true }
            }
        }
    });
}

// تحديث كامل للواجهة والرسوم البيانية
async function refreshDashboard() {
    const response = await fetch('/Sensors/GetSensors');
    const sensors = await response.json();
    const tbody = document.querySelector("#sensorTable tbody");

    // تحديث قائمة الحساسات المفتوحة
    expandedSensorIds = new Set();
    document.querySelectorAll('tr[aria-expanded="true"]').forEach(row => {
        const sensorIdCell = row.children[0];
        const sensorId = sensorIdCell?.textContent?.trim();
        if (sensorId) expandedSensorIds.add(sensorId);
    });

    tbody.innerHTML = '';

    sensors.forEach((sensor) => {
        const isOpen = expandedSensorIds.has(sensor.sensorId);

        const row = document.createElement("tr");
        row.setAttribute("data-widget", "expandable-table");
        row.setAttribute("aria-expanded", isOpen ? "true" : "false");

        row.innerHTML = `
            
            <td>${sensor.sensorId}</td>
            <td>
                ${sensor.sensorState === "red" ? '<span class="badge bg-danger">Critical</span>' :
                sensor.sensorState === "yellow" ? '<span class="badge bg-warning text-dark">Warning</span>' :
                    '<span class="badge bg-success rounded-pill">Normal</span>'}
            </td>
            <td>${new Date(sensor.sensorPositioningDate).toISOString().split('T')[0]}</td>
            <td>${sensor.sensorDangerSituation ? "Yes" : "No"}</td>
        `;

        const expandable = document.createElement("tr");
        expandable.className = isOpen ? "expandable-body" : "expandable-body d-none";
        expandable.innerHTML = `
            <td colspan="5">
                <div id="charts-container-${sensor.sensorId}" class="p-2 bg-light rounded shadow-sm">
                    <div class="text-center py-3">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Loading...</span>
                        </div>
                        <p class="text-muted mt-2">Loading sensor data...</p>
                    </div>
                </div>
            </td>
        `;

        tbody.appendChild(row);
        tbody.appendChild(expandable);

        // إذا كان الحساس مفتوح، نعيد تحميل بياناته
        if (isOpen) {
            const container = expandable.querySelector('[id^="charts-container-"]');
            loadSensorCharts(sensor.sensorId, container);
            container.dataset.loaded = "true";
        }
    });

    attachExpandableEvents();
}

// ربط أحداث التوسيع
function attachExpandableEvents() {
    const rows = document.querySelectorAll('tr[data-widget="expandable-table"]');

    rows.forEach(row => {
        row.addEventListener('click', () => {
            const nextRow = row.nextElementSibling;
            const container = nextRow.querySelector('[id^="charts-container-"]');

            const sensorId = container.id.replace("charts-container-", "");

            if (!container.dataset.loaded) {
                loadSensorCharts(sensorId, container);
                container.dataset.loaded = "true";
            }

            // أضف الحساس إلى قائمة المفتوحين
            expandedSensorIds.add(sensorId);
        });
    });
}

// البحث
document.addEventListener('DOMContentLoaded', () => {
    refreshDashboard();
    setInterval(refreshDashboard, 30000);

    document.getElementById('sensorSearch')?.addEventListener('input', function () {
        const value = this.value.toLowerCase();
        const rows = document.querySelectorAll('tbody tr[data-widget="expandable-table"]');

        rows.forEach(row => {
            const sensorId = row.children[0].innerText.toLowerCase();
            const status = row.children[1].innerText.toLowerCase();
            row.style.display = (sensorId.includes(value) || status.includes(value)) ? '' : 'none';

            const nextRow = row.nextElementSibling;
            if (nextRow && nextRow.classList.contains('expandable-body')) {
                nextRow.style.display = row.style.display;
            }
        });
    });
});
