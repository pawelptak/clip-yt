function toggleQualitySelectorAvailability(isEnabled) {
    document.querySelectorAll("#quality-select-container .radio-option")
        .forEach(el => el.classList.toggle("disabled", !isEnabled));

    if (!isEnabled) {
        const firstQualityRadioInput = document.querySelector("#quality-select-container input[type='radio']");
        firstQualityRadioInput.checked = true;
    }
}
