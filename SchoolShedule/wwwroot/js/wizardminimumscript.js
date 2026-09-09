let templateId = window.templateIdValue ?? 0;
let originalName = window.originalTemplateName ?? "";

$(document).ready(function () {

    updateGenerateButtonState();

    if (templateId !== 0) {
        $("#btnGenerate").removeClass("d-none");
    }

    $(".hours-input, #period").on("input change", function () {
        $("#btnGenerate").addClass("d-none");
    });

    $(".class-select").on("change", updateGenerateButtonState);

    $("#btnSave").on("click", onSaveTemplate);
    $("#btnGenerate").on("click", onGenerate);
});

function updateGenerateButtonState() {
    const anyClassSelected = $(".class-select:checked").length > 0;

    if (templateId !== 0 && anyClassSelected) {
        $("#btnGenerate").removeClass("d-none");
    } else {
        $("#btnGenerate").addClass("d-none");
    }
}

function onSaveTemplate() {
    const period = parseInt($("#period").val());

    const cells = [];
    $(".hours-input").each(function () {
        const hours = parseInt($(this).val());
        if (!isNaN(hours) && hours > 0) {
            cells.push({
                subjectId: $(this).data("subject"),
                classGroupId: $(this).data("class"),
                hours: hours
            });
        }
    });

    openNameModal(originalName);

    $("#templateNameConfirmBtn").off("click").on("click", async function () {
        const name = $("#templateNameInput").val().trim();
        if (!name) {
            $("#templateNameError").text("Введите имя шаблона.").show();
            return;
        }

        const check = await checkTemplateName(name);

        if (check.status === "OK") {
            submitTemplate(name, period, cells, false);
        }
        else if (check.status === "NAME_EXISTS") {
            $("#templateNameError").html(`
                Шаблон с именем <b>${name}</b> уже существует.<br>
                Хотите <b>заменить</b> его или <b>сохранить как новый</b>?
                <div class="mt-2">
                    <button class="btn btn-warning btn-sm me-2" id="replaceBtn">Заменить</button>
                    <button class="btn btn-secondary btn-sm" id="newNameBtn">Новое имя</button>
                </div>
            `).show();

            $("#replaceBtn").on("click", function () {
                submitTemplate(name, period, cells, true);
            });

            $("#newNameBtn").on("click", function () {
                $("#templateNameInput").val("");
                $("#templateNameError").hide();
            });
        }
        else {
            alert("Неизвестный ответ сервера.");
        }
    });
}

function openNameModal(initialName) {
    $("#templateNameInput").val(initialName);
    $("#templateNameError").hide();
    const modal = new bootstrap.Modal(document.getElementById("templateNameModal"));
    modal.show();
}

function checkTemplateName(name) {
    return $.ajax({
        url: '/ScheduleGenerator/CheckTemplateName',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            name: name,
            templateId: templateId
        })
    });
}

function submitTemplate(name, period, cells, replace) {
    const data = {
        name: name,
        periodType: period,
        cells: cells,
        replaceExisting: replace,
        templateId: templateId
    };

    $.ajax({
        url: '/ScheduleGenerator/WizardTemplateMinimumSave',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (resp) {
            templateId = resp.templateId;
            originalName = name;

            $("#btnGenerate").removeClass("d-none");
            updateGenerateButtonState();

            const modal = bootstrap.Modal.getInstance(document.getElementById("templateNameModal"));
            modal.hide();

            alert("Шаблон сохранён.");
        }
    });
}

function onGenerate() {

    const manualMode = $("#manualModeCheckbox").is(":checked");

    const selectedClasses = $(".class-select:checked")
        .map(function () { return $(this).data("class-id"); })
        .get();

    if (manualMode) {
        // Создаём форму для POST
        const form = document.createElement("form");
        form.method = "POST";
        form.action = "/ScheduleGenerator/BuildManualSchedule";
        form.target = "_blank"; // новая вкладка

        // templateId — обычное поле
        const templateInput = document.createElement("input");
        templateInput.type = "hidden";
        templateInput.name = "templateId";
        templateInput.value = templateId;
        form.appendChild(templateInput);

        // selectedIds — массив, как ожидает ASP.NET Core
        selectedClasses.forEach(id => {
            const input = document.createElement("input");
            input.type = "hidden";
            input.name = "selectedIds"; // важно: без []
            input.value = id;
            form.appendChild(input);
        });

        document.body.appendChild(form);
        form.submit();

        return;

    }
    else {
        $.ajax({
            url: '/ScheduleGenerator/WizardTemplateMinimumGenerate',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                templateId: templateId,
                selectedIds: selectedClasses
            }),
            success: function () {
                alert("Расписание сгенерировано.");
            },
            error: function (xhr) {
                let msg = "Ошибка генерации расписания.";

                if (xhr.responseJSON && xhr.responseJSON.error) {
                    msg = xhr.responseJSON.error;
                }

                alert(msg);
            }
        });
    }
}
