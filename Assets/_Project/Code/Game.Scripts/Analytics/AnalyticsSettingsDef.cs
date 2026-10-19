using System;
using RG.DefinitionSystem.Core;
using UnityEngine;

namespace Game.Scripts.Analytics
{
    [Serializable]
    public sealed class AnalyticsSettingsDef : SingletonDefinition
    {
        public string PostHogHost = "https://eu.i.posthog.com";

        [Tooltip("Project API key: public and write-only, fine to keep in the repo.")]
        public string PostHogApiKey;

        [Tooltip("Off: the editor only logs events to the console, so test runs stay out of the data.")]
        public bool SendFromEditor;
    }
}
