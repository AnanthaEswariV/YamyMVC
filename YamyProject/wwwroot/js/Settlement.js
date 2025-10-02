function recalcRow($row) {
    var onHand = parseFloat($row.find('.on-hand').val()) || 0;
    var price = parseFloat($row.find('.price').val()) || 0;
    var newQty = parseFloat($row.find('.new-qty').val()) || 0;

    $row.find('.sys-amount').val((onHand * price).toFixed(2));
    $row.find('.qty-diff').val((newQty - onHand).toFixed(2));
    $row.find('.minus-amt').val(Math.max(0, (onHand - newQty) * price).toFixed(2));
    $row.find('.plus-amt').val(Math.max(0, (newQty - onHand) * price).toFixed(2));
}

function setupAutocomplete($row) {
    $row.find('.item-code, .item-name').autocomplete({
        source: function (request, response) {
            $.getJSON('@Url.Action("GetItems")', { term: request.term }, function (data) {
                console.log('@Url.Action("GetItems")');
                debugger
                response($.map(data, function (item) {
                    return {
                        label: item.Code + " - " + item.Name,
                        value: item.Code,
                        itemData: item
                    };
                }));
            });
        },
        select: function (event, ui) {
            $row.find('.item-code').val(ui.item.itemData.Code);
            $row.find('.item-name').val(ui.item.itemData.Name);
            $row.find('.on-hand').val(ui.item.itemData.OnHand);
            $row.find('.price').val(ui.item.itemData.CostPrice);
            recalcRow($row);
            return false;
        },
        minLength: 1
    });
}

$(document).ready(function () {
    // Event delegation for dynamic elements
    debugger
    $('#itemsTable').on('click', '.remove-row', function () {
        $(this).closest('tr').remove();
        updateRowIndexes();
    });

    $('#itemsTable').on('input', '.price, .new-qty', function () {
        recalcRow($(this).closest('tr'));
    });

    // Initialize existing rows
    $('#itemsTable tbody tr').each(function () {
        var $row = $(this);
        setupAutocomplete($row);
        recalcRow($row);
    });

    $('#addRowBtn').click(function () {
        var index = $('#itemsTable tbody tr').length;
        var newRow = $(`
                    <tr>
                        <td>${index + 1}</td>
                        <td><input name="Items[${index}].ItemCode" class="form-control item-code" /></td>
                        <td><input name="Items[${index}].ItemName" class="form-control item-name" /></td>
                        <td><input name="Items[${index}].OnHand" class="form-control on-hand" readonly /></td>
                        <td><input name="Items[${index}].Price" class="form-control price" /></td>
                        <td><input class="form-control sys-amount" readonly /></td>
                        <td><input name="Items[${index}].NewOnHand" class="form-control new-qty" /></td>
                        <td><input class="form-control qty-diff" readonly /></td>
                        <td><input name="Items[${index}].MinusAmount" class="form-control minus-amt" readonly /></td>
                        <td><input name="Items[${index}].PlusAmount" class="form-control plus-amt" readonly></td>
                        <td><button type="button" class="btn btn-danger remove-row">Remove</button></td>
                    </tr>
                `);
        $('#itemsTable tbody').append(newRow);
        setupAutocomplete(newRow);
    });

    function updateRowIndexes() {
        $('#itemsTable tbody tr').each(function (i) {
            $(this).find('td:first').text(i + 1);
            $(this).find('input, select').each(function () {
                var name = $(this).attr('name');
                if (name) {
                    var newName = name.replace(/\d+/, i);
                    $(this).attr('name', newName);
                }
            });
        });
    }
});