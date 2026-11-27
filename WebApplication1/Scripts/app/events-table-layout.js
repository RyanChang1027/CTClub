var App = App || {};
App.Events = App.Events || {};

App.Events.tableLayout = (function (window, document) {
    "use strict";

    /* ============================================================
       1. Configuration
       ============================================================ */

    var canvas;
    var emptyState;
    var tableButtons;
    var deleteArea;
    var configPanel;
    var configNameInput;
    var configCapacitySelect;
    var seatAssignmentGrid;

    var tableCount = 0;
    var draggedTableId = null;
    var draggedShape = null;
    var selectedTable = null;

    /* ============================================================
       2. Initialization
       ============================================================ */

    function init() {
        canvas = document.getElementById("table-layout-canvas");
        emptyState = document.getElementById("table-layout-empty-state");
        deleteArea = document.getElementById("table-layout-delete-area");
        configPanel = document.getElementById("table-layout-config-panel");
        configNameInput = document.getElementById("table-config-name");
        configCapacitySelect = document.getElementById("table-config-capacity");
        seatAssignmentGrid = document.getElementById("table-layout-seat-grid");

        if (!canvas || !emptyState) {
            return;
        }

        tableButtons = document.querySelectorAll(
            ".table-layout-element[data-table-shape]"
        );

        bindTableButtons();
        bindCanvas();
        bindDeleteArea();
        bindConfigPanel();
    }

    /* ============================================================
       3. Table Element Dragging
       ============================================================ */

    function bindTableButtons() {
        Array.prototype.forEach.call(tableButtons, function (button) {
            button.setAttribute("draggable", "true");

            button.addEventListener("dragstart", function (event) {
                draggedShape = button.getAttribute("data-table-shape");
                draggedTableId = null;

                event.dataTransfer.setData(
                    "text/plain",
                    "new:" + draggedShape
                );

                event.dataTransfer.effectAllowed = "copy";
            });
        });
    }

    /* ============================================================
       4. Canvas Dragging & Dropping
       ============================================================ */

    function bindCanvas() {
        canvas.addEventListener("dragstart", function (event) {
            var table = findParentTable(event.target);

            if (!table) {
                return;
            }

            draggedTableId = table.id;
            draggedShape = null;

            event.dataTransfer.setData(
                "text/plain",
                "move:" + table.id
            );

            event.dataTransfer.effectAllowed = "move";
        });

        canvas.addEventListener("dragover", function (event) {
            event.preventDefault();
            event.dataTransfer.dropEffect =
                draggedTableId ? "move" : "copy";
        });

        canvas.addEventListener("drop", function (event) {
            event.preventDefault();

            var dropData = event.dataTransfer.getData("text/plain");

            if (!dropData) {
                return;
            }

            var position = getDropPosition(event);

            if (dropData.indexOf("new:") === 0) {
                var shape = dropData.substring(4);

                if (!isValidShape(shape)) {
                    return;
                }

                var table = createTable(shape);

                moveTable(
                    table,
                    position.left - (table.offsetWidth / 2),
                    position.top - (table.offsetHeight / 2)
                );
            }
            else if (dropData.indexOf("move:") === 0) {
                var tableId = dropData.substring(5);
                var existingTable = document.getElementById(tableId);

                if (existingTable && canvas.contains(existingTable)) {
                    moveTable(
                        existingTable,
                        position.left - (existingTable.offsetWidth / 2),
                        position.top - (existingTable.offsetHeight / 2)
                    );
                }
            }

            draggedTableId = null;
            draggedShape = null;
        });
    }

    /* ============================================================
       5. Table Creation
       ============================================================ */

    function createTable(shape) {
        var nextTableNumber = 1;

        while (document.getElementById("table-layout-item-" + nextTableNumber)) {
            nextTableNumber++;
        }

        tableCount = nextTableNumber;

        var table = document.createElement("div");

        table.id = "table-layout-item-" + tableCount;
        table.className =
            "table-layout-table table-layout-table--" + shape;

        table.setAttribute("draggable", "true");
        table.setAttribute("data-table-shape", shape);
        table.setAttribute("data-capacity", "12");

        var label = document.createElement("span");
        label.textContent = "Table " + tableCount;

        table.appendChild(label);
        renderTableSeats(table, 12);
        table.addEventListener("click", function () {
            selectTable(table);
        });
        canvas.appendChild(table);

        emptyState.style.display = "none";

        return table;
    }

    function renderTableSeats(table, capacity) {
        // Remove the previous chairs.
        var existingSeats = table.querySelectorAll(".table-layout-seat");

        Array.prototype.forEach.call(existingSeats, function (seat) {
            seat.remove();
        });

        var tableSize = 110;
        var center = tableSize / 2;
        var radius = 78;

        for (var i = 0; i < capacity; i++) {
            var angle = -Math.PI / 2 + (i * 2 * Math.PI / capacity);

            var seat = document.createElement("div");

            seat.className = "table-layout-seat";
            seat.setAttribute("data-seat-number", i + 1);

            var x = center + radius * Math.cos(angle);
            var y = center + radius * Math.sin(angle);

            seat.style.left = x + "px";
            seat.style.top = y + "px";

            table.appendChild(seat);
        }
    }

    function renderSeatAssignmentGrid(capacity) {
        seatAssignmentGrid.innerHTML = "";

        for (var i = 1; i <= capacity; i++) {
            var seat = document.createElement("div");

            seat.className = "table-layout-assignment-seat";
            seat.setAttribute("data-seat-number", i);
            seat.textContent = "Available Seat";

            seatAssignmentGrid.appendChild(seat);
        }
    }

    function isValidShape(shape) {
        return shape === "round" ||
            shape === "rectangle" ||
            shape === "oval";
    }

    function selectTable(table) {
        if (selectedTable) {
            selectedTable.classList.remove("is-selected");
        }

        selectedTable = table;
        selectedTable.classList.add("is-selected");

        // Load the selected table's current settings.
        configNameInput.value = table.querySelector("span").textContent;
        configCapacitySelect.value = table.getAttribute("data-capacity");

        renderSeatAssignmentGrid(
            parseInt(table.getAttribute("data-capacity"), 10)
        );

        // Show the configuration panel.
        configPanel.hidden = false;

        positionConfigPanel(table);
    }

    function bindConfigPanel() {

        // Update the table name while typing.
        configNameInput.addEventListener("input", function () {
            if (!selectedTable) {
                return;
            }

            var label = selectedTable.querySelector("span");

            if (label) {
                label.textContent =
                    configNameInput.value.trim() || "Untitled Table";
            }
        });

        // Save the selected capacity for the current table.
        configCapacitySelect.addEventListener("change", function () {
            if (!selectedTable) {
                return;
            }

            var capacity = parseInt(configCapacitySelect.value, 10);

            selectedTable.setAttribute("data-capacity", capacity);

            renderTableSeats(selectedTable, capacity);
            renderSeatAssignmentGrid(capacity);
        });

        // Close the panel when clicking outside the table and panel.
        document.addEventListener("click", function (event) {

            if (!selectedTable) {
                return;
            }

            // Keep the panel open when clicking the selected table.
            if (selectedTable.contains(event.target)) {
                return;
            }

            // Keep the panel open when interacting with its controls.
            if (configPanel.contains(event.target)) {
                return;
            }

            selectedTable.classList.remove("is-selected");
            selectedTable = null;
            configPanel.hidden = true;
        });
    }

    function positionConfigPanel(table) {
        var gap = 14;

        var tableLeft = table.offsetLeft;
        var tableTop = table.offsetTop;
        var tableRight = tableLeft + table.offsetWidth;

        var panelWidth = configPanel.offsetWidth;
        var panelHeight = configPanel.offsetHeight;

        var left;

        // Prefer the right side of the selected table.
        if (tableRight + gap + panelWidth <= canvas.clientWidth) {
            left = tableRight + gap;
        }
        // Otherwise, display the panel on the left.
        else {
            left = tableLeft - panelWidth - gap;
        }

        // Keep the panel inside the canvas.
        left = Math.max(
            0,
            Math.min(left, canvas.clientWidth - panelWidth)
        );

        var top = tableTop + (table.offsetHeight - panelHeight) / 2;

        top = Math.max(
            0,
            Math.min(top, canvas.clientHeight - panelHeight)
        );

        configPanel.style.left = left + "px";
        configPanel.style.top = top + "px";
    }

    /* ============================================================
       6. Table Positioning
       ============================================================ */

    function getDropPosition(event) {
        var bounds = canvas.getBoundingClientRect();

        return {
            left: event.clientX - bounds.left,
            top: event.clientY - bounds.top
        };
    }

    function moveTable(table, left, top) {
        var maxLeft = canvas.clientWidth - table.offsetWidth;
        var maxTop = canvas.clientHeight - table.offsetHeight;

        left = Math.max(0, Math.min(left, maxLeft));
        top = Math.max(0, Math.min(top, maxTop));

        table.style.left = left + "px";
        table.style.top = top + "px";
    }

    /* ============================================================
   7. Delete Area
   ============================================================ */

    function bindDeleteArea() {

        deleteArea.addEventListener("dragover", function (event) {
            if (!draggedTableId) {
                return;
            }

            event.preventDefault();
            event.dataTransfer.dropEffect = "move";

            deleteArea.classList.add("is-drag-over");
        });

        deleteArea.addEventListener("dragleave", function (event) {
            if (!deleteArea.contains(event.relatedTarget)) {
                deleteArea.classList.remove("is-drag-over");
            }
        });

        deleteArea.addEventListener("drop", function (event) {
            event.preventDefault();

            deleteArea.classList.remove("is-drag-over");

            if (!draggedTableId) {
                return;
            }

            var table = document.getElementById(draggedTableId);

            if (table && canvas.contains(table)) {
                table.remove();
            }

            draggedTableId = null;
            draggedShape = null;

            if (canvas.querySelectorAll(".table-layout-table").length === 0) {
                emptyState.style.display = "";
            }
        });
    }

    /* ============================================================
       7. Helpers
       ============================================================ */

    function findParentTable(element) {
        while (element && element !== canvas) {
            if (
                element.classList &&
                element.classList.contains("table-layout-table")
            ) {
                return element;
            }

            element = element.parentNode;
        }

        return null;
    }

    /* ============================================================
       8. Start
       ============================================================ */

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    return {
        init: init
    };

}(window, document));