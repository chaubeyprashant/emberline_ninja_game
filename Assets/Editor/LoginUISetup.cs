using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Emberline.UI;

namespace Emberline.Editor
{
    public class LoginUISetup : UnityEditor.Editor
    {
        [MenuItem("Emberline/Setup/Create Login UI")]
        public static void CreateLoginUI()
        {
            // 1. Create Event System if it doesn't exist
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            // 2. Create Canvas
            GameObject canvasGO = new GameObject("LoginCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // 3. Create Background Panel
            GameObject panelGO = new GameObject("Background");
            panelGO.transform.SetParent(canvasGO.transform, false);
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // 4. Create Title Text
            GameObject titleGO = new GameObject("TitleText");
            titleGO.transform.SetParent(panelGO.transform, false);
            Text titleText = titleGO.AddComponent<Text>();
            titleText.text = "EMBERLINE";
            titleText.fontSize = 48;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            RectTransform titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 150);
            titleRect.sizeDelta = new Vector2(400, 100);

            // 5. Create Status Text
            GameObject statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(panelGO.transform, false);
            Text statusText = statusGO.AddComponent<Text>();
            statusText.text = "Ready to login.";
            statusText.fontSize = 24;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = Color.yellow;
            statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            RectTransform statusRect = statusGO.GetComponent<RectTransform>();
            statusRect.anchoredPosition = new Vector2(0, -150);
            statusRect.sizeDelta = new Vector2(400, 50);

            // 6. Create Guest Button
            GameObject guestBtnGO = CreateButton("GuestLoginButton", "Login as Guest", new Vector2(0, 20), panelGO.transform);
            Button guestButton = guestBtnGO.GetComponent<Button>();

            // 7. Create Google Button
            GameObject googleBtnGO = CreateButton("GoogleLoginButton", "Login with Google", new Vector2(0, -60), panelGO.transform);
            Button googleButton = googleBtnGO.GetComponent<Button>();

            // 8. Attach and Configure LoginController
            LoginController controller = canvasGO.AddComponent<LoginController>();
            
            // We use SerializedObject to set private serialized fields easily in editor script
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("guestLoginButton").objectReferenceValue = guestButton;
            serializedController.FindProperty("googleLoginButton").objectReferenceValue = googleButton;
            serializedController.FindProperty("statusText").objectReferenceValue = statusText;
            serializedController.ApplyModifiedProperties();

            // Select the newly created canvas in the hierarchy
            Selection.activeGameObject = canvasGO;
            Debug.Log("Login UI has been successfully generated in the scene.");
        }

        private static GameObject CreateButton(string name, string textStr, Vector2 position, Transform parent)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);
            
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            Button button = buttonGO.AddComponent<Button>();
            
            RectTransform rectTransform = buttonGO.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(250, 50);

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            Text text = textGO.AddComponent<Text>();
            text.text = textStr;
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return buttonGO;
        }
    }
}
