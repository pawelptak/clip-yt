function toggleQualitySelectorAvailability(isEnabled) {
    document.querySelectorAll("#quality-select-container .radio-option")
        .forEach(el => el.classList.toggle("disabled", !isEnabled));
}
