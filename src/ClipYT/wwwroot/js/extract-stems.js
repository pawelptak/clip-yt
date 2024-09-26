const appData = document.getElementById('app-data');
const mp3FormatEnumValue = appData.getAttribute('data-mp3-format');

function toggleExtractStems() {
    var selection = $('input[name="Format"]:checked').val();
    if (selection == mp3FormatEnumValue) {
        setInputsDisabledValue(false);
        $('#extract-stems-wrapper').show();
    } else {
        $('#extract-stems-wrapper').hide();
        setInputsDisabledValue(true);
    }
}

function setInputsDisabledValue(value) {
    $('#extract-stems-wrapper').find('.option-input').each(function () {
        var $this = $(this);
        $this.prop('disabled', value);
    });
}

$('input[name="Format"]').on('change', toggleExtractStems);