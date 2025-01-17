namespace RimModManager.TextureOptimizer
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.Logging;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities.Text;
    using RimModManager.RimWorld;
    using System.Numerics;

    public class OptimizeTexturesWindow : ImWindow
    {
        private readonly RimModManagerConfig config;

        private bool scrollToEnd = false;

        private readonly TextureProcessor processor = new();

        private readonly Lock _lock = new();
        private readonly List<LogMessage> messages = [];

        protected override string Name { get; } = "Optimize Textures";

        public OptimizeTexturesWindow(RimModManagerConfig config)
        {
            this.config = config;
        }

        public override void Init()
        {
            processor.LogMessage += OnLogMessage;
            processor.StartWorkers();
        }

        public override void Dispose()
        {
            processor.StopWorkers();
        }

        private void OnLogMessage(LogMessage message)
        {
            lock (_lock)
            {
                if (messages.Count > 10000)
                {
                    messages.RemoveAt(0);
                }

                messages.Add(message);
                scrollToEnd = true;
            }
        }

        public override unsafe void DrawContent()
        {
            ImGui.BeginDisabled(processor.IsProcessing);

            if (ImGui.Button("Convert"u8))
            {
                if (config.CheckPaths())
                {
                    processor.ProcessPaths(config.LocalModsFolder!, config.SteamModFolder!);
                }
            }

            ImGui.EndDisabled();

            if (processor.IsProcessing)
            {
                ImGui.SameLine();
                if (ImGui.Button("Cancel"u8))
                {
                    processor.Cancel();
                }
            }

            ImGui.SeparatorText("Options");

            ImGui.BeginDisabled(processor.IsProcessing);

            var bc7quick = processor.BC7Quick;
            if (ImGui.Checkbox("BC7 Quick"u8, ref bc7quick))
            {
                processor.BC7Quick = bc7quick;
            }

            ImGui.BeginDisabled(true);
            var generateMips = processor.GenerateMips;
            if (ImGui.Checkbox("Generate Mips"u8, ref generateMips))
            {
                processor.GenerateMips = generateMips;
            }
            ImGui.EndDisabled();

            var overwriteFiles = processor.OverwriteFiles;
            if (ImGui.Checkbox("Overwrite Files"u8, ref overwriteFiles))
            {
                processor.OverwriteFiles = overwriteFiles;
            }

            var upscaleTextures = processor.UpscaleTextures;
            if (ImGui.Checkbox("Upscale Textures", ref upscaleTextures))
            {
                processor.UpscaleTextures = upscaleTextures;
            }

            var minSize = processor.MinSize;
            if (upscaleTextures && ImGui.InputInt("Min Size", ref minSize))
            {
                processor.MinSize = minSize;
            }

            var downscaleTextures = processor.DownscaleTextures;
            if (ImGui.Checkbox("Downscale Textures", ref downscaleTextures))
            {
                processor.DownscaleTextures = downscaleTextures;
            }

            var maxSize = processor.MaxSize;
            if (downscaleTextures && ImGui.InputInt("Max Size", ref maxSize))
            {
                processor.MaxSize = maxSize;
            }

            ImGui.EndDisabled();

            /*
            var batchSize = processor.BatchSize;
            if (ImGui.InputInt("Batch Size"u8, ref batchSize))
            {
                processor.BatchSize = batchSize;
            }
            */

            if (processor.Total != 0)
            {
                byte* buffer = stackalloc byte[1024];
                StrBuilder builder = new(buffer, 1024);
                builder.Append(processor.Processed);
                builder.Append("/"u8);
                builder.Append(processor.Total);
                builder.End();
                ImGui.Text(builder);
                float progress = processor.Processed / (float)processor.Total;
                ImGui.ProgressBar(progress, new Vector2(400, 0));
            }

            ImGui.SeparatorText("Logs");

            DisplayLogs();

            //processor.ProcessTextures();
        }

        private unsafe void DisplayLogs()
        {
            if (ImGui.Button("Clear Logs"u8))
            {
                messages.Clear();
            }
            lock (_lock)
            {
                if (ImGui.BeginChild("LogContent", ImGuiChildFlags.FrameStyle))
                {
                    var avail = ImGui.GetContentRegionAvail();
                    var lineHeight = ImGui.GetTextLineHeightWithSpacing();
                    var scroll = ImGui.GetScrollY();

                    int start = (int)Math.Floor(scroll / lineHeight);
                    int end = (int)Math.Ceiling(avail.Y / lineHeight) + start;

                    start = Math.Max(start, 0);
                    end = Math.Min(end, messages.Count);

                    if (start > 0)
                    {
                        ImGui.Dummy(new(1, start * lineHeight));
                    }

                    for (int i = start; i < end; i++)
                    {
                        var message = messages[i];
                        ImGui.TextColored(GetColor(message.Severity), message.Message);
                    }

                    int delta = messages.Count - end;
                    if (delta > 0)
                    {
                        ImGui.Dummy(new(1, delta * lineHeight));
                    }

                    if (scrollToEnd)
                    {
                        ImGui.SetScrollHereY(1);
                        scrollToEnd = false;
                    }

                    ImGui.EndChild();
                }
            }
        }

        private static Vector4 GetColor(LogSeverity severity)
        {
            return severity switch
            {
                LogSeverity.Trace => Colors.Gray,
                LogSeverity.Debug => Colors.Gray,
                LogSeverity.Info => Colors.White,
                LogSeverity.Warning => Colors.Yellow,
                LogSeverity.Error => Colors.Red,
                LogSeverity.Critical => Colors.Crimson,
                _ => Colors.White,
            };
        }
    }
}