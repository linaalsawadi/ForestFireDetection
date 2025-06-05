// ✅ تحديث المخططات والصفوف والعدادات بالـ SignalR
const charts = {};
let expandedSensorIds = new Set();

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

function drawAllCharts(sensorId, data) {
    renderLineChart(`tempChart-${sensorId}`, "Temperature (°C)", data.map(d => ({ timestamp: d.timestamp, value: d.temperature })), "rgba(255, 99, 132, 1)");
    renderLineChart(`humidityChart-${sensorId}`, "Humidity (%)", data.map(d => ({ timestamp: d.timestamp, value: d.humidity })), "rgba(54, 162, 235, 1)");
    renderLineChart(`smokeChart-${sensorId}`, "Smoke", data.map(d => ({ timestamp: d.timestamp, value: d.smoke })), "rgba(255, 206, 86, 1)");
}

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

const chartHubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/chartHub")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

chartHubConnection.serverTimeoutInMilliseconds = 10 * 60 * 1000;
chartHubConnection.keepAliveIntervalInMilliseconds = 30 * 1000;

chartHubConnection.on("ReceiveSensorData", function (sensorId, latestPoint, state, danger, totalGreen, totalYellow, totalRed, totalOffline, positioningData) {
    // ✅ تحديث العدادات
    document.getElementById("count-green").textContent = totalGreen;
    document.getElementById("count-yellow").textContent = totalYellow;
    document.getElementById("count-red").textContent = totalRed;
    document.getElementById("count-offline").textContent = totalOffline;

    const row = Array.from(document.querySelectorAll("#sensorTable tbody tr"))
        .find(tr => tr.children[0]?.textContent?.trim() === sensorId);

    if (row) {
        const statusCell = row.children[1];
        const fireScoreCell = row.children[2];
        const positioningDataCell = row.children[3];
        const dangerCell = row.children[4];

        statusCell.innerHTML =
            state === "offline" ? '<span class="badge bg-secondary rounded-pill">Offline</span>' :
            state === "red" ? '<span class="badge bg-danger rounded-pill">Critical</span>' :
            state === "yellow" ? '<span class="badge bg-warning text-dark  rounded-pill">Warning</span>' :
                        '<span class="badge bg-success rounded-pill">Normal</span>';

        const score = latestPoint.fireScore;
        if (score !== null && score !== undefined) {
            const rounded = score.toFixed(2);
            if (score >= 35) fireScoreCell.innerHTML = `<span class="fw-bold text-danger">${rounded}</span>`;
            else if (score >= 25) fireScoreCell.innerHTML = `<span class="fw-bold text-warning">${rounded}</span>`;
            else fireScoreCell.innerHTML = `<span class="fw-bold text-success">${rounded}</span>`;
        } else {
            fireScoreCell.innerHTML = '<span class="text-muted">N/A</span>';
        }

        positioningDataCell.textContent = new Date(positioningData).toLocaleString();
        dangerCell.textContent = danger ? "Yes" : "No";
    }

    if (!expandedSensorIds.has(sensorId)) return;

    const chartTypes = ["temp", "humidity", "smoke"];
    const values = {
        temp: latestPoint.temperature,
        humidity: latestPoint.humidity,
        smoke: latestPoint.smoke
    };

    chartTypes.forEach(type => {
        const chartId = `${type}Chart-${sensorId}`;
        const chart = charts[chartId];
        if (chart) {
            const label = new Date(latestPoint.timestamp).toLocaleTimeString();
            chart.data.labels.push(label);
            chart.data.datasets[0].data.push(values[type]);

            if (chart.data.labels.length > 10) {
                chart.data.labels.shift();
                chart.data.datasets[0].data.shift();
            }

            chart.update();
        }
    });
});

chartHubConnection.start()
    .then(() => console.log("Connected to chartHub"))
    .catch(err => console.error("chartHub connection failed:", err));

function attachExpandableEvents() {
    document.querySelectorAll('tr[data-widget="expandable-table"]').forEach(row => {
        row.addEventListener('click', () => {
            const nextRow = row.nextElementSibling;
            const container = nextRow.querySelector('[id^="charts-container-"]');
            const sensorId = container?.id.replace("charts-container-", "");
            if (sensorId && !container.dataset.loaded) {
                loadSensorCharts(sensorId, container);
                container.dataset.loaded = "true";
            }
            expandedSensorIds.add(sensorId);
        });
    });
}

refreshDashboard();

function refreshDashboard() {
    fetch('/Sensors/GetSensors')
        .then(res => res.json())
        .then(sensors => {
            const tbody = document.querySelector("#sensorTable tbody");
            expandedSensorIds = new Set();

            document.querySelectorAll('tr[aria-expanded="true"]').forEach(row => {
                const sensorId = row.children[0]?.textContent?.trim();
                if (sensorId) expandedSensorIds.add(sensorId);
            });

            tbody.innerHTML = '';

            sensors.forEach(sensor => {
                const isOpen = expandedSensorIds.has(sensor.sensorId);
                const fireScore = sensor.fireScore;
                let fireScoreHtml = '<span class="text-muted">N/A</span>';

                if (fireScore !== null && fireScore !== undefined) {
                    const scoreValue = fireScore.toFixed(2);
                    if (fireScore >= 35) fireScoreHtml = `<span class="fw-bold text-danger">${scoreValue}</span>`;
                    else if (fireScore >= 25) fireScoreHtml = `<span class="fw-bold text-warning">${scoreValue}</span>`;
                    else fireScoreHtml = `<span class="fw-bold text-success">${scoreValue}</span>`;
                }

                const row = document.createElement("tr");
                row.setAttribute("data-widget", "expandable-table");
                row.setAttribute("aria-expanded", isOpen ? "true" : "false");

                row.innerHTML = `
                    <td>${sensor.sensorId}</td>
                    <td>
                        ${sensor.sensorState === "offline" ? '<span class="badge bg-secondary rounded-pill">Offline</span>' :
                        sensor.sensorState === "red" ? '<span class="badge bg-danger rounded-pill">Critical</span>' :
                        sensor.sensorState === "yellow" ? '<span class="badge bg-warning text-dark rounded-pill">Warning</span>' :
                            '<span class="badge bg-success rounded-pill">Normal</span>'}
                    </td>
                    <td>${fireScoreHtml}</td>
                    <td>${new Date(sensor.sensorPositioningDate).toLocaleString()}</td>
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

                if (isOpen) {
                    const container = expandable.querySelector('[id^="charts-container-"]');
                    if (container) {
                        loadSensorCharts(sensor.sensorId, container);
                        container.dataset.loaded = "true";
                    }
                }
            });

            attachExpandableEvents();
        });
}

chartHubConnection.on("KeepAlive", (timestamp) => {
    console.log("KeepAlive from server:", timestamp);
});