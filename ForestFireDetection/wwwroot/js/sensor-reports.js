let currentSensorId = '';
let selectedYear = new Date().getFullYear();
let selectedMonth = new Date().getMonth() + 1;

window.onload = async function () {
    await loadSensors();
    setupMonthBar();
    loadCalendar();
};

async function loadSensors() {
    const res = await fetch('/Sensors/GetSensors');
    const sensors = await res.json();
    const select = document.getElementById('sensorId');

    sensors.forEach(s => {
        const opt = document.createElement('option');
        opt.value = s.sensorId;
        opt.textContent = s.sensorId;
        select.appendChild(opt);
    });

    if (sensors.length > 0) {
        currentSensorId = sensors[0].sensorId;
        select.value = currentSensorId;
    }

    select.addEventListener('change', () => {
        currentSensorId = select.value;
        loadCalendar();
    });
}

function setupMonthBar() {
    const monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    const bar = document.getElementById('monthBar');

    for (let i = 1; i <= 12; i++) {
        const btn = document.createElement('button');
        btn.textContent = monthNames[i - 1];
        btn.classList.add('btn', 'btn-outline-primary', 'm-1');
        if (i === selectedMonth) btn.classList.add('active');

        btn.onclick = () => {
            selectedMonth = i;
            document.querySelectorAll('#monthBar button').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            loadCalendar();
        };

        bar.appendChild(btn);
    }
}

async function loadCalendar() {
    const calendar = document.getElementById('calendarDays');
    calendar.innerHTML = '';

    const year = selectedYear;
    const month = selectedMonth;
    const daysInMonth = new Date(year, month, 0).getDate();

    const res = await fetch(`/Sensors/GetMonthlyOverview?sensorId=${currentSensorId}&year=${year}&month=${month}`);
    const data = await res.json();

    const summaryMap = {};
    data.forEach(entry => {
        const day = new Date(entry.date).getDate();
        summaryMap[day] = entry;
    });

    for (let day = 1; day <= daysInMonth; day++) {
        const box = document.createElement('div');
        box.classList.add('day-box');

        const content = summaryMap[day];
        box.innerHTML = `
            <strong>${day}</strong><br/>
            ${content ? `🌡️ ${content.avgTemperature.toFixed(1)}°` : ''}
        `;

        box.onclick = () => onDayClick(`${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`);
        calendar.appendChild(box);
    }
}

function onDayClick(day) {
    window.location.href = `/Sensors/DailyReport?sensorId=${currentSensorId}&date=${day}`;
}
