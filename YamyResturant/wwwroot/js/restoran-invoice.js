// ============================================================
// restoran-invoice.js
// Place this file in: wwwroot/js/restoran-invoice.js
// It is loaded as a normal static script, so Razor never parses
// the full-HTML invoice template (which is what broke the inline view).
//
// The view sets window.GET_ORDER_DETAILS_URL before this file loads.
// ============================================================

function printInvoice(orderId) {
    var detailsUrl = window.GET_ORDER_DETAILS_URL || '/Home/GetOrderDetails';

    $.get(detailsUrl + '?id=' + orderId, function (res) {
        if (!res.status) {
            toastr.error(res.message || "Failed to load invoice");
            return;
        }

        var order = res.data.order;
        var items = res.data.items || [];

        var subtotal = 0;
        var rows = "";
        $.each(items, function (i, item) {
            var amount = parseFloat(item.amount);
            subtotal += amount;
            rows += `
                <tr>
                    <td style="border:1px solid #ccc; padding:6px;">${i + 1}</td>
                    <td style="border:1px solid #ccc; padding:6px;">${item.itemName}</td>
                    <td style="border:1px solid #ccc; padding:6px; text-align:right;">${parseFloat(item.price).toFixed(2)}</td>
                    <td style="border:1px solid #ccc; padding:6px; text-align:center;">${item.qty}</td>
                    <td style="border:1px solid #ccc; padding:6px; text-align:right;">${amount.toFixed(2)}</td>
                </tr>`;
        });

        var discount = parseFloat(order.discountAmount) || 0;
        var tax = parseFloat(order.taxAmount) || 0;
        var grandTotal = parseFloat(order.grandTotal) || 0;

        var invoiceHtml = `
        <html>
        <head>
            <title>Invoice ${order.orderNo}</title>
        </head>
        <body style="font-family:Arial, sans-serif; padding:20px; color:#000;">
            <div style="text-align:center; margin-bottom:20px;">
                <h2 style="margin:0;">Restoran</h2>
                <p style="margin:2px 0; font-size:13px;">Tax Invoice</p>
            </div>

            <table style="width:100%; font-size:13px; margin-bottom:15px;">
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
                </tr>
            </table>

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

            <table style="width:100%; margin-top:15px; font-size:14px;">
                <tr>
                    <td style="text-align:right; padding-right:15px;">Subtotal:</td>
                    <td style="text-align:right; padding:4px 0;" width="120">AED ${subtotal.toFixed(2)}</td>
                </tr>
                <tr>
                    <td style="text-align:right; padding-right:15px;">Discount:</td>
                    <td style="text-align:right; padding:4px 0;">AED ${discount.toFixed(2)}</td>
                </tr>
                <tr>
                    <td style="text-align:right; padding-right:15px;">Tax:</td>
                    <td style="text-align:right; padding:4px 0;">AED ${tax.toFixed(2)}</td>
                </tr>
                <tr style="font-size:17px; font-weight:bold; border-top:2px solid #000;">
                    <td style="text-align:right; padding-right:15px;">Grand Total:</td>
                    <td style="text-align:right; padding:4px 0;">AED ${grandTotal.toFixed(2)}</td>
                </tr>
            </table>

            <div style="text-align:center; margin-top:25px; font-size:12px; color:#555;">
                <p>Thank you for your visit!</p>
            </div>
        </body>
        </html>`;

        var printWindow = window.open('', '', 'width=800,height=600');
        printWindow.document.write(invoiceHtml);
        printWindow.document.close();
        printWindow.focus();

        setTimeout(function () {
            printWindow.print();
        }, 300);

    }).fail(function () {
        toastr.error("Failed to load invoice");
    });
}
