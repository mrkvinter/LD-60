using Code.Cheats;
using Code.Game.Scripts.EntitySystem;
using Game.Scripts.Construction;
using UnityEngine;

namespace Code.Game.Editor.Cheats
{
    public class ConstructionCheatCategory : IGameCheatCategory
    {
        public void Draw()
        {
            if (CheatServices.ResolveOrDefault(typeof(EntityService)) is not EntityService entities)
            {
                GUILayout.Label("The game is not ready yet.");
                return;
            }

            var built = 0;
            var total = 0;
            foreach (var entity in entities.Entities)
            {
                if (entity is not ConstructionSite site) continue;

                total++;
                if (site.IsBuilt) built++;
            }

            GUILayout.Label($"Built {built} / {total}");

            if (GUILayout.Button("Build everything")) ConstructionCheats.BuildAll();
        }
    }

    public static class ConstructionCheats
    {
        public static void BuildAll()
        {
            foreach (var entity in CheatServices.Resolve<EntityService>().Entities)
            {
                if (entity is ConstructionSite site) site.Complete();
            }
        }
    }
}
