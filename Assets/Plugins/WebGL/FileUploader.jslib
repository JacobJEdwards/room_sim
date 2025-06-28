mergeInto(LibraryManager.library, {
    UploadImage: function (gameObjectName, methodName) {
        var gameObjectNameStr = UTF8ToString(gameObjectName);
        var methodNameStr = UTF8ToString(methodName);

        var input = document.createElement('input');
        input.type = 'file';
        input.accept = 'image/*';
        input.style.display = 'none';

        input.onchange = function (event) {
            var file = event.target.files[0];
            if (!file) {
                document.body.removeChild(input);
                return;
            }

            var reader = new FileReader();
            reader.onload = function (e) {
                var base64Data = e.target.result.substring(e.target.result.indexOf(',') + 1);

                // --- THE FIX FOR YOUR FILE ---
                // We use the global 'unityInstance' created by the loader script.
                if (unityInstance) {
                    unityInstance.SendMessage(gameObjectNameStr, methodNameStr, base64Data);
                } else {
                    console.error("Unity instance not found! Cannot send message.");
                }
            };

            reader.readAsDataURL(file);
            document.body.removeChild(input);
        };

        document.body.appendChild(input);
        input.click();
    }
});