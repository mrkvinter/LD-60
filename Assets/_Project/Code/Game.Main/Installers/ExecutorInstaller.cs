using System;
using System.Linq;
using Code.Game.Core.ExecutorSystem;
using Code.Game.Core.ExecutorSystem.Conditions;
using VContainer;

namespace Game.Main.installers
{
    public sealed class ExecutorInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            BindActionExecutorSystem(builder);
        }
        
        private static void BindActionExecutorSystem(IContainerBuilder builder)
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(e => e.GetTypes()).ToList();
            var types = allTypes
                .Where(e => e.IsSubclassOf(typeof(BaseExecutor)) && !e.IsAbstract && !e.IsGenericType)
                .ToList();

            foreach (var executorType in types)
            {
                builder.Register(executorType, Lifetime.Transient).As<BaseExecutor>();
            }

            builder.Register<ActionExecutorSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<ConditionService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}