function setupRadioHighlight(wrapperSelector) {
    const wrapper = document.querySelector(wrapperSelector);

    if (!wrapper) return;

    const highlightBar = wrapper.querySelector('.radio-highlight-bar');
    const radios = Array.from(wrapper.querySelectorAll('.radio-input'));

    function moveHighlightBar() {
        const checkedRadio = radios.find(r => r.checked);
        const option = checkedRadio.closest('.radio-option');
        const rect = option.getBoundingClientRect();
        const wrapperRect = wrapper.getBoundingClientRect();
        highlightBar.style.left = (rect.left - wrapperRect.left) + 'px';
        highlightBar.style.width = rect.width + 'px';
    }

    radios.forEach(radio => {
        radio.addEventListener('change', moveHighlightBar);
    });

    // Initial position
    moveHighlightBar();
    //window.addEventListener('resize', moveHighlightBar);
}
