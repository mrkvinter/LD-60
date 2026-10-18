using Code.Cheats;
using Game.Scripts.Upgrades;
using UnityEngine;

namespace Code.Game.Editor.Cheats
{
    public class UpgradeCheatCategory : IGameCheatCategory
    {
        public void Draw()
        {
            if (CheatServices.ResolveOrDefault(typeof(UpgradeService)) is not UpgradeService upgrades)
            {
                GUILayout.Label("The game is not ready yet.");
                return;
            }

            GUILayout.Label($"Points {upgrades.Points}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 point")) upgrades.AddPoints(1);
            if (GUILayout.Button("+5 points")) upgrades.AddPoints(5);
            if (GUILayout.Button("+25 points")) upgrades.AddPoints(25);
            GUILayout.EndHorizontal();
        }
    }
}
