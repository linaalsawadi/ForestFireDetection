document.addEventListener("DOMContentLoaded", function () {
    // Initialize the map at Sakarya University
    var map = L.map('map').setView([40.7411, 30.3331], 15);

    // Add OpenStreetMap layer
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    // Locations and chart data
    var locations = [
        { name: "Main Building", lat: 40.7411, lon: 30.3331, data: { temp: [22, 23, 24, 22, 21], humidity: [50, 52, 48, 47, 49], smoke: [10, 12, 15, 11, 14] } },
        { name: "Central Library", lat: 40.7420, lon: 30.3340, data: { temp: [21, 22, 23, 21, 20], humidity: [55, 54, 53, 52, 51], smoke: [9, 11, 14, 12, 10] } },
        { name: "Science Laboratories", lat: 40.7405, lon: 30.3325, data: { temp: [24, 25, 26, 24, 23], humidity: [45, 47, 44, 42, 46], smoke: [8, 10, 13, 9, 12] } }
    ];

    // Create markers and add charts on click
    locations.forEach(function (location) {
        var marker = L.marker([location.lat, location.lon]).addTo(map);
        marker.bindPopup(`<b>${location.name}</b><br><canvas id='chart-${location.name.replace(/\s+/g, '')}' width='250' height='200'></canvas>`);

        marker.on('popupopen', function () {
            setTimeout(() => {
                var canvasId = `chart-${location.name.replace(/\s+/g, '')}`;
                var canvas = document.getElementById(canvasId);
                if (!canvas) return;

                // Remove any previous charts
                canvas.outerHTML = `<canvas id='${canvasId}' width='250' height='200'></canvas>`;
                canvas = document.getElementById(canvasId);

                var ctx = canvas.getContext('2d');
                new Chart(ctx, {
                    type: 'line',
                    data: {
                        labels: ['1', '2', '3', '4', '5'],
                        datasets: [
                            {
                                label: 'Temperature (°C)',
                                data: location.data.temp,
                                borderColor: 'red',
                                fill: false
                            },
                            {
                                label: 'Humidity (%)',
                                data: location.data.humidity,
                                borderColor: 'blue',
                                fill: false
                            },
                            {
                                label: 'Smoke (PPM)',
                                data: location.data.smoke,
                                borderColor: 'gray',
                                fill: false
                            }
                        ]
                    },
                    options: {
                        responsive: false,
                        maintainAspectRatio: false
                    }
                });
            }, 100);
        });
    });
});
