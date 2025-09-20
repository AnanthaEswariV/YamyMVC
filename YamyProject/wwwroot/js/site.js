// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$('body').delegate('.js-render-modal', 'click', function () {
    var btn = $(this);
    var modal = $('#Modal');

    modal.find('#ModalLabel').text(btn.data('title'));

    if (btn.data('update') !== undefined) {
        updatedRow = btn.parents('tr');
    }

    $.get({
        url: btn.data('url'),
        success: function (form) {
            modal.find('.modal-body').html(form);
            $.validator.unobtrusive.parse(modal);
            applySelect2();
        },
        error: function () {
            showErrorMessage();
        }
    });
    modal.modal('show');
});
