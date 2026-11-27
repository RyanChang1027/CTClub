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

    var tableCount = 0;
    var draggedTableId = null;
    var draggedShape = null;

    /* ============================================================
       2. Initialization
       ============================================================ */

    function init() {
        canvas = document.getElementById("table-layout-canvas");
        emptyState = document.getElementById("table-layout-empty-state");
        deleteArea = document.getElementById("table-layout-delete-area");

        if (!canvas || !emptyState) {
            return;
        }

        tableButtons = document.querySelectorAll(
            ".table-layout-element[data-table-shape]"
        );

        bindTableButtons();
        bindCanvas();
        bindDeleteArea();
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

                moveTable(table, position.left, position.top);
            }
            else if (dropData.indexOf("move:") === 0) {
                var tableId = dropData.substring(5);
                var existingTable = document.getElementById(tableId);

                if (existingTable && canvas.contains(existingTable)) {
                    moveTable(
                        existingTable,
                        position.left,
                        position.top
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

        var label = document.createElement("span");
        label.textContent = "Table " + tableCount;

        table.appendChild(label);
        canvas.appendChild(table);

        emptyState.style.display = "none";

        return table;
    }

    function isValidShape(shape) {
        return shape === "round" ||
            shape === "rectangle" ||
            shape === "oval";
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