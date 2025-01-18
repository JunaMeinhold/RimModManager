namespace RimModManager.RimWorld.Profiles
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.Logging;
    using Hexa.NET.Utilities.Text;
    using System.Numerics;

    public class RimProfileManager
    {
        private readonly List<RimProfile> profiles = [];
        private static readonly string profilesFolder = "Profiles";

        public RimProfileManager()
        {
            if (!Directory.Exists(profilesFolder))
            {
                Directory.CreateDirectory(profilesFolder);
            }

            foreach (var profileFile in Directory.EnumerateFiles(profilesFolder, "*.xml"))
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

        public void Create(ModsConfig config)
        {
            CreateProfileDialog dialog = new(this) { Userdata = config };
            dialog.Show(static (s, r) =>
            {
                if (s is not CreateProfileDialog dialog || r != DialogResult.Ok) return;
                RimProfile profile = new(dialog.ProfileName, (ModsConfig)dialog.Userdata!);
                dialog.ProfileManager.profiles.Add(profile);
                profile.Save(Path.Combine(profilesFolder, dialog.ProfileName + ".xml"));
            });
        }

        public void OpenProfileManager(RimModList mods)
        {
            WidgetManager.Register(new ManageProfilesWindow(this, mods), true);
        }
    }

    public class ManageProfilesWindow : ImWindow
    {
        private readonly RimProfileManager profileManager;
        private readonly RimModList mods;
        private RimProfile? selectedProfile;
        private float split = 200;

        public ManageProfilesWindow(RimProfileManager profileManager, RimModList mods)
        {
            this.profileManager = profileManager;
            this.mods = mods;
        }

        protected override string Name { get; } = "Manage Profiles";

        public override unsafe void DrawContent()
        {
            ImGui.BeginChild("##SidePanel", new Vector2(split, 0));
            var avail = ImGui.GetContentRegionAvail();
            if (ImGui.BeginListBox("##ProfilesList"u8, avail))
            {
                foreach (var profile in profileManager.Profiles)
                {
                    if (ImGui.Selectable(profile.Name, selectedProfile == profile))
                    {
                        selectedProfile = profile;
                        selectedProfile.PopulateList(mods);
                    }
                }
                ImGui.EndListBox();
            }
            ImGui.EndChild();

            ImGuiSplitter.VerticalSplitter("Splitter"u8, ref split);

            ImGui.BeginChild("##MainPanel");

            if (selectedProfile != null)
            {
                ImGuiManager.PushFont("FA");

                byte* buffer = stackalloc byte[2048];
                StrBuilder builder = new(buffer, 2048);

                ImGui.Text("Name:"u8);
                ImGui.SameLine();
                ImGui.Text(selectedProfile.Name);
                ImGui.Text("Game Version:"u8);
                ImGui.SameLine();
                ImGui.Text(selectedProfile.Version);
                ImGui.Separator();

                float lineHeight = ImGui.GetTextLineHeightWithSpacing();

                if (ImGui.BeginTable("##Mods"u8, 2, ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable, new Vector2(0, -lineHeight)))
                {
                    ImGui.TableSetupColumn("Index"u8);
                    ImGui.TableSetupColumn("Name"u8);

                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < selectedProfile.ActiveMods.Count; i++)
                    {
                        RimMod mod = selectedProfile.ActiveMods[i];
                        ImGui.TableNextRow();
                        ImGui.TableSetColumnIndex(0);
                        builder.Reset();
                        builder.Append(i);
                        builder.End();
                        ImGui.Text(builder);

                        ImGui.TableSetColumnIndex(1);
                        var width = ImGui.GetContentRegionAvail().X;
                        builder.Reset();
                        builder.Append(mod.GetIcon());
                        builder.End();
                        ImGui.TextColored(mod.GetIconColor(), builder);

                        ImGui.SameLine();

                        ImGui.Selectable(mod.Name);

                        bool hovered = ImGui.IsItemHovered();

                        bool messagesHovered = mod.DrawMessages(builder, hovered, width);

                        if (hovered && !messagesHovered)
                        {
                            mod.DrawTooltip(builder);
                        }
                    }

                    ImGui.EndTable();
                }

                selectedProfile.Messages.DrawBar(builder);

                ImGuiManager.PopFont();
            }

            ImGui.EndChild();
        }
    }
}