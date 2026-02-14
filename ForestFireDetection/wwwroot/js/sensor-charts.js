// ═══════════════════════════════════════════
// GREEN SHIELD — Sensors Page Charts + SignalR
// ═══════════════════════════════════════════

const charts = {};
let expandedSensorIds = new Set();
const MAX_CHART_POINTS = 15;

function loadSensorCharts(sensorId, container) {
    fetch(`/Sensors/GetSensorData?sensorId=${sensorId}`)
        .then(res => res.json())
        .then(data => {
            // Take only the last MAX_CHART_POINTS entries
            const sliced = data.slice(-MAX_CHART_POINTS);
            const labels = sliced.map(d => formatTime(d.timestamp));

            container.innerHTML = `
                <div class="row g-3">
                    <div class="col-md-4">
                        <div class="gs-panel-chart-container gs-chart-light">
                            <div class="gs-panel-chart-label">
                                <i class="fas fa-thermometer-half" style="color:var(--gs-red)"></i> Temperature (°C)
                            </div>
                            <div class="gs-chart-wrapper"><canvas id="tempChart-${sensorId}"></canvas></div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="gs-panel-chart-container gs-chart-light">
                            <div class="gs-panel-chart-label">
                                <i class="fas fa-tint" style="color:var(--gs-blue)"></i> Humidity (%)
                            </div>
                            <div class="gs-chart-wrapper"><canvas id="humidityChart-${sensorId}"></canvas></div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="gs-panel-chart-container gs-chart-light">
                            <div class="gs-panel-chart-label">
                                <i class="fas fa-smog" style="color:var(--gs-yellow)"></i> Smoke
                            </div>
                            <div class="gs-chart-wrapper"><canvas id="smokeChart-${sensorId}"></canvas></div>
                        </div>
                    </div>
                </div>`;

            renderSChart(`tempChart-${sensorId}`, labels, sliced.map(d => d.temperature), "#ef4444", "rgba(239,68,68,.08)");
            renderSChart(`humidityChart-${sensorId}`, labels, sliced.map(d => d.humidity), "#3b82f6", "rgba(59,130,246,.08)");
            renderSChart(`smokeChart-${sensorId}`, labels, sliced.map(d => d.smoke), "#f59e0b", "rgba(245,158,11,.08)");
        })
        .catch(() => {
            container.innerHTML = `<p style="color:var(--gs-red);text-align:center;padding:20px;">Failed to load data.</p>`;
        });
}

function formatTime(ts) {
    const d = new Date(ts);
    return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function renderSChart(canvasId, labels, data, color, bg) {
    const ctx = document.getElementById(canvasId)?.getContext('2d');
    if (!ctx) return;
    if (charts[canvasId]) charts[canvasId].destroy();

    charts[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                borderColor: color,
                backgroundColor: bg,
                fill: true,
                tension: 0.4,
                borderWidth: 2,
                pointRadius: 3,
                pointHoverRadius: 5,
                pointBackgroundColor: '#fff',
                pointBorderColor: color,
                pointBorderWidth: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 400 },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: '#111827',
                    titleFont: { size: 11, weight: '600' },
                    bodyFont: { size: 11 },
                    cornerRadius: 8,
                    padding: 8,
                    displayColors: false
                }
            },
            scales: {
                x: {
                    ticks: {
                        maxTicksLimit: 5,
                        maxRotation: 0,
                        autoSkip: true,
                        font: { size: 10 },
                        color: '#9ca3af'
                    },
                    grid: { display: false }
                },
                y: {
                    beginAtZero: true,
                    ticks: {
                        maxTicksLimit: 5,
                        font: { size: 10 },
                        color: '#9ca3af'
                    },
                    grid: { color: '#f3f4f6' }
                }
            }
        }
    });
}

// ── Trim chart to MAX_CHART_POINTS ──
function pushToSensorChart(chartId, label, value) {
    const chart = charts[chartId];
    if (!chart) return;

    chart.data.labels.push(label);
    chart.data.datasets[0].data.push(value);

    // Always keep only the last MAX_CHART_POINTS
    while (chart.data.labels.length > MAX_CHART_POINTS) {
        chart.data.labels.shift();
        chart.data.datasets[0].data.shift();
    }

    chart.update('none');
}

