// إنشاء الاتصال مع SignalR Hub
const connection = new signalR.HubConnectionBuilder().withUrl("/alertHub").build();

// الاستماع للتنبيهات
connection.on("ReceiveAlert", function (message) {
    alert(message);  // يمكنك استبدال هذا لعرض التنبيه بشكل مخصص
    showAlertOnPage(message); // هذه دالة يمكنك استخدامها لعرض التنبيه في واجهتك
});

// بدء الاتصال بـ SignalR
connection.start().catch(function (err) {
    return console.error(err.toString());
});

// دالة لعرض التنبيه على الصفحة
function showAlertOnPage(message) {
    // مثال على عرض التنبيه عبر نافذة أو أي مكان آخر في الصفحة
    console.log("Alert: " + message);
    // يمكنك إضافة كود لعرض نافذة التنبيه (مثل modal أو alert)
}
