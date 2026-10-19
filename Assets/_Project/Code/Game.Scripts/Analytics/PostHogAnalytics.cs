using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Game.Scripts.Analytics
{
    public sealed class PostHogAnalytics : IAnalytics, IDisposable
    {
        private const int FlushThreshold = 20;
        private const int FlushIntervalSeconds = 10;
        private const double QuitFlushTimeoutSeconds = 1.5;
        private const string UserIdKey = "AnalyticsUserId";

        private readonly string batchUrl;
        private readonly string apiKey;
        private readonly string source;
        private readonly string userId;
        private readonly string sessionId = Guid.NewGuid().ToString();
        private readonly List<Dictionary<string, object>> pending = new();
        private readonly List<UnityWebRequest> inFlight = new();

        private bool quitting;
        private bool disposed;

        public PostHogAnalytics(string host, string apiKey, string source)
        {
            this.apiKey = apiKey;
            this.source = source;
            batchUrl = string.IsNullOrEmpty(host) ? null : $"{host.TrimEnd('/')}/batch/";
            userId = LoadUserId();

            if (!IsConfigured)
            {
                Debug.LogWarning($"[PostHog] Host or API key is not set in {nameof(AnalyticsSettingsDef)}, events are dropped.");
                return;
            }

            Application.quitting += OnQuitting;
            Application.focusChanged += OnFocusChanged;
            FlushLoop().Forget();
        }

        private bool IsConfigured => !string.IsNullOrEmpty(batchUrl) && !string.IsNullOrEmpty(apiKey);

        public void Track(string eventName, Dictionary<string, object> properties)
        {
            if (!IsConfigured || disposed) return;

            var payload = properties != null ? new Dictionary<string, object>(properties) : new Dictionary<string, object>();
            payload["source"] = source;
            payload["game_version"] = Application.version;
            payload["platform"] = Application.platform.ToString();
            payload["session_id"] = sessionId;
            payload["$process_person_profile"] = false;

            pending.Add(new Dictionary<string, object>
            {
                ["event"] = eventName,
                ["distinct_id"] = userId,
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["properties"] = payload
            });

            if (pending.Count >= FlushThreshold) Flush();
        }

        public void Flush()
        {
            if (quitting) SendBlocking();
            else Send().Forget();
        }

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
            Application.quitting -= OnQuitting;
            Application.focusChanged -= OnFocusChanged;
        }

        private static string LoadUserId()
        {
            var id = PlayerPrefs.GetString(UserIdKey, string.Empty);
            if (!string.IsNullOrEmpty(id)) return id;

            id = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(UserIdKey, id);
            PlayerPrefs.Save();
            return id;
        }

        private async UniTaskVoid FlushLoop()
        {
            while (!disposed)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(FlushIntervalSeconds), DelayType.UnscaledDeltaTime);
                await Send();
            }
        }

        private async UniTask Send()
        {
            var request = StartRequest();
            if (request == null) return;

            try
            {
                await request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[PostHog] Send failed: {request.responseCode} {request.error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PostHog] Send failed: {e.Message}");
            }
            finally
            {
                inFlight.Remove(request);
                request.Dispose();
            }
        }

        // Unity does not wait for requests on exit and Send's continuations never run after quitting, so the main
        // thread waits for them here. A browser cannot block, so on WebGL the queue goes out when the page loses focus.
        private void SendBlocking()
        {
            var request = StartRequest();
            request?.SendWebRequest();

#if !UNITY_WEBGL || UNITY_EDITOR
            var elapsed = Stopwatch.StartNew();
            while (inFlight.Exists(r => !r.isDone) && elapsed.Elapsed.TotalSeconds < QuitFlushTimeoutSeconds)
            {
                Thread.Sleep(10);
            }
#endif
        }

        private UnityWebRequest StartRequest()
        {
            if (pending.Count == 0) return null;

            var batch = new List<Dictionary<string, object>>(pending);
            pending.Clear();

            var body = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                ["api_key"] = apiKey,
                ["batch"] = batch
            });

            var request = new UnityWebRequest(batchUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            inFlight.Add(request);
            return request;
        }

        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus) Flush();
        }

        private void OnQuitting()
        {
            quitting = true;
            Flush();
        }
    }
}
