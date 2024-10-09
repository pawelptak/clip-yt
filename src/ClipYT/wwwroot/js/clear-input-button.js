$(document).ready(function () {
    const input = document.querySelector(".clear-input")

    function handleInputChange(e) {
        if (e.target.value && !input.classList.contains("clear-input--touched")) {
            input.classList.add("clear-input--touched");
        } else if (!e.target.value && input.classList.contains("clear-input--touched")) {
            input.classList.remove("clear-input--touched");
        }
    }

    input.addEventListener("input", handleInputChange)

    const clearButton = document.querySelector(".clear-input-button")

    const handleButtonClick = (e) => {
        input.value = ''
        input.focus()
        input.classList.remove("clear-input--touched")
    }

    clearButton.addEventListener("click", handleButtonClick)
});