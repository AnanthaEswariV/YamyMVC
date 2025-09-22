let dataTable;





// Define saveCategory globally
function saveCategory() {
    var categoryName = $("#categoryName").val();

    if (!categoryName.trim()) {
        alert("Please enter a category name.");
        return;
    }

    $.ajax({
        url: '/Lists/AddCategory',
        type: 'POST',
        data: { categoryName: categoryName },
        success: function (result) {
            if (result.status) {
                var modalEl = document.getElementById("addCategoryModal");
                var modal = bootstrap.Modal.getInstance(modalEl);
                modal.hide();

                $("#addCategoryForm")[0].reset();
                toastr.success('Category Added Successfully!');
                // ✅ Refresh the category table/grid
                if (typeof loadCategories === "function") {
                    loadCategories();
                }
                // setTimeout(function () {
                //     location.reload();
                // }, 1500);
            } else {
                toastr.error(result.message);
            }
        },
        error: function (xhr, status, error) {
            alert("Error: " + error);
        }
    });
}



function saveUnit() {
    const name = $("#addUnitName").val();
    if (!name.trim()) {
        alert("Please enter a unit name.");
        return;
    }

    $.post('/Lists/AddUnit', { name: name }, function (res) {
        if (res.status) {
            const modalEl = document.getElementById("addUnitModal");
            const modal = bootstrap.Modal.getInstance(modalEl);
            modal.hide();
            $("#addUnitForm")[0].reset();
            toastr.success('Unit Added Successfully!');
            // Redirect to dashboard after a short delay
            setTimeout(function () {
                location.reload();
            }, 1500); // Optional delay before redirect
        } else {
            toastr.error(result.message);
        }
    }).fail(function (xhr) {
        alert("Error: " + xhr.statusText);
    });
}



function loadManagersDropdown(selectedId = null) {
    $.getJSON('/Inventory/GetAllEmployees', function (employees) {
        const $manager = $('#manager');
        $manager.empty().append('<option value="">Select Manager</option>');

        employees.forEach(emp => {
            const selected = selectedId && emp.id === selectedId ? 'selected' : '';
            $manager.append(`<option value="${emp.id}" ${selected}>${emp.name}</option>`);
        });
    });
}




$(document).ready(function () {
    $.ajax({
        url: '/HR/GetAccruedSalaries',
        type: 'GET',
        success: function (data) {
            console.log("✅ API Response:", data);

            // List of dropdowns with their matching code textbox
            const dropdowns = [
                { ddl: '#accruedSalaries' }
            ];

            dropdowns.forEach(set => {
                let $ddl = $(set.ddl);
                console.log("Found dropdown?", set.ddl, $ddl.length);

                $ddl.empty().append('<option value="">-- Select --</option>');

                $.each(data, function (i, item) {
                    $ddl.append(
                        $('<option>', {
                            value: item.id,
                            text: item.code + ' - ' + item.name,
                            'data-code': item.code
                        })
                    );
                });

                console.log("Total options now for", set.ddl, ":", $ddl.find('option').length);

                // On change, update the code textbox
                $ddl.on('change', function () {
                    let selected = $(this).find(':selected');
                    let code = selected.data('code');
                    console.log("Selected in", set.ddl, ":", selected.text(), "Code:", code);
                    $(set.codeBox).val(code || "");
                });
            });
        },
        error: function (xhr, status, error) {
            console.error("❌ Error loading accounts:", status, error);
        }
    });
});




$(document).ready(function () {
    loadManagersDropdown();



    $("#newWarehouseForm").submit(function (e) {
        e.preventDefault();

        let model = {
            Id: parseInt($("#warehouseId").val()) || 0,
            Name: $("#warehouseName").val(),
            City: $("#city").val(),
            BuildingName: $("#buildingName").val(),
            EmpId: parseInt($("#manager").val()) || null,
            AccountId: parseInt($("#accruedSalaries").val()) || null,
        };

        $.ajax({
            url: "/Inventory/SaveWarehouse",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(model),
            success: function (res) {
                if (res && res.status) {
                    toastr.success(res.message || "Warehouse saved successfully!");
                    $("#warehouseCode").val(res.code || "");
                    $("#newWarehouseModal").modal("hide");
                    $("#newWarehouseForm")[0].reset();
                    $("#warehouseId").val(0);

                    //      // ✅ Refresh warehouse table and dropdown
                    // if (typeof loadWarehouse === "function") {
                    //     loadWarehouse();
                    // }
                    // if (typeof loadWarehouses === "function") {
                    //     loadWarehouses();
                    // }
                    setTimeout(function () {
                        location.reload();
                    }, 1500);
                } else {
                    toastr.error(res?.message || "Failed to save warehouse");
                }
            },
            error: function (xhr) {
                toastr.error(xhr.responseJSON?.message || xhr.statusText || "Server error");
            }
        });
    });

});
