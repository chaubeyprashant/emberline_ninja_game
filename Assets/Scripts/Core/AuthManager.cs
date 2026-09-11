using System;
using UnityEngine;

#if FIREBASE_AUTH
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
#endif

namespace Emberline.Core
{
    /// <summary>
    /// Centralised Firebase Authentication service. Handles:
    /// - Anonymous (guest) sign-in with session persistence
    /// - Google sign-in via Android Credential Manager
    /// - Guest → Google account linking (preserves UID)
    /// - Auth state restoration on cold start
    ///
    /// All Firebase calls are guarded by FIREBASE_AUTH so the project compiles
    /// without the SDK. In stub mode, guest login returns a device-based ID.
    ///
    /// Add FIREBASE_AUTH to Player Settings → Scripting Define Symbols after
    /// importing FirebaseApp.unitypackage and FirebaseAuth.unitypackage.
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        // ---------------------------------------------------------------- singleton
        public static AuthManager Instance { get; private set; }

        // ---------------------------------------------------------------- public state
        public bool IsInitialised { get; private set; }
        public bool IsAuthenticated { get; private set; }
        public bool IsGuest { get; private set; }
        public bool IsGoogleUser { get; private set; }
        public string FirebaseUid { get; private set; } = "";
        public string DisplayName { get; private set; } = "";

        // ---------------------------------------------------------------- events
        /// <summary>Fires after any successful sign-in or link. Carries the UID.</summary>
        public event Action<string> OnAuthStateChanged;

        /// <summary>Fires on any auth error. Carries a player-friendly message.</summary>
        public event Action<string> OnAuthError;

        // ---------------------------------------------------------------- constants
        private const string PrefKeyUid = "firebase_uid";
        private const string PrefKeyAuthMethod = "firebase_auth_method";

#if FIREBASE_AUTH
        private FirebaseAuth _auth;
        private FirebaseUser _user;
#endif

        // ================================================================ lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitialiseFirebase();
        }

        private void OnDestroy()
        {
#if FIREBASE_AUTH
            if (_auth != null) { _auth.StateChanged -= OnFirebaseStateChanged; _auth = null; }
#endif
            if (Instance == this) Instance = null;
        }

        // ================================================================ initialisation

        private void InitialiseFirebase()
        {
#if FIREBASE_AUTH
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"[Auth] Firebase dependency check failed: {task.Result}");
                    RaiseError("Firebase is unavailable on this device.");
                    return;
                }
                _auth = FirebaseAuth.DefaultInstance;
                _auth.StateChanged += OnFirebaseStateChanged;
                IsInitialised = true;

                // Restore existing session — do NOT create a new anonymous user.
                if (_auth.CurrentUser != null)
                {
                    ApplyUser(_auth.CurrentUser);
                    Debug.Log($"[Auth] Session restored: {FirebaseUid} (guest={IsGuest})");
                }
                else
                {
                    Debug.Log("[Auth] No existing session. Waiting for user action.");
                }
            });
#else
            Debug.Log("[Auth] Stub mode — Firebase SDK not imported.");
            IsInitialised = true;
#endif
        }

#if FIREBASE_AUTH
        private void OnFirebaseStateChanged(object sender, EventArgs e)
        {
            var user = _auth.CurrentUser;
            if (user == _user) return;
            _user = user;
            if (user != null) ApplyUser(user);
            else ClearState();
        }

        private void ApplyUser(FirebaseUser user)
        {
            _user = user;
            FirebaseUid = user.UserId;
            DisplayName = string.IsNullOrEmpty(user.DisplayName) ? "" : user.DisplayName;
            IsAuthenticated = true;
            IsGuest = user.IsAnonymous;
            IsGoogleUser = false;
            foreach (var info in user.ProviderData)
            {
                if (info.ProviderId == GoogleAuthProvider.ProviderId)
                {
                    IsGoogleUser = true;
                    if (string.IsNullOrEmpty(DisplayName))
                        DisplayName = info.DisplayName ?? "";
                    break;
                }
            }

            PlayerPrefs.SetString(PrefKeyUid, FirebaseUid);
            PlayerPrefs.SetString(PrefKeyAuthMethod, IsGoogleUser ? "google" : "guest");
            PlayerPrefs.Save();

            OnAuthStateChanged?.Invoke(FirebaseUid);
        }
#endif

        private void ClearState()
        {
            FirebaseUid = "";
            DisplayName = "";
            IsAuthenticated = false;
            IsGuest = false;
            IsGoogleUser = false;
        }

        private void RaiseError(string message)
        {
            Debug.LogWarning($"[Auth] {message}");
            OnAuthError?.Invoke(message);
        }

        // ================================================================ guest sign-in

        /// <summary>
        /// Sign in anonymously. If a session already exists, this is a no-op.
        /// </summary>
        public void SignInAsGuest()
        {
#if FIREBASE_AUTH
            if (!IsInitialised) { RaiseError("Firebase is still loading."); return; }
            if (IsAuthenticated) { OnAuthStateChanged?.Invoke(FirebaseUid); return; }

            _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled) { RaiseError("Sign-in was cancelled."); return; }
                if (task.IsFaulted)
                {
                    RaiseError(FriendlyError(task.Exception));
                    return;
                }
                ApplyUser(task.Result.User);
                Debug.Log($"[Auth] Guest sign-in: {FirebaseUid}");
            });
