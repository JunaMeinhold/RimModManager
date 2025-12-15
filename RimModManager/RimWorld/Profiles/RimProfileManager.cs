namespace RimModManager.RimWorld.Profiles
{
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Logging;

    public class RimProfileManager
    {
        private readonly List<RimProfile> profiles = [];

        private static string ProfilesFolder => Paths.ProfilesFolder;

        public RimProfileManager()
        {
            if (!Directory.Exists(ProfilesFolder))
            {
                Directory.CreateDirectory(ProfilesFolder);
            }

            foreach (var profileFile in Directory.EnumerateFiles(ProfilesFolder, "*.xml"))
            {
                try
                {
                    RimProfile profile = RimProfile.Load(profileFile);
                    profile.Name = Path.GetFileNameWithoutExtension(profileFile);
                    profiles.Add(profile);
                }
                catch (Exception ex)
                {
                    LoggerFactory.General.Error($"Failed to load profile '{profileFile}'");
                    LoggerFactory.General.Log(ex);
                }
            }
        }

        public IReadOnlyList<RimProfile> Profiles => profiles;

        public bool Contains(string profileName)
        {
            foreach (var profile in profiles)
            {
                if (profile.Name == profileName)
                {
                    return true;
                }
            }
            return false;
        }

        public void Create(RimLoadOrder config)
        {
            CreateProfileDialog dialog = new(this) { Userdata = config };
            dialog.Show(static (s, r) =>
            {
                if (s is not CreateProfileDialog dialog || r != DialogResult.Ok) return;
                RimProfile profile = new(dialog.ProfileName, (RimLoadOrder)dialog.Userdata!);
                dialog.ProfileManager.profiles.Add(profile);
                try
                {
                    profile.Save(Path.Combine(ProfilesFolder, dialog.ProfileName + ".xml"));
                }
                catch (Exception ex)
                {
                    LoggerFactory.General.Error("Failed to create profile.");
                    LoggerFactory.General.Log(ex);
                }
            });
        }

        public void Delete(RimProfile profile)
        {
            try
            {
                File.Delete(Path.Combine(ProfilesFolder, profile.Name + ".xml"));
                profiles.Remove(profile);
            }
            catch (Exception ex)
            {
                LoggerFactory.General.Error("Failed to delete profile.");
                LoggerFactory.General.Log(ex);
            }
        }

        public void Rename(RimProfile profile)
        {
            RenameProfileDialog dialog = new(this, profile);
            dialog.Show(static (s, r) =>
            {
                if (s is not RenameProfileDialog dialog || r != DialogResult.Ok) return;
                RimProfile profile = dialog.Profile;
                dialog.ProfileManager.profiles.Add(profile);
                string oldPath = Path.Combine(ProfilesFolder, profile.Name + ".xml");
                string newPath = Path.Combine(ProfilesFolder, dialog.NewProfileName + ".xml");
                try
                {
                    File.Move(oldPath, newPath);
                }
                catch (Exception ex)
                {
                    LoggerFactory.General.Error("Failed to rename profile.");
                    LoggerFactory.General.Log(ex);
                }
            });
        }

        public void OpenProfileManager(RimModList mods)
        {
            new ManageProfilesWindow(this, mods).Show();
        }
    }
}