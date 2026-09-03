let draggedSubject = null;
let currentCell = null;
let usedTeachers = new Set();

// DRAG START
document.addEventListener("dragstart", function (e) {
    const item = e.target.closest(".subject-item");
    if (!item) return;

    e.dataTransfer.effectAllowed = "move";
    e.dataTransfer.setData("text/plain", item.dataset.subjectId);

    draggedSubject = item;
});

// DRAG OVER
document.addEventListener("dragover", function (e) {
    const dz = e.target.closest(".dropzone");
    const cell = e.target.closest(".schedule-cell");

    if (!dz && !cell) return;

    e.preventDefault();
    e.dataTransfer.dropEffect = "move";
});

// DROP
document.addEventListener("drop", function (e) {
    const dz = e.target.closest(".dropzone");
    const cell = e.target.closest(".schedule-cell");

    if ((!dz && !cell) || !draggedSubject) return;

    e.preventDefault();

    currentCell = cell ?? dz.closest(".schedule-cell");

    $('#teacher-select').val(null).trigger('change');

    const modal = new bootstrap.Modal(document.getElementById("teacherModal"));
    modal.show();
});

// CONFIRM TEACHER
document.getElementById("teacher-select-confirm").addEventListener("click", function () {
    const teacherId = document.getElementById("teacher-select").value;

    const modal = bootstrap.Modal.getInstance(document.getElementById("teacherModal"));
    modal.hide();

    usedTeachers.add(teacherId);

    const subjectName = draggedSubject.dataset.subjectName;
    const teacherText = document.querySelector(`#teacher-select option[value="${teacherId}"]`).innerText;

    currentCell.querySelector(".cell-content").innerHTML = `
        <div class="p-1 bg-light border rounded">
            <strong>${subjectName}</strong><br/>
            <small>${teacherText}</small>
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
});

// SELECT2: подсветка и сортировка

function highlightTeachers(option) {
    if (!option.id) return option.text;

    const subjects = ($(option.element).data('subjects') || '')
        .toString()
        .split(',')
        .filter(x => x);

    const selectedSubjectId = draggedSubject
        ? draggedSubject.dataset.subjectId
        : null;

    const isProfile = selectedSubjectId && subjects.includes(selectedSubjectId);
    const isUsed = usedTeachers.has(option.id);

    let cls = '';
    if (isUsed) cls = 'bg-success text-white px-1';
    else if (!isProfile) cls = 'bg-warning px-1';

    if (!cls)
        return option.text;

    return $('<span class="' + cls + '">' + option.text + '</span>');
}

function teacherSorter(data) {
    if (!draggedSubject) return data;

    const subjectId = draggedSubject.dataset.subjectId;

    return data.sort((a, b) => {
        const aId = a.id;
        const bId = b.id;

        const aSubjects = ($(a.element).data('subjects') || '')
            .toString()
            .split(',')
            .filter(x => x);
        const bSubjects = ($(b.element).data('subjects') || '')
            .toString()
            .split(',')
            .filter(x => x);

        const aUsed = usedTeachers.has(aId);
        const bUsed = usedTeachers.has(bId);

        const aProfile = aSubjects.includes(subjectId);
        const bProfile = bSubjects.includes(subjectId);

        if (aUsed && !bUsed) return -1;
        if (!aUsed && bUsed) return 1;

        if (aProfile && !bProfile) return -1;
        if (!aProfile && bProfile) return 1;

        return a.text.localeCompare(b.text);
    });
}
