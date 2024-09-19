function toggleSeparateTracks() {
    var selection = $('input[name="Format"]:checked').val();
    if (selection == mp3FormatEnumValue) {
        $('#separate-tracks-wrapper').show();
    } else {
        $('#separate-tracks-wrapper').hide();
        resetSeparateTracksInput();
    }
}

function resetSeparateTracksInput() {
    $('#separate-tracks-wrapper').find('.radio-input').each(function () {
        var $this = $(this);
        if ($this.val() == separateTracksEnumValue) {
            $this.prop('checked', false);
        } else {
            $this.prop('checked', true);
        }
    });
}

$('input[name="Format"]').on('change', toggleSeparateTracks);