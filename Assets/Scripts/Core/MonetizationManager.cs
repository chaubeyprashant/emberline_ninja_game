using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using Unity.Services.Core;

namespace Emberline.Core
{
    public class MonetizationManager : MonoBehaviour, IDetailedStoreListener
    {
        private static MonetizationManager _instance;
        public static MonetizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MonetizationManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MonetizationManager");
                        _instance = go.AddComponent<MonetizationManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            var _ = Instance;
        }

        [Header("AdMob units")]
        [SerializeField] private string androidRewardedAdUnit = "ca-app-pub-2773109772362951/8814201034";
        [SerializeField] private string iosRewardedAdUnit = "ca-app-pub-3940256099942544/1712485313";
        [SerializeField] private string androidInterstitialAdUnit = "ca-app-pub-3940256099942544/1033173712";
        [SerializeField] private string iosInterstitialAdUnit = "ca-app-pub-3940256099942544/4411468910";

        private RewardedAd _rewardedAd;
        private InterstitialAd _interstitialAd;
        private bool _rewardedLoading;
        private bool _interstitialLoading;
        private volatile bool _rewardEarned;
        private bool _rewardSettled;

        // Load retries: 2, 4, 8 ... 60 s, reset on a successful load. One failed
        // request used to leave the button dead until the next ad was shown.
        private int _rewardedAttempt, _interstitialAttempt;

        // A full-screen ad owns the game: time stops and the score goes quiet,
        // and both come back exactly as they were when it closes.
        private float _savedTimeScale = 1f;
        private bool _savedAudioPause, _inFullScreen;
        private float _lastFullScreenAt = -9999f;
        private int _missionsSinceInterstitial;
        private string _rewardedPlacement = "rewarded";

        /// <summary>Interstitial pacing: every Nth finished mission, never within this many seconds of any ad.</summary>
        public const int MissionsPerInterstitial = 3;
        public const float InterstitialCooldown = 180f;

        private Action _onRewardedAdComplete;
        private Action _onRewardedAdFailure;
        private Action _onInterstitialAdComplete;
        private readonly Queue<Action> _mainThread = new Queue<Action>();

        [Header("IAP Settings")]
        public const string RemoveAdsId = "remove_ads";
        public const string Coins100Id = "100_coins";

        private IStoreController _storeController;
        private IExtensionProvider _storeExtensionProvider;

        public bool HasRemovedAds
        {
            get => PlayerPrefs.GetInt("RemoveAds", 0) == 1;
            private set
            {
                PlayerPrefs.SetInt("RemoveAds", value ? 1 : 0);
                PlayerPrefs.Save();
                if (!value) return;
                _interstitialAd?.Destroy();
                _interstitialAd = null;
            }
        }

        /// <summary>A rewarded ad can be shown right now. The HUD greys "(AD)" buttons while it is false.</summary>
        public bool RewardedReady
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return _rewardedAd != null && _rewardedAd.CanShowAd();
#endif
            }
        }

        private async void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize ads synchronously on boot without awaiting cloud services
            InitializeAds();

            try
            {
                await UnityServices.InitializeAsync();
            }
            catch (Exception e)
            {
                // The usual cause is a project not linked to a Unity Cloud project
                // (Project Settings > Services). AdMob does not need it; purchasing
                // still starts below, but link the project before a store build.
                Debug.LogWarning($"[MonetizationManager] Unity Services did not start ({e.GetType().Name}): {e.Message}");
            }

            InitializePurchasing();
        }

        private void Update()
        {
            lock (_mainThread)
            {
                while (_mainThread.Count > 0)
                    _mainThread.Dequeue()?.Invoke();
            }
        }

        private void OnMainThread(Action action)
        {
            lock (_mainThread) _mainThread.Enqueue(action);
        }

        #region Ads Integration

        private string RewardedAdUnit =>
            Application.platform == RuntimePlatform.IPhonePlayer ? iosRewardedAdUnit : androidRewardedAdUnit;

        private string InterstitialAdUnit =>
            Application.platform == RuntimePlatform.IPhonePlayer ? iosInterstitialAdUnit : androidInterstitialAdUnit;

        /// <summary>True when European privacy rules require a way to change the ad choice.</summary>
        public bool PrivacyOptionsRequired
        {
            get
            {
#if UNITY_EDITOR
                return false;
#else
                return ConsentInformation.PrivacyOptionsRequirementStatus ==
                       PrivacyOptionsRequirementStatus.Required;
#endif
            }
        }

        public void ShowPrivacyOptions()
        {
#if !UNITY_EDITOR
            ConsentForm.ShowPrivacyOptionsForm(error =>
            {
                OnMainThread(() =>
                {
                    if (error != null)
                        Debug.LogWarning($"[MonetizationManager] Privacy options: {error.Message}");
                });
            });
#endif
        }

        private void InitializeAds()
        {
#if UNITY_EDITOR
            Debug.Log("[MonetizationManager] AdMob is not shown in the Editor. Rewarded buttons grant the reward so the flow can be tested.");
#else
            Debug.Log("[MonetizationManager] Requesting ad consent.");
            var request = new ConsentRequestParameters
            {
                TagForUnderAgeOfConsent = false,
            };
#if DEVELOPMENT_BUILD
            // Test switches, development builds only. A file ads_debug.txt in the
            // app's files folder, one directive per line:
            //   eea              treat this phone as being in the EU (consent form shows)
            //   reset            forget the stored consent choice first
            //   device <id>      a test device: test ads only, never live ones. The id
            //                    is printed in logcat by the SDK on first launch.
            var debug = ReadAdsDebug();
            if (debug.reset) ConsentInformation.Reset();
            if (debug.eea || debug.devices.Count > 0)
                request.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = debug.eea ? DebugGeography.EEA : DebugGeography.Disabled,
                    TestDeviceHashedIds = debug.devices,
                };
            if (debug.devices.Count > 0)
                MobileAds.SetRequestConfiguration(new RequestConfiguration { TestDeviceIds = debug.devices });
            Debug.Log($"[MonetizationManager] ads debug: eea={debug.eea} reset={debug.reset} devices={debug.devices.Count}");
