// ============================================================
// restoran-invoice.js   ->  wwwroot/js/restoran-invoice.js
// Kept in a static file so Razor never parses the full-HTML templates.
//
// Globals:
//   printInvoice(orderId)   - print a SAVED order (uses window.GET_ORDER_DETAILS_URL)
//   printBillData(data)     - print the CURRENT (unsaved) cart as a bill
// ============================================================

function _invoiceRows(items) {
    var rows = "";
    $.each(items, function (i, item) {
        var price = parseFloat(item.price);
        var amount = parseFloat(item.amount);
        rows += `
            <tr>
                <td style="border:1px solid #ccc; padding:6px;">${i + 1}</td>
                <td style="border:1px solid #ccc; padding:6px;">${item.itemName}</td>
                <td style="border:1px solid #ccc; padding:6px; text-align:right;">${price.toFixed(2)}</td>
                <td style="border:1px solid #ccc; padding:6px; text-align:center;">${item.qty}</td>
                <td style="border:1px solid #ccc; padding:6px; text-align:right;">${amount.toFixed(2)}</td>
            </tr>`;
    });
    return rows;
}

function _invoiceShell(title, headerRows, items, subtotal, discount, tax, grandTotal, footerNote) {
    var rows = _invoiceRows(items);
    var lines = "";
    lines += `<tr><td style="text-align:right; padding-right:15px;">Subtotal:</td><td style="text-align:right; padding:4px 0;" width="120">AED ${subtotal.toFixed(2)}</td></tr>`;
    if (discount > 0) lines += `<tr><td style="text-align:right; padding-right:15px;">Discount:</td><td style="text-align:right; padding:4px 0;">AED ${discount.toFixed(2)}</td></tr>`;
    if (tax > 0) lines += `<tr><td style="text-align:right; padding-right:15px;">Tax:</td><td style="text-align:right; padding:4px 0;">AED ${tax.toFixed(2)}</td></tr>`;
    lines += `<tr style="font-size:17px; font-weight:bold; border-top:2px solid #000;"><td style="text-align:right; padding-right:15px;">Grand Total:</td><td style="text-align:right; padding:4px 0;">AED ${grandTotal.toFixed(2)}</td></tr>`;

    return `
    <html>
    <head><title>${title}</title></head>
    <body style="font-family:Arial, sans-serif; padding:20px; color:#000;">
        <div style="text-align:center; margin-bottom:18px;">
            <h2 style="margin:0;">Restoran</h2>
            <p style="margin:2px 0; font-size:13px;">${title}</p>
        </div>

        <table style="width:100%; font-size:13px; margin-bottom:15px;">${headerRows}</table>

        <table style="width:100%; border-collapse:collapse; font-size:13px;">
            <thead>
                <tr>
                    <th style="border:1px solid #ccc; padding:6px; background:#f0f0f0; text-align:left;" width="40">#</th>
                    <th style="border:1px solid #ccc; padding:6px; background:#f0f0f0; text-align:left;">Item</th>
                    <th style="border:1px solid #ccc; padding:6px; background:#f0f0f0; text-align:right;" width="80">Price</th>
                    <th style="border:1px solid #ccc; padding:6px; background:#f0f0f0; text-align:center;" width="60">Qty</th>
                    <th style="border:1px solid #ccc; padding:6px; background:#f0f0f0; text-align:right;" width="90">Total</th>
                </tr>
            </thead>
            <tbody>${rows}</tbody>
        </table>

        <table style="width:100%; margin-top:15px; font-size:14px;">${lines}</table>

        <div style="text-align:center; margin-top:25px; font-size:12px; color:#555;">
            <p>${footerNote}</p>
        </div>
    </body>
    </html>`;
}

function _openPrint(html) {
    var w = window.open('', '', 'width=800,height=600');
    w.document.write(html);
    w.document.close();
    w.focus();
    setTimeout(function () { w.print(); }, 300);
}

// ---- Print a SAVED order ----
function printInvoice(orderId) {
    var detailsUrl = window.GET_ORDER_DETAILS_URL || '/Home/GetOrderDetails';
    $.get(detailsUrl + '?id=' + orderId, function (res) {
        if (!res.status) { toastr.error(res.message || "Failed to load invoice"); return; }
        var order = res.data.order;
        var items = res.data.items || [];
        var subtotal = 0;
        $.each(items, function (i, it) { subtotal += parseFloat(it.amount); });

        var header = `
            <tr>
                <td style="padding:2px 0;"><strong>Invoice No:</strong> ${order.orderNo}</td>
                <td style="padding:2px 0; text-align:right;"><strong>Date:</strong> ${new Date().toLocaleString()}</td>
            </tr>
            <tr>
                <td style="padding:2px 0;"><strong>Customer:</strong> ${order.customerName || 'Walk-In'}</td>
                <td style="padding:2px 0; text-align:right;"><strong>Mobile:</strong> ${order.customerMobile || '-'}</td>
            </tr>
            <tr>
                <td style="padding:2px 0;"><strong>Order Type:</strong> ${order.orderType === 'DineIn' ? 'Dine In' : 'Takeaway'}</td>
                <td style="padding:2px 0; text-align:right;"><strong>Table:</strong> ${order.tableName || 'N/A'}</td>
            </tr>`;

        var discount = parseFloat(order.discountAmount) || 0;
        var tax = parseFloat(order.taxAmount) || 0;
        var grandTotal = parseFloat(order.grandTotal) || 0;

        _openPrint(_invoiceShell('Tax Invoice ' + order.orderNo, header, items, subtotal, discount, tax, grandTotal, 'Thank you for your visit!'));
    }).fail(function () { toastr.error("Failed to load invoice"); });
}

// ---- Print the CURRENT cart as a bill (pre-payment) ----
function printBillData(data) {
    var items = data.items || [];
    var metaRows = "";
    if (data.note) metaRows += `<tr><td colspan="2" style="padding:2px 0;"><strong>Note:</strong> ${data.note}</td></tr>`;
    if (data.kitchenNote) metaRows += `<tr><td colspan="2" style="padding:2px 0;"><strong>Kitchen:</strong> ${data.kitchenNote}</td></tr>`;

    var header = `
        <tr>
            <td style="padding:2px 0;"><strong>Bill</strong> (not yet paid)</td>
            <td style="padding:2px 0; text-align:right;"><strong>Date:</strong> ${new Date().toLocaleString()}</td>
        </tr>
        <tr>
            <td style="padding:2px 0;"><strong>Customer:</strong> ${data.customerName || 'Walk-In'}</td>
            <td style="padding:2px 0; text-align:right;"><strong>Mobile:</strong> ${data.customerMobile || '-'}</td>
        </tr>
        <tr>
            <td style="padding:2px 0;"><strong>Order Type:</strong> ${data.orderType === 'DineIn' ? 'Dine In' : 'Takeaway'}</td>
            <td style="padding:2px 0; text-align:right;"><strong>Table:</strong> ${data.tableName || 'N/A'}</td>
        </tr>
        ${data.guests ? `<tr><td style="padding:2px 0;"><strong>Guests:</strong> ${data.guests}</td><td></td></tr>` : ''}
        ${metaRows}`;

    _openPrint(_invoiceShell('Bill', header, items, data.subtotal, data.discount || 0, 0, data.grandTotal, 'This is a bill, not a paid receipt.'));
}
