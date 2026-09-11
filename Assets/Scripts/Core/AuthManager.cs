using System;
using System.Threading.Tasks;
using UnityEngine;
#if FIREBASE_AUTH_ENABLED
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
#endif

namespace Emberline.Core
{
    /// <summary>
    /// Manages Firebase Authentication for Guest and Google sign-in.
    /// Add the scripting define symbol FIREBASE_AUTH_ENABLED in
    /// Player Settings after importing the Firebase SDK.
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

#if FIREBASE_AUTH_ENABLED
        private FirebaseAuth auth;
        private FirebaseUser user;
        private DependencyStatus dependencyStatus = DependencyStatus.UnavailableOther;
#endif

        public Action<string> OnSignInSuccess;
        public Action<string> OnSignInFailed;
        
        public bool IsFirebaseReady { get; private set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
#if FIREBASE_AUTH_ENABLED
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                dependencyStatus = task.Result;
                if (dependencyStatus == DependencyStatus.Available)
                {
                    InitializeFirebaseAuth();
                }
                else
                {
                    Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
                }
            });
#else
            Debug.LogWarning("Firebase Auth SDK not imported. Running in stub mode. Add FIREBASE_AUTH_ENABLED to Scripting Define Symbols after importing the Firebase SDK.");
            // In stub mode, mark as ready immediately so guest login works offline
            IsFirebaseReady = true;
#endif
        }

#if FIREBASE_AUTH_ENABLED
        private void InitializeFirebaseAuth()
        {
            auth = FirebaseAuth.DefaultInstance;
            auth.StateChanged += AuthStateChanged;
            AuthStateChanged(this, null);
            IsFirebaseReady = true;
            Debug.Log("Firebase Auth initialized successfully.");
        }

        private void AuthStateChanged(object sender, EventArgs eventArgs)
        {
            if (auth.CurrentUser != user)
            {
                bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
                if (!signedIn && user != null)
                {
                    Debug.Log("Signed out " + user.UserId);
                }
                user = auth.CurrentUser;
                if (signedIn)
                {
                    Debug.Log("Signed in " + user.UserId);
                }
            }
        }
#endif

        public void SignInAnonymously()
        {
#if FIREBASE_AUTH_ENABLED
            if (!IsFirebaseReady)
            {
                OnSignInFailed?.Invoke("Firebase is not ready yet.");
                return;
            }

            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("SignInAnonymouslyAsync was canceled.");
                    OnSignInFailed?.Invoke("Sign in canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                    OnSignInFailed?.Invoke(task.Exception.InnerExceptions.Count > 0 ? task.Exception.InnerExceptions[0].Message : "Sign in failed.");
                    return;
                }

                AuthResult result = task.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})", result.User.DisplayName, result.User.UserId);
                OnSignInSuccess?.Invoke(result.User.UserId);
            });
#else
            Debug.Log("Guest Sign In (stub mode — no Firebase SDK).");
            OnSignInSuccess?.Invoke("guest_" + SystemInfo.deviceUniqueIdentifier);
#endif
        }

        public void SignInWithGoogle(string idToken)
        {
#if FIREBASE_AUTH_ENABLED
            if (!IsFirebaseReady)
            {
                OnSignInFailed?.Invoke("Firebase is not ready yet.");
                return;
            }
            
            if (string.IsNullOrEmpty(idToken))
            {
                OnSignInFailed?.Invoke("Google ID token is missing.");
                return;
            }

            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
            auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("SignInWithCredentialAsync was canceled.");
                    OnSignInFailed?.Invoke("Google sign in canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
                    OnSignInFailed?.Invoke(task.Exception.InnerExceptions.Count > 0 ? task.Exception.InnerExceptions[0].Message : "Google sign in failed.");
                    return;
                }

                AuthResult result = task.Result;
                Debug.LogFormat("User signed in successfully with Google: {0} ({1})", result.User.DisplayName, result.User.UserId);
                OnSignInSuccess?.Invoke(result.User.UserId);
            });
#else
            Debug.LogWarning("Google Sign In unavailable — Firebase SDK not imported.");
            OnSignInFailed?.Invoke("Google Sign In requires the Firebase SDK. Import it and add FIREBASE_AUTH_ENABLED to Scripting Define Symbols.");
#endif
        }

        public void SignOut()
        {
#if FIREBASE_AUTH_ENABLED
            if (auth != null)
            {
                auth.SignOut();
                user = null;
            }
#endif
        }

        private void OnDestroy()
        {
#if FIREBASE_AUTH_ENABLED
            if (auth != null)
            {
                auth.StateChanged -= AuthStateChanged;
                auth = null;
            }
#endif
        }
    }
}
