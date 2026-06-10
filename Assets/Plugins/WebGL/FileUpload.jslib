mergeInto(LibraryManager.library, {
    OpenImageFileDialog: function (gameObjectNamePtr, callbackMethodNamePtr)
    {
        const gameObjectName = UTF8ToString(gameObjectNamePtr);
        const callbackMethodName = UTF8ToString(callbackMethodNamePtr);

        const input = document.createElement("input");
        input.type = "file";
        input.accept = "image/*";

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
                SendMessage(
                    gameObjectName,
                    callbackMethodName,
                    reader.result
                );
            };

            reader.readAsDataURL(file);
        };

        input.click();
    }
});