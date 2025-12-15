namespace RimModManager
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.UI;
    using Hexa.NET.Utilities.Text;
    using System;
    using System.Numerics;

    public enum ToastAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    public class ToastManager
    {
        private static readonly List<ToastMessage> toasts = [];
        private static readonly Lock lockObj = new();
        private static readonly Queue<(ToastMessage toast, ActionType type)> queue = [];
        private static ToastAnchor anchor = ToastAnchor.TopRight;
        private static Vector2 anchorPadding = new(16);
        private static Vector2 itemSpacing = new(8);

        private enum ActionType
        {
            Add,
            Remove
        }

        public static ToastAnchor Anchor { get => anchor; set => anchor = value; }

        public static Vector2 AnchorPadding { get => anchorPadding; set => anchorPadding = value; }

        public static Vector2 ItemSpacing { get => itemSpacing; set => itemSpacing = value; }

        public static void Draw()
        {
            lock (lockObj)
            {
                while (queue.TryDequeue(out var result))
                {
                    var (toast, action) = result;
                    if (action == ActionType.Add)
                    {
                        toasts.Add(toast);
                    }
                    else if (action == ActionType.Remove)
                    {
                        toasts.Remove(toast);
                    }
                }
            }
            Vector2 sizeMax = Vector2.Zero;
            foreach (var toast in toasts)
            {
                sizeMax = Vector2.Max(sizeMax, toast.MeasureSize());
            }

            Vector2 pos = ComputeOrigin(sizeMax);

            foreach (var toast in toasts)
            {
                pos.Y += toast.Draw(pos, sizeMax).Y + itemSpacing.Y;
            }
        }

        private static Vector2 ComputeOrigin(Vector2 sizeMax)
        {
            var vp = ImGui.GetWindowViewport();
            var pos = vp.WorkPos;
            switch (anchor)
            {
                case ToastAnchor.TopLeft:
                    pos.X += anchorPadding.X;
                    pos.Y += anchorPadding.Y;
                    break;

                case ToastAnchor.TopRight:
                    pos.X += vp.WorkSize.X - sizeMax.X - anchorPadding.X;
                    pos.Y += anchorPadding.Y;
                    break;

                case ToastAnchor.BottomLeft:
                    pos.X += anchorPadding.X;
                    pos.Y += vp.WorkSize.Y - sizeMax.Y - anchorPadding.Y;
                    break;

                case ToastAnchor.BottomRight:
                    pos.X += vp.WorkSize.X - sizeMax.X - anchorPadding.X;
                    pos.Y += vp.WorkSize.Y - sizeMax.Y - anchorPadding.Y;
                    break;
            }
            return pos;
        }

        internal static void Show(ToastMessage task)
        {
            lock (lockObj)
            {
                queue.Enqueue((task, ActionType.Add));
            }
        }

        internal static void Close(ToastMessage task)
        {
            lock (lockObj)
            {
                queue.Enqueue((task, ActionType.Remove));
            }
        }
    }

    public enum ToastMessageFlags
    {
        None,
        Spinner,
    }

    public class ToastMessage : IProgress<float>
    {
        private float progress = float.NaN;
        private string title;
        private string? description;
        private readonly ToastMessageFlags flags = ToastMessageFlags.Spinner;
        private Vector2 titleSize;
        private Vector2 descriptionSize;
        private Vector2 totalSize;
        private Vector2 contentSize;

        public ToastMessage(string title, string? description = null, ToastMessageFlags flags = ToastMessageFlags.None)
        {
            this.title = title;
            this.description = description;
        }

        public void Report(float value)
        {
            progress = value;
        }

        public string Title { get => title; set => title = value; }

        public string? Description { get => description; set => description = value; }

        public float BorderSize { get; set; } = 4;

        public bool CanClose { get; set; } = true;

        public Vector2 Draw(Vector2 origin, Vector2 size)
        {
            var style = ImGui.GetStyle();
            var draw = ImGui.GetForegroundDrawList();
            ImRect totalRect = new(origin, origin + size);
            ImGui.SetNextWindowPos(origin);
            ImGui.SetNextWindowSize(size);
            ImGui.Begin(title, ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus);

            var id = ImGui.GetID(title);
            ImGuiP.ItemAdd(totalRect, id);

            var pos = origin;
            ImRect rect = new(pos, pos + size);

            draw.AddRectFilled(rect.Min, rect.Max, 0xff3f3f3f, 8);
            pos += new Vector2(BorderSize);
            size -= new Vector2(BorderSize * 2);
            rect = new(pos, pos + size);

            draw.AddRectFilled(rect.Min, rect.Max, 0xff7b3e00, 8);
            var progressMax = Vector2.Lerp(rect.Min, rect.Max, float.IsNaN(progress) ? 1.0f : progress);
            progressMax.Y = rect.Max.Y;
            draw.AddRectFilled(pos, progressMax, 0xffc86500, 8);
            pos += style.FramePadding + style.ItemSpacing;

            var fontSize = ImGui.GetFontSize();

            ImRect contentRect = new(pos, pos + size - (style.FramePadding + style.ItemSpacing) * 2);

            if ((flags & ToastMessageFlags.Spinner) != 0)
            {
              
                var spinnerSize = Spinner(draw, pos, fontSize * 0.5f, 2, 0xffffffff);
                pos.X += spinnerSize.X + style.ItemSpacing.X;
            }

            draw.AddText(pos, 0xffffffff, title);
            pos += new Vector2(0, titleSize.Y + style.ItemSpacing.Y);

            draw.AddText(pos, 0xffffffff, description);

            if (CanClose)
            {
                var rec = totalRect;
                var offset = contentSize.X + style.FramePadding.X + style.ItemSpacing.X;
                rec.Min.X += offset;
                Button(draw, id, rec);
            }
            ImGui.End();
            return totalSize;
        }

        private unsafe void Button(ImDrawListPtr draw, uint id, ImRect rect)
        {
            float textSize = ImGui.GetTextLineHeight();

            uint btnId = id;

            bool hovered = false, held = false;
            if (ImGuiP.ButtonBehavior(rect, btnId, ref hovered, ref held))
            {
                Close();
            }

            uint bgColor = held ? ImGui.GetColorU32(ImGuiCol.ButtonActive) : hovered ? ImGui.GetColorU32(ImGuiCol.ButtonHovered) : 0x000000;
            draw.AddRectFilled(rect.Min, rect.Max, bgColor, 8, ImDrawFlags.RoundCornersBottomRight | ImDrawFlags.RoundCornersTopRight);

            byte* buf = stackalloc byte[16];
            StrBuilder sb = new(buf, 16);
            sb.Append(MaterialIcons.Close);
            sb.End();

            Vector2 crossSize = new(textSize);
            Vector2 crossPos = (rect.Min + rect.Max) * 0.5f - crossSize * 0.5f;
            draw.AddText(crossPos, 0xffffffff, sb);
        }

        public static unsafe Vector2 Spinner(ImDrawListPtr draw, Vector2 pos, float radius, float thickness, uint color)
        {
            var g = ImGui.GetCurrentContext();
            var style = ImGui.GetStyle();

            Vector2 size = new(radius * 2, (radius + style.FramePadding.Y) * 2);
            // Render
            draw.PathClear();

            const int num_segments = 24;

            int start = (int)Math.Abs(MathF.Sin((float)(g.Time * 1.8f)) * (num_segments - 5));

            float a_min = (float)Math.PI * 2.0f * start / num_segments;
            float a_max = (float)Math.PI * 2.0f * ((float)num_segments - 3) / num_segments;

            Vector2 center = pos + new Vector2(radius, radius + style.FramePadding.Y);

            for (var i = 0; i < num_segments; i++)
            {
                float a = a_min + i / (float)num_segments * (a_max - a_min);
                var time = (float)g.Time;
                var pp = new Vector2(center.X + MathF.Cos(a + time * 8) * radius, center.Y + MathF.Sin(a + time * 8) * radius);
                draw.PathLineTo(pp);
            }

            draw.PathStroke(color, 0, thickness);

            return size;
        }

        public Vector2 MeasureSize()
        {
            var style = ImGui.GetStyle();
            contentSize = Vector2.Zero;

            if ((flags & ToastMessageFlags.Spinner) != 0)
            {
                contentSize.X += 11 * 2 + style.ItemSpacing.X;
            }

            titleSize = ImGui.CalcTextSize(title);
            contentSize += titleSize + style.ItemSpacing;

            if (description != null)
            {
                descriptionSize = ImGui.CalcTextSize(description);
                contentSize.Y += descriptionSize.Y + style.ItemSpacing.Y;
                contentSize.X = MathF.Max(contentSize.X, descriptionSize.X + style.ItemSpacing.X);
            }

            totalSize = new Vector2(BorderSize * 2) + style.FramePadding + style.ItemSpacing + contentSize;
            if (CanClose)
            {
                totalSize.X += ImGui.GetTextLineHeight();
            }
            return totalSize;
        }

        public void Show()
        {
            ToastManager.Show(this);
        }

        public void Close()
        {
            ToastManager.Close(this);
        }
    }

    public static class ToastMessageExtensions
    {
        public static ToastMessage ToToast(this Task task, string title, string? description = null, ToastMessageFlags flags = ToastMessageFlags.None)
        {
            ToastMessage message = new(title, description, flags);
            message.Show();
            task.ContinueWith(x =>
            {
                message.Close();
            });
            return message;
        }
    }
}