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

                // Try multiple ways to access the Unity instance
                try {
                    // Method 1: Global unityInstance (Unity 2020+)
                    if (typeof unityInstance !== 'undefined' && unityInstance) {
                        unityInstance.SendMessage(gameObjectNameStr, methodNameStr, base64Data);
                    }
                    // Method 2: Global gameInstance (older Unity versions)
                    else if (typeof gameInstance !== 'undefined' && gameInstance) {
                        gameInstance.SendMessage(gameObjectNameStr, methodNameStr, base64Data);
                    }
                    // Method 3: Module-based (Unity 2020.1+)
                    else if (typeof Module !== 'undefined' && Module.unityInstance) {
                        Module.unityInstance.SendMessage(gameObjectNameStr, methodNameStr, base64Data);
                    }
                    // Method 4: MyGameInstance (custom template)
                    else if (typeof MyGameInstance !== 'undefined' && MyGameInstance) {
                        MyGameInstance.SendMessage(gameObjectNameStr, methodNameStr, base64Data);
                    }
                    // Method 5: Direct Module SendMessage
                    else if (typeof Module !== 'undefined' && Module.SendMessage) {
                        Module.SendMessage(gameObjectNameStr, methodNameStr, base64Data);
                    }
                    // Method 6: Window.unityGame
                    else if (window.unityGame) {
                        window.unityGame.SendMessage(gameObjectNameStr, methodNameStr, base64Data);
                    }
                    else {
                        console.error("Unity instance not found! Cannot send message.");
                        console.log("Available globals:", Object.keys(window).filter(k => k.toLowerCase().includes('unity') || k.toLowerCase().includes('game') || k.toLowerCase().includes('instance')));
                    }
                } catch (error) {
                    console.error("Error sending message to Unity:", error);
                }
            };

            reader.readAsDataURL(file);
            document.body.removeChild(input);
        };

        document.body.appendChild(input);
        input.click();
    }
});