window.getElementText = (elementId) => {
    const element = document.getElementById(elementId);
    return element ? element.textContent : null;
}

function validateCoordinateInput(input) {
    input.value = input.value.replace(/[^0-9.-]/g, '');
}

window.compressCsvToGzip = async function (base64Csv) {
    const binary = atob(base64Csv);
    const uint8Array = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        uint8Array[i] = binary.charCodeAt(i);
    }
    const gzipped = pako.gzip(uint8Array);
    return btoa(String.fromCharCode(...gzipped));
}