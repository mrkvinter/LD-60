using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using RG.DefinitionSystem.Core;
using RG.DefinitionSystem.UnityAdapter;
using VContainer;

namespace Game.Main.installers
{
    public static class DefInstaller
    {
        public static void Install(IContainerBuilder builder, DefinitionDatabase definitionDatabase)
        {
            var maps = new List<IDefinitionMap>();
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(e => e.GetTypes()).ToList();
            var allDefTypes = types
                .Where(e => e.IsSubclassOf(typeof(Definition)))
                .ToList();

            var defTypes = allDefTypes.Where(e => !e.IsAbstract && !e.IsGenericType).ToList();
            var abstractDefTypes = allDefTypes.Where(e => e.IsAbstract && !e.IsGenericType).ToList();

            var configuratorType = typeof(DefinitionMapConfigurator<>);
            foreach (var defType in defTypes)
            {
                var configurator =
                    (IDefinitionMapConfigurator)Activator.CreateInstance(configuratorType.MakeGenericType(defType));
                var map = configurator.Configure(definitionDatabase.Defs, definitionDatabase.Definitions);
                maps.Add(map);
            }

            foreach (var defType in abstractDefTypes)
            {
                var configurator =
                    (IDefinitionMapConfigurator)Activator.CreateInstance(configuratorType.MakeGenericType(defType));
                var map = configurator.Configure(maps);
                maps.Add(map);
            }

            var definitionResolver = new DefinitionResolver(maps);
            DefManager.Initialize(definitionResolver);
            builder.RegisterInstance(definitionResolver).AsSelf();
        }
    }
}
