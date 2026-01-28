window.downloadFileFromStream = (fileName, dataUrl) => {
    const anchorElement = document.createElement('a');
    anchorElement.href = dataUrl;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
}
