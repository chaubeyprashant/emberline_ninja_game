using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Emberline.Core;

namespace Emberline.UI
{
    public class LoginController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button guestLoginButton;
        [SerializeField] private Button googleLoginButton;
        [SerializeField] private Text statusText;
        [SerializeField] private string nextSceneName = "MainMenu";

        private void OnEnable()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnSignInSuccess += HandleSignInSuccess;
                AuthManager.Instance.OnSignInFailed += HandleSignInFailed;
            }
        }

        private void OnDisable()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnSignInSuccess -= HandleSignInSuccess;
                AuthManager.Instance.OnSignInFailed -= HandleSignInFailed;
            }
        }

        private void Start()
        {
            if (guestLoginButton != null)
                guestLoginButton.onClick.AddListener(OnGuestLoginClicked);

            if (googleLoginButton != null)
                googleLoginButton.onClick.AddListener(OnGoogleLoginClicked);

            SetStatus("Ready to login.");
        }

        private void OnGuestLoginClicked()
        {
            SetStatus("Logging in as guest...");
            guestLoginButton.interactable = false;
            googleLoginButton.interactable = false;
            AuthManager.Instance.SignInAnonymously();
        }

        private void OnGoogleLoginClicked()
        {
            SetStatus("Logging in with Google...");
            guestLoginButton.interactable = false;
            googleLoginButton.interactable = false;

            // In a real application, you'd use a Google Sign-In plugin here to obtain the ID Token
            // e.g. GoogleSignIn.DefaultInstance.SignIn().ContinueWith(task => ... AuthManager.Instance.SignInWithGoogle(task.Result.IdToken));
            // For now, we call it with an empty token which will fail, unless simulated.
            AuthManager.Instance.SignInWithGoogle(""); 
        }

        private void HandleSignInSuccess(string userId)
        {
            SetStatus($"Login successful! User ID: {userId}");
            // Transition to next scene
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
        }

        private void HandleSignInFailed(string error)
        {
            SetStatus($"Login failed: {error}");
            guestLoginButton.interactable = true;
            googleLoginButton.interactable = true;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log(message);
        }
    }
}
