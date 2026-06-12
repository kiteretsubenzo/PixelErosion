mergeInto(LibraryManager.library, {
    OpenImageFileDialog: function (gameObjectNamePtr, callbackMethodNamePtr)
    {
        const gameObjectName = UTF8ToString(gameObjectNamePtr);
        const callbackMethodName = UTF8ToString(callbackMethodNamePtr);

        // 💡 【iOS対策】使い回し可能なinput要素をbodyに登録して、iPhoneのメモリ掃除から守る
        let input = document.getElementById("unity-ios-file-picker");
        if (!input)
        {
            input = document.createElement("input");
            input.id = "unity-ios-file-picker";
            input.type = "file";
            input.accept = ".bytes,image/*";
            input.style.display = "none";
            document.body.appendChild(input);
        }

        input.onchange = function (e)
        {
            const file = e.target.files[0];
            if (!file) return;

            const reader = new FileReader();
            reader.onload = function ()
            {
                // 💡 【iOS大容量対策】JS側でBase64のヘッダーを削り、C#側でのメモリコピーを防ぐ
                const resultStr = reader.result;
                const commaIndex = resultStr.indexOf(',');
                const base64Data = commaIndex >= 0 ? resultStr.substring(commaIndex + 1) : resultStr;

                const payload = JSON.stringify({
                    fileName: file.name,
                    dataUrl: base64Data
                });

                // 💡 余計な変数を挟まず、直球で呼ぶ！これでUnityのコンパイラが正しく翻訳します
                SendMessage(gameObjectName, callbackMethodName, payload);

                input.value = ""; // 連続選択対策
            };

            reader.readAsDataURL(file);
        };

        input.value = "";
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