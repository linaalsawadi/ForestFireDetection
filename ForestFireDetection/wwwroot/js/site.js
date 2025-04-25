const connection = new signalR.HubConnectionBuilder()
    .withUrl("/alarmHub")
    .build();

connection.on("NewAlarm", function (alarm) {
    // عرض التنبيه على الشاشة
    console.log("New Alarm Received:", alarm);

    // مثال: عرض نافذة تنبيه
    alert(`🚨 Fire Alert!\nLocation: ${alarm.location}\nTemperature: ${alarm.temperature}°C\nSmoke: ${alarm.smoke}`);
});

connection.start().catch(err => console.error(err));







