package com.ergebins.emberline3d;

import android.app.Activity;
import android.os.CancellationSignal;
import android.util.Log;

import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.CustomCredential;
import androidx.credentials.Credential;

import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

import com.unity3d.player.UnityPlayer;

import java.util.concurrent.Executors;

/**
 * Minimal bridge between Unity and Android Credential Manager for Google Sign-In.
 *
 * Unity calls {@link #signIn(Activity)} via JNI. On success, the Google ID token
 * is delivered to AuthManager.OnGoogleTokenReceived via UnitySendMessage. On failure,
 * AuthManager.OnGoogleSignInFailed is called instead.
 *
 * NOTE: Replace WEB_CLIENT_ID with your Firebase project's Web Client ID from
 * Firebase Console → Authentication → Sign-in method → Google → Web client ID.
 */
public class GoogleCredentialBridge {

    private static final String TAG = "GoogleCredentialBridge";

    // TODO: Replace with your Firebase Web Client ID.
    // Firebase Console → Authentication → Sign-in method → Google → Web client ID.
    private static final String WEB_CLIENT_ID = "925675040622-f5g654n6dkvfhjupeau2q4sdkpfj8g8c.apps.googleusercontent.com";

    private static final String UNITY_GAME_OBJECT = "AuthManager";

    public static void signIn(Activity activity) {
        if (activity == null) {
            sendFailure("Activity is null");
            return;
        }

        try {
            GetSignInWithGoogleOption googleOption = new GetSignInWithGoogleOption.Builder(WEB_CLIENT_ID)
                    .build();

            GetCredentialRequest request = new GetCredentialRequest.Builder()
                    .addCredentialOption(googleOption)
                    .build();

            CredentialManager credentialManager = CredentialManager.create(activity);

            credentialManager.getCredentialAsync(
                    activity,
                    request,
                    new CancellationSignal(),
                    Executors.newSingleThreadExecutor(),
                    new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                        @Override
                        public void onResult(GetCredentialResponse response) {
                            handleSignInResponse(response);
                        }

                        @Override
                        public void onError(GetCredentialException e) {
                            Log.w(TAG, "Credential Manager error: " + e.getType() + " - " + e.getMessage(), e);
                            sendFailure("Google sign-in failed. Error: " + e.getType() + " - " + e.getMessage());
                        }
                    }
            );
        } catch (Exception e) {
            Log.e(TAG, "Failed to start Credential Manager", e);
            sendFailure(e.getMessage());
        }
    }

    private static void handleSignInResponse(GetCredentialResponse response) {
        try {
            Credential credential = response.getCredential();

            if (credential instanceof CustomCredential &&
                credential.getType().equals(GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL)) {

                CustomCredential customCredential = (CustomCredential) credential;
                GoogleIdTokenCredential googleCredential = GoogleIdTokenCredential.createFrom(customCredential.getData());
                String idToken = googleCredential.getIdToken();

                if (idToken != null && !idToken.isEmpty()) {
                    Log.d(TAG, "Google ID token obtained");
                    UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, "OnGoogleTokenReceived", idToken);
                } else {
                    sendFailure("Empty ID token from Google");
                }
            } else {
                sendFailure("Unexpected credential type: " + credential.getClass().getName());
            }
        } catch (Exception e) {
            Log.e(TAG, "Error processing credential response", e);
            sendFailure(e.getMessage());
        }
    }

    private static void sendFailure(String message) {
        UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, "OnGoogleSignInFailed",
                message != null ? message : "Unknown error");
    }
}