#else
            // Stub: use device ID as a fake UID.
            FirebaseUid = "guest_" + SystemInfo.deviceUniqueIdentifier;
            DisplayName = "";
            IsAuthenticated = true;
            IsGuest = true;
            IsGoogleUser = false;
            PlayerPrefs.SetString(PrefKeyUid, FirebaseUid);
            PlayerPrefs.SetString(PrefKeyAuthMethod, "guest");
            PlayerPrefs.Save();
            OnAuthStateChanged?.Invoke(FirebaseUid);
#endif
        }

        // ================================================================ google sign-in

        /// <summary>
        /// Starts the Google sign-in flow via Android Credential Manager.
        /// The native bridge calls back into <see cref="OnGoogleTokenReceived"/>
        /// or <see cref="OnGoogleSignInFailed"/>.
        /// </summary>
        public void SignInWithGoogle()
        {
#if FIREBASE_AUTH
            if (!IsInitialised) { RaiseError("Firebase is still loading."); return; }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var bridge = new AndroidJavaClass("com.ergebins.emberline3d.GoogleCredentialBridge");
                using var activity = GetUnityActivity();
                bridge.CallStatic("signIn", activity);
            }
            catch (Exception ex)
            {
                RaiseError("Could not start Google sign-in: " + ex.Message);
            }
#elif UNITY_EDITOR
            RaiseError("Google sign-in is only available on Android device builds.");
#else
            RaiseError("Google sign-in is not supported on this platform.");
#endif
        }

        /// <summary>Called from the Android native bridge via UnitySendMessage.</summary>
        public void OnGoogleTokenReceived(string idToken)
        {
#if FIREBASE_AUTH
            if (string.IsNullOrEmpty(idToken)) { RaiseError("Empty Google token."); return; }

            // If already a guest, LINK rather than sign in (preserves UID + progress).
            if (IsAuthenticated && IsGuest)
            {
                LinkGoogleCredential(idToken);
                return;
            }

            var credential = GoogleAuthProvider.GetCredential(idToken, null);
            _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled) { RaiseError("Google sign-in was cancelled."); return; }
                if (task.IsFaulted)
                {
                    RaiseError(FriendlyError(task.Exception));
                    return;
                }
                ApplyUser(task.Result.User);
                Debug.Log($"[Auth] Google sign-in: {FirebaseUid}");
            });
#else
            FirebaseUid = "google_" + idToken.GetHashCode().ToString("X8");
            DisplayName = "Google User";
            IsAuthenticated = true;
            IsGuest = false;
            IsGoogleUser = true;
            PlayerPrefs.SetString(PrefKeyUid, FirebaseUid);
            PlayerPrefs.SetString(PrefKeyAuthMethod, "google");
            PlayerPrefs.Save();
            OnAuthStateChanged?.Invoke(FirebaseUid);
#endif
        }

        /// <summary>Called from the Android native bridge via UnitySendMessage.</summary>
        public void OnGoogleSignInFailed(string error)
        {
            if (string.IsNullOrEmpty(error) || error.Contains("cancel", StringComparison.OrdinalIgnoreCase)
                                             || error.Contains("CANCELED", StringComparison.OrdinalIgnoreCase))
                RaiseError("Google sign-in was cancelled.");
            else
                RaiseError("Google sign-in failed. Check your connection and try again.");
        }

        // ================================================================ guest → google linking

#if FIREBASE_AUTH
        private void LinkGoogleCredential(string idToken)
        {
            var credential = GoogleAuthProvider.GetCredential(idToken, null);
            _user.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled) { RaiseError("Account linking was cancelled."); return; }
                if (task.IsFaulted)
                {
                    // Credential already belongs to another Firebase user.
                    if (IsCredentialConflict(task.Exception))
                    {
                        RaiseError("This Google account is already linked to another player. Sign out first or use a different Google account.");
                        return;
                    }
                    RaiseError(FriendlyError(task.Exception));
                    return;
                }
                // UID stays the same — progress preserved.
                ApplyUser(task.Result.User);
                Debug.Log($"[Auth] Guest linked to Google. UID unchanged: {FirebaseUid}");
            });
        }

        private static bool IsCredentialConflict(AggregateException ex)
        {
            if (ex == null) return false;
            foreach (var inner in ex.Flatten().InnerExceptions)
                if (inner is FirebaseAccountLinkException) return true;
            return false;
        }
#endif

        // ================================================================ sign out

        public void SignOut()
        {
#if FIREBASE_AUTH
            if (_auth != null) _auth.SignOut();
#endif
            ClearState();
            PlayerPrefs.DeleteKey(PrefKeyUid);
            PlayerPrefs.DeleteKey(PrefKeyAuthMethod);
            PlayerPrefs.Save();
        }

        // ================================================================ helpers

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject GetUnityActivity()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
#endif

#if FIREBASE_AUTH
        private static string FriendlyError(AggregateException ex)
        {
            if (ex == null) return "An unknown error occurred.";
            foreach (var inner in ex.Flatten().InnerExceptions)
            {
                if (inner is FirebaseException fe)
                {
                    return fe.ErrorCode switch
                    {
                        (int)AuthError.NetworkRequestFailed => "Network error. Check your connection and try again.",
                        (int)AuthError.TooManyRequests => "Too many attempts. Please wait a moment.",
                        (int)AuthError.UserDisabled => "This account has been disabled.",
                        (int)AuthError.OperationNotAllowed => "This sign-in method is not enabled.",
                        _ => "Authentication failed. Please try again.",
                    };
                }
            }
            return "Authentication failed. Please try again.";
        }
#endif
    }
}
