using Game.Scripts.Analytics;
using RG.DefinitionSystem.Core;
using VContainer;

namespace Game.Main.installers
{
    public static class AnalyticsInstaller
    {
#if UNITY_EDITOR
        private const string Source = "editor";
#else
        private const string Source = "build";
#endif

        public static void Install(IContainerBuilder builder)
        {
            builder.Register<IAnalytics>(_ =>
            {
                var settings = DefManager.GetSingletonDef<AnalyticsSettingsDef>();
#if UNITY_EDITOR
                if (!settings.SendFromEditor) return new DebugAnalytics();
#endif
                return new PostHogAnalytics(settings.PostHogHost, settings.PostHogApiKey, Source);
            }, Lifetime.Singleton);
        }
    }
}
