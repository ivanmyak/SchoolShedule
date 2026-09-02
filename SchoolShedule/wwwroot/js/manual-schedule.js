let draggedSubject = null;
let currentCell = null;

// === DRAG START ===
document.addEventListener("dragstart", function (e) {
    const item = e.target.closest(".subject-item");
    if (!item) return;

    draggedSubject = item;
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

    const modal = new bootstrap.Modal(document.getElementById("teacherModal"));
    modal.show();
});

// === CONFIRM TEACHER ===
document.getElementById("teacher-select-confirm").addEventListener("click", function () {
    const teacherId = document.getElementById("teacher-select").value;

    const modal = bootstrap.Modal.getInstance(document.getElementById("teacherModal"));
    modal.hide();

    currentCell.dataset.teacherId = teacherId;

    const roomModal = new bootstrap.Modal(document.getElementById("roomModal"));
    roomModal.show();
});

// === CONFIRM ROOM ===
document.getElementById("room-select-confirm").addEventListener("click", function () {
    const roomId = document.getElementById("room-select").value;

    const modal = bootstrap.Modal.getInstance(document.getElementById("roomModal"));
    modal.hide();

    currentCell.dataset.roomId = roomId;

    const subjectName = draggedSubject.dataset.subjectName;

    currentCell.querySelector(".cell-content").innerHTML = `
        <div class="p-1 bg-light border rounded">
            <strong>${subjectName}</strong><br/>
            <small>Преп.: ${document.querySelector(`#teacher-select option[value="${currentCell.dataset.teacherId}"]`).innerText}</small><br/>
            <small>Каб.: ${document.querySelector(`#room-select option[value="${roomId}"]`).innerText}</small>
        </div>
    `;

    let hours = parseInt(draggedSubject.dataset.hours);
    hours -= 1;
    draggedSubject.dataset.hours = hours;
    draggedSubject.querySelector(".badge").innerText = hours;

    if (hours <= 0) {
        draggedSubject.setAttribute("draggable", "false");
        draggedSubject.classList.add("disabled");
    }

    draggedSubject = null;

    checkClassFilled();
});

// === Проверка заполненности текущего класса ===
function checkClassFilled() {
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
            subjectName: content.querySelector("strong").innerText,
            teacherId: cell.dataset.teacherId,
            roomId: cell.dataset.roomId
        });
    });

    const currentModel = window.manualModels[window.manualIndex];

    currentModel.FilledLessons = lessons;

    const allFilled = window.manualModels.every(m => m.FilledLessons && m.FilledLessons.length > 0);

    if (!allFilled) {
        if (!confirm(`Вы пытаетесь сохранить расписание только для класса ${currentModel.ClassGroup.Name}. Продолжить?`)) {
            return;
        }
    } else {
        alert("Все классы заполнены. Сохраняем расписание всех классов.");
    }

    $.ajax({
        url: '/ScheduleGenerator/SaveManualSchedule',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(window.manualModels),
        success: function () {
            alert("Расписание сохранено!");
        },
        error: function () {
            alert("Ошибка сохранения расписания.");
        }
    });
});
