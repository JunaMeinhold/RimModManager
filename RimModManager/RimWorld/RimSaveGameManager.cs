namespace RimModManager.RimWorld
{
    using Hexa.NET.Logging;
    using System.Collections.Generic;

    public class RimSaveGameManager
    {
        private readonly List<RimSaveGame> saveGames = [];

        public IReadOnlyList<RimSaveGame> SaveGames => saveGames;

        public void Load(RimModManagerConfig config, RimModList mods)
        {
            saveGames.Clear();
            string saveGameFolder = Path.Combine(Path.GetDirectoryName(config.GameConfigFolder!)!, "Saves");
            if (!Directory.Exists(saveGameFolder)) return;

            foreach (var saveGamePath in Directory.GetDirectories(saveGameFolder))
            {
                try
                {
                    RimSaveGame saveGame = RimSaveGame.Load(saveGamePath, mods);
                    saveGames.Add(saveGame);
                }
                catch (Exception ex)
                {
                    LoggerFactory.General.Error("Failed to load save game.");
                    LoggerFactory.General.Log(ex);
                }
            }

            saveGames.Sort(RimSaveGameComparer.Instance);
        }
    }
}