// ── Toggle expand/collapse ──
function attachExpandableEvents() {
    document.querySelectorAll('.sensor-row').forEach(row => {
        const newRow = row.cloneNode(true);
        row.parentNode.replaceChild(newRow, row);

        newRow.addEventListener('click', function () {
            const sid = this.getAttribute('data-sensor-id');
            const cr = document.querySelector(`.sensor-charts-row[data-sensor-id="${sid}"]`);
            if (!cr) return;

            const chevron = this.querySelector('.fa-chevron-down, .fa-chevron-up');

            if (cr.classList.contains('d-none')) {
                cr.classList.remove('d-none');
                expandedSensorIds.add(sid);
                if (chevron) { chevron.classList.remove('fa-chevron-down'); chevron.classList.add('fa-chevron-up'); }

                const c = cr.querySelector(`#charts-container-${sid}`);
                if (c && !c.dataset.loaded) {
                    loadSensorCharts(sid, c);
                    c.dataset.loaded = "true";
                }
            } else {
                cr.classList.add('d-none');
                expandedSensorIds.delete(sid);
                if (chevron) { chevron.classList.remove('fa-chevron-up'); chevron.classList.add('fa-chevron-down'); }
            }
        });
    });
}

// ═══ SignalR ═══
const chartHubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/chartHub")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

chartHubConnection.serverTimeoutInMilliseconds = 10 * 60 * 1000;
chartHubConnection.keepAliveIntervalInMilliseconds = 30 * 1000;

chartHubConnection.on("ReceiveSensorData", function (sensorId, lp, state, danger, tG, tY, tR, tO, pd) {
    // ── Update counters ──
    const counters = { "count-green": tG, "count-yellow": tY, "count-red": tR, "count-offline": tO };
    Object.entries(counters).forEach(([id, val]) => {
        const el = document.getElementById(id);
        if (el) el.textContent = val;
    });

    // ── Update table row ──
    const row = document.querySelector(`.sensor-row[data-sensor-id="${sensorId}"]`);
    if (row) {
        const cells = row.children;

        // Status badge
        const states = {
            offline: ['bg-secondary', 'Offline'],
            red: ['bg-danger', 'Critical'],
            yellow: ['bg-warning text-dark', 'Warning'],
            green: ['bg-success', 'Normal']
        };
        const [cls, label] = states[state] || states.green;
        cells[1].innerHTML = `<span class="badge ${cls} rounded-pill">${label}</span>`;

        // Sensor dot color
        const dot = cells[0].querySelector('.gs-sensor-dot');
        if (dot) dot.className = `gs-sensor-dot gs-dot-${state}`;

        // Fire score
        const s = lp.fireScore;
        if (s != null) {
            const c = s >= 75 ? 'var(--gs-red)' : s >= 50 ? 'var(--gs-yellow)' : 'var(--gs-green)';
            cells[2].innerHTML = `<span class="fw-bold" style="color:${c}">${s.toFixed(1)}%</span>`;
        }

        // Last update
        cells[3].innerHTML = `<small class="text-muted">${new Date(pd).toLocaleString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}</small>`;

        // Danger
        cells[4].innerHTML = danger
            ? `<span style="color:var(--gs-red);font-weight:600;"><i class="fas fa-exclamation-circle me-1"></i>Yes</span>`
            : `<span class="text-muted">No</span>`;
    }

    // ── Update live charts if expanded ──
    if (!expandedSensorIds.has(sensorId)) return;

    const time = formatTime(lp.timestamp);

    pushToSensorChart(`tempChart-${sensorId}`, time, lp.temperature);
    pushToSensorChart(`humidityChart-${sensorId}`, time, lp.humidity);
    pushToSensorChart(`smokeChart-${sensorId}`, time, lp.smoke);
});

chartHubConnection.on("KeepAlive", () => { });

chartHubConnection.start()
    .then(() => console.log("chartHub connected"))
    .catch(err => console.error("chartHub error:", err));

// ── Search ──
document.getElementById("sensorSearch")?.addEventListener("keyup", function () {
    const v = this.value.toLowerCase();
    document.querySelectorAll('.sensor-row').forEach(r => {
        const match = r.textContent.toLowerCase().includes(v);
        r.style.display = match ? "" : "none";
        const sid = r.getAttribute('data-sensor-id');
        const cr = document.querySelector(`.sensor-charts-row[data-sensor-id="${sid}"]`);
        if (cr && !match) cr.classList.add('d-none');
    });
});

// ── Init ──
document.addEventListener('DOMContentLoaded', attachExpandableEvents);