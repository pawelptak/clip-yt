const appData = document.getElementById('app-data');
const mp3FormatEnumValue = appData.getAttribute('data-mp3-format');

function toggleSeparateTracks() {
    var selection = $('input[name="Format"]:checked').val();
    if (selection == mp3FormatEnumValue) {
        setInputsDisabledValue(false);
        $('#separate-tracks-wrapper').show();
    } else {
        $('#separate-tracks-wrapper').hide();
        setInputsDisabledValue(true);
    }
}

function setInputsDisabledValue(value) {
    $('#separate-tracks-wrapper').find('.radio-input').each(function () {
        var $this = $(this);
        $this.prop('disabled', value);
    });
}

$('input[name="Format"]').on('change', toggleSeparateTracks);