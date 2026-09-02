let draggedSubject = null;
let draggedHours = 0;
let draggedSubjectId = 0;
let currentCell = null;

// === DRAG START ===
document.addEventListener("dragstart", function (e) {
    const item = e.target.closest(".subject-item");
    if (!item) return;

    draggedSubject = item;
    draggedSubjectId = item.dataset.subjectId;
    draggedHours = parseInt(item.dataset.hours);

    e.dataTransfer.effectAllowed = "move";
});

// === DRAG OVER ===
document.addEventListener("dragover", function (e) {
    if (e.target.closest(".dropzone")) {
        e.preventDefault();
    }
});

// === DROP ===
document.addEventListener("drop", function (e) {
    const dropzone = e.target.closest(".dropzone");
    if (!dropzone || !draggedSubject) return;

    e.preventDefault();
    currentCell = dropzone.closest(".schedule-cell");

    // Открываем модалку выбора преподавателя
    const modal = new bootstrap.Modal(document.getElementById("teacherModal"));
    modal.show();
});

// === CONFIRM TEACHER ===
document.getElementById("teacher-select-confirm").addEventListener("click", function () {
    const teacherId = document.getElementById("teacher-select").value;

    const modal = bootstrap.Modal.getInstance(document.getElementById("teacherModal"));
    modal.hide();

    // Открываем модалку выбора кабинета
    const roomModal = new bootstrap.Modal(document.getElementById("roomModal"));
    roomModal.show();

    // Сохраняем выбранного преподавателя
    currentCell.dataset.teacherId = teacherId;
});

// === CONFIRM ROOM ===
document.getElementById("room-select-confirm").addEventListener("click", function () {
    const roomId = document.getElementById("room-select").value;

    const modal = bootstrap.Modal.getInstance(document.getElementById("roomModal"));
    modal.hide();

    // Сохраняем кабинет
    currentCell.dataset.roomId = roomId;

    // Вставляем предмет в ячейку
    const subjectName = draggedSubject.querySelector("span").innerText;

    currentCell.querySelector(".cell-content").innerHTML = `
        <div class="p-1 bg-light border rounded">
            <strong>${subjectName}</strong><br/>
            <small>Преп.: ${document.querySelector(`#teacher-select option[value="${currentCell.dataset.teacherId}"]`).innerText}</small><br/>
            <small>Каб.: ${document.querySelector(`#room-select option[value="${roomId}"]`).innerText}</small>
        </div>
    `;

    // Уменьшаем количество часов
    draggedHours -= 1;
    draggedSubject.dataset.hours = draggedHours;
    draggedSubject.querySelector(".badge").innerText = draggedHours;

    // Если часов больше нет — блокируем предмет
    if (draggedHours <= 0) {
        draggedSubject.setAttribute("draggable", "false");
        draggedSubject.classList.add("disabled");
    }

    draggedSubject = null;

    // Проверяем, все ли предметы распределены
    checkAllSubjectsPlaced();
});

// === Проверка, все ли предметы распределены ===
function checkAllSubjectsPlaced() {
    const items = document.querySelectorAll(".subject-item");
    const allZero = Array.from(items).every(i => parseInt(i.dataset.hours) === 0);

    if (allZero) {
        document.getElementById("save-schedule-btn").removeAttribute("disabled");
    }
}

// === Сохранение расписания ===
document.getElementById("save-schedule-btn").addEventListener("click", function () {

    const lessons = [];

    document.querySelectorAll(".schedule-cell").forEach(cell => {
        const content = cell.querySelector(".cell-content div");
        if (!content) return;

        lessons.push({
            day: cell.dataset.day,
            period: cell.dataset.period,
            subjectId: content.querySelector("strong").innerText,
            teacherId: cell.dataset.teacherId,
            roomId: cell.dataset.roomId
        });
    });

    const payload = {
        classId: window.manualClassId,
        period: window.manualPeriod,
        lessons: lessons
    };

    $.ajax({
        url: '/ScheduleGenerator/SaveManualSchedule',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function () {
            alert("Расписание сохранено!");
        },
        error: function () {
            alert("Ошибка сохранения расписания.");
        }
    });
});