#endif
            ConsentInformation.Update(request, updateError =>
            {
                OnMainThread(() =>
                {
                    if (updateError != null)
                        Debug.LogWarning($"[MonetizationManager] Consent info: {updateError.Message}");

                    ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                    {
                        OnMainThread(() =>
                        {
                            if (formError != null)
                                Debug.LogWarning($"[MonetizationManager] Consent form: {formError.Message}");
                            if (ConsentInformation.CanRequestAds())
                                StartMobileAds();
                            else
                                Debug.LogWarning("[MonetizationManager] The consent choice does not allow ads.");
                        });
                    });
                });
            });
#endif
        }

#if DEVELOPMENT_BUILD
        private static (bool eea, bool reset, List<string> devices) ReadAdsDebug()
        {
            var devices = new List<string>();
            bool eea = false, reset = false;
            var path = System.IO.Path.Combine(Application.persistentDataPath, "ads_debug.txt");
            if (!System.IO.File.Exists(path)) return (false, false, devices);
            foreach (var raw in System.IO.File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line == "eea") eea = true;
                else if (line == "reset") reset = true;
                else if (line.StartsWith("device ")) devices.Add(line.Substring(7).Trim());
            }
            return (eea, reset, devices);
        }
#endif

        private void StartMobileAds()
        {
            if (RewardedAdUnit.StartsWith("ca-app-pub-3940256099942544"))
                Debug.LogError("[MonetizationManager] AdMob is still on Google's sample units. These ads pay nothing.");

            Debug.Log("[MonetizationManager] Initializing AdMob.");
            MobileAds.Initialize(status =>
            {
                OnMainThread(() =>
                {
                    if (status == null)
                    {
                        Debug.LogError("[MonetizationManager] AdMob initialization failed.");
                        return;
                    }

                    Debug.Log("[MonetizationManager] AdMob initialization complete. Loading ads.");
                    LoadRewardedAd();
                    LoadInterstitialAd();
                });
            });
        }

        public void LoadInterstitialAd()
        {
            // No live interstitial unit yet. Do not request Google's sample unit
            // from the production app id.
            if (HasRemovedAds || _interstitialLoading || InterstitialAdUnit.StartsWith("ca-app-pub-3940256099942544"))
                return;
            _interstitialLoading = true;
            var previous = _interstitialAd;
            _interstitialAd = null;
            previous?.Destroy();

            InterstitialAd.Load(InterstitialAdUnit, new AdRequest(), (ad, error) =>
            {
                OnMainThread(() =>
                {
                    _interstitialLoading = false;
                    if (error != null || ad == null)
                    {
                        Debug.LogError($"[MonetizationManager] Interstitial failed to load: {error}");
                        RetryLater(LoadInterstitialAd, ref _interstitialAttempt);
                        return;
                    }

                    _interstitialAttempt = 0;
                    _interstitialAd = ad;
                    Track(ad, "interstitial");
                    Debug.Log("[MonetizationManager] Interstitial loaded.");
                });
            });
        }

        public void LoadRewardedAd()
        {
            if (_rewardedLoading) return;
            _rewardedLoading = true;
            var previous = _rewardedAd;
            _rewardedAd = null;
            previous?.Destroy();

            RewardedAd.Load(RewardedAdUnit, new AdRequest(), (ad, error) =>
            {
                OnMainThread(() =>
                {
                    _rewardedLoading = false;
                    if (error != null || ad == null)
                    {
                        Debug.LogError($"[MonetizationManager] Rewarded ad failed to load: {error}");
                        RetryLater(LoadRewardedAd, ref _rewardedAttempt);
                        return;
                    }

                    _rewardedAttempt = 0;
                    _rewardedAd = ad;
                    ad.OnAdPaid += v => OnMainThread(() => LogPaid(_rewardedPlacement, v));
                    ad.OnAdImpressionRecorded += () => OnMainThread(() =>
                        Debug.Log($"[ADS] impression placement={_rewardedPlacement}"));
                    Debug.Log("[MonetizationManager] Rewarded ad loaded.");
                });
            });
        }

        public void ShowInterstitialAd(Action onComplete = null)
        {
            if (HasRemovedAds || _interstitialAd == null || !_interstitialAd.CanShowAd())
            {
                onComplete?.Invoke();
                if (!HasRemovedAds) LoadInterstitialAd();
                return;
            }

            _onInterstitialAdComplete = onComplete;
            var ad = _interstitialAd;
            BeginFullScreen();
            ad.OnAdFullScreenContentFailed += error =>
            {
                OnMainThread(() =>
                {
                    Debug.LogError($"[MonetizationManager] Interstitial failed to show: {error}");
                    FinishInterstitial();
                });
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                OnMainThread(FinishInterstitial);
            };
            ad.Show();
        }

        public void ShowRewardedAd(Action onRewardSuccess, Action onFailedOrSkipped = null,
            string placement = "rewarded")
        {
            _rewardedPlacement = placement;
#if UNITY_EDITOR
            Debug.Log("[MonetizationManager] Editor rewarded ad simulated.");
            onRewardSuccess?.Invoke();
#else
            Debug.Log($"[MonetizationManager] ShowRewardedAd. ready: {_rewardedAd != null && _rewardedAd.CanShowAd()}, unit: {RewardedAdUnit}");
            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
            {
                Debug.LogWarning("[MonetizationManager] Rewarded ad is not ready.");
                onFailedOrSkipped?.Invoke();
                LoadRewardedAd();
                return;
            }

            _onRewardedAdComplete = onRewardSuccess;
            _onRewardedAdFailure = onFailedOrSkipped;
            _rewardEarned = false;
            _rewardSettled = false;
            var ad = _rewardedAd;
            BeginFullScreen();
            ad.OnAdFullScreenContentFailed += error =>
            {
                OnMainThread(() =>
                {
                    Debug.LogError($"[MonetizationManager] Rewarded ad failed to show: {error}");
                    FinishRewarded();
                });
            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                OnMainThread(FinishRewarded);
            };
            ad.Show(_ => { _rewardEarned = true; });
#endif
        }

        private void FinishRewarded()
        {
            if (_rewardSettled) return;
            _rewardSettled = true;
            EndFullScreen();
            var success = _onRewardedAdComplete;
            var failure = _onRewardedAdFailure;
            _onRewardedAdComplete = null;
            _onRewardedAdFailure = null;
            if (_rewardEarned) success?.Invoke();
            else failure?.Invoke();
            _rewardEarned = false;
            LoadRewardedAd();
        }

        private void FinishInterstitial()
        {
            EndFullScreen();
            var done = _onInterstitialAdComplete;
            _onInterstitialAdComplete = null;
            done?.Invoke();
            LoadInterstitialAd();
        }

        /// <summary>Call when a mission or duel is won; drives interstitial pacing.</summary>
        public void OnMissionComplete() => _missionsSinceInterstitial++;

        /// <summary>
        /// Shows an interstitial if pacing allows, then continues with
        /// <paramref name="next"/> either way. Never with Remove Ads, never within
        /// <see cref="InterstitialCooldown"/> s of any ad, at most every
        /// <see cref="MissionsPerInterstitial"/>th finished mission, and only
        /// where the caller says it is fair (<paramref name="eligible"/>: not the
        /// first missions, not mid-fight).
        /// </summary>
        public void ShowPacedInterstitial(Action next, bool eligible)
        {
            var due = eligible && !HasRemovedAds
                      && _missionsSinceInterstitial >= MissionsPerInterstitial
                      && Time.realtimeSinceStartup - _lastFullScreenAt >= InterstitialCooldown
                      && _interstitialAd != null && _interstitialAd.CanShowAd();
            if (!due) { next?.Invoke(); return; }
            _missionsSinceInterstitial = 0;
            ShowInterstitialAd(next);
        }

        private void BeginFullScreen()
        {
            if (_inFullScreen) return;
            _inFullScreen = true;
            _savedTimeScale = Time.timeScale;
            _savedAudioPause = AudioListener.pause;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        private void EndFullScreen()
        {
            if (!_inFullScreen) return;
            _inFullScreen = false;
            Time.timeScale = _savedTimeScale;
            AudioListener.pause = _savedAudioPause;
            _lastFullScreenAt = Time.realtimeSinceStartup;
        }

        private void RetryLater(Action load, ref int attempt)
        {
            attempt = Mathf.Min(attempt + 1, 6);
            var delay = Mathf.Min(60f, Mathf.Pow(2f, attempt));
            StartCoroutine(After(delay, load));
        }

        private static System.Collections.IEnumerator After(float seconds, Action then)
        {
            // Realtime: the defeat card stops the clock, and a retry must still fire.
            yield return new WaitForSecondsRealtime(seconds);
            then?.Invoke();
        }

        private void Track(InterstitialAd ad, string placement)
        {
            ad.OnAdPaid += v => OnMainThread(() => LogPaid(placement, v));
            ad.OnAdImpressionRecorded += () => OnMainThread(() => Debug.Log($"[ADS] impression placement={placement}"));
        }

        /// <summary>One logcat line per paid impression, so each placement's earnings can be read off a test.</summary>
        private static void LogPaid(string placement, AdValue v) =>
            Debug.Log($"[ADS] paid placement={placement} micros={v.Value} currency={v.CurrencyCode} precision={v.Precision}");

        #endregion

        #region IAP Integration

        private void InitializePurchasing()
        {
            if (IsInitialized()) return;

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            builder.AddProduct(RemoveAdsId, ProductType.NonConsumable);
            builder.AddProduct(Coins100Id, ProductType.Consumable);

            UnityPurchasing.Initialize(this, builder);
        }

        private bool IsInitialized()
        {
            return _storeController != null && _storeExtensionProvider != null;
        }

        public void BuyRemoveAds()
        {
            BuyProductID(RemoveAdsId);
        }

        public void BuyCoins()
        {
            BuyProductID(Coins100Id);
        }

        private void BuyProductID(string productId)
        {
            if (IsInitialized())
            {
                Product product = _storeController.products.WithID(productId);

                if (product != null && product.availableToPurchase)
                {
                    Debug.Log($"Purchasing product asynchronously: '{product.definition.id}'");
                    _storeController.InitiatePurchase(product);
                }
                else
                {
                    Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase.");
                }
            }
        }
        
        public void RestorePurchases()
        {
            if (!IsInitialized()) return;
            
            if (Application.platform == RuntimePlatform.IPhonePlayer || 
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                Debug.Log("RestorePurchases started ...");
                
                var apple = _storeExtensionProvider.GetExtension<IAppleExtensions>();
                apple.RestoreTransactions((result, message) => {
                    Debug.Log($"RestorePurchases continuing: {result}. If no further messages, no purchases available to restore. Message: {message}");
                });
            }
            else if (Application.platform == RuntimePlatform.Android)
            {
                // Google Play also restores owned items on initialisation; this is
                // the explicit path behind a "Restore purchases" button.
                _storeExtensionProvider.GetExtension<IGooglePlayStoreExtensions>()
                    .RestoreTransactions((ok, message) =>
                        Debug.Log($"RestorePurchases (Google Play): {ok} {message}"));
            }
            else
            {
                Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
            }
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _storeExtensionProvider = extensions;
            // The receipt is the truth, not PlayerPrefs: a reinstall or a new phone
            // gets Remove Ads back as soon as the store answers.
            var removeAds = controller.products.WithID(RemoveAdsId);
            if (removeAds != null && removeAds.hasReceipt && !HasRemovedAds)
            {
                Debug.Log("[MonetizationManager] Remove Ads restored from the store receipt.");
                HasRemovedAds = true;
            }
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"IAP Initialization Failed: {error}. Message: {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (String.Equals(args.purchasedProduct.definition.id, RemoveAdsId, StringComparison.Ordinal))
            {
                Debug.Log("Purchase Successful: Remove Ads");
                HasRemovedAds = true;
            }
            else if (String.Equals(args.purchasedProduct.definition.id, Coins100Id, StringComparison.Ordinal))
            {
                Debug.Log("Purchase Successful: 100 Coins");
                Wallet.Earn(100);
            }
            else
            {
                Debug.Log($"ProcessPurchase: FAIL. Unrecognized product: '{args.purchasedProduct.definition.id}'");
            }

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError($"Purchase failed - Product: '{product.definition.id}', PurchaseFailureReason: {failureReason}");
        }
        
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogError($"Purchase failed - Product: '{product.definition.id}', FailureDescription: {failureDescription.message}");
        }
        
        #endregion
    }
}
