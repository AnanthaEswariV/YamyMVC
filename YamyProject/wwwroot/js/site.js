
function showErrorMessage(message = 'Something went wrong!') {
    Swal.fire({
        icon: 'error',
        title: 'Oops...',
        text: message.responseText !== undefined ? message.responseText : message,
        customClass: {
            confirmButton: "btn btn-primary"
        }
    });
}
$(document).ready(function () {
    $('body').delegate('.js-render-modal', 'click', function () {
    var btn = $(this);
    var modal = $('#Modal');
    modal.find('#ModalLabel').text(btn.data('title'));
    if (btn.data('update') !== undefined) {
        updatedRow = btn.parents('tr');
    }
    $.get({
        url: btn.data('url'),success: function (form) {
            modal.find('.modal-body').html(form);
            $.validator.unobtrusive.parse(modal);
            //applySelect2();
           },
        error: function () {
          //  showErrorMessage();
        }
    });
    modal.modal('show');
});
});
$('#Modal').on('hidden.bs.modal', function () {
    $('#btnOpenModal').focus(); // return to your Add button
});


function applySelect2() {
    $('.js-select2').select2();
    $('.js-select2').on('select2:select', function (e) {
        $('form').not('#SignOut').validate().element('#' + $(this).attr('id'));
    });
}
