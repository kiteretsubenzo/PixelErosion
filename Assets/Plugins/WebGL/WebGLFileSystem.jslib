mergeInto(LibraryManager.library, {
    OpenImageFileDialog: function (gameObjectNamePtr, callbackMethodNamePtr)
    {
        const gameObjectName = UTF8ToString(gameObjectNamePtr);
        const callbackMethodName = UTF8ToString(callbackMethodNamePtr);

        const input = document.createElement("input");
        input.type = "file";
        input.accept = ".bytes,image/*";

        input.onchange = function (e)
        {
            const file = e.target.files[0];

            if (!file)
            {
                return;
            }

            const reader = new FileReader();

            reader.onload = function ()
            {
                const payload = JSON.stringify({
                    fileName: file.name,
                    dataUrl: reader.result
                });

                SendMessage(
                    gameObjectName,
                    callbackMethodName,
                    payload
                );
            };

            reader.readAsDataURL(file);
        };

        input.click();
    }
});

mergeInto(LibraryManager.library, {
    DownloadFile: function (fileNamePtr, mimeTypePtr, base64Ptr) {
        const fileName = UTF8ToString(fileNamePtr);
        const mimeType = UTF8ToString(mimeTypePtr);
        const base64 = UTF8ToString(base64Ptr);

        const link = document.createElement("a");
        link.href = "data:" + mimeType + ";base64," + base64;
        link.download = fileName;

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
});