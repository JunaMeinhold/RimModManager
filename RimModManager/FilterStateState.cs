namespace RimModManager
{
    using RimModManager.RimWorld;

    public class FilterState
    {
        private readonly FilteredList<RimMod> list;
        private ModKind filter = ModKind.All;
        private FilterMode filterMode;
        private string searchString = string.Empty;

        public FilterState(IReadOnlyList<RimMod> list)
        {
            this.list = new(list, FilterSelector);
        }

        public FilteredList<RimMod> Mods => list;

        private bool FilterSelector(RimMod mod)
        {
            if (filter != ModKind.All && filter != mod.Kind) return false;

            if (filterMode == FilterMode.Messages && mod.Messages.Count == 0) return false;

            switch (filterMode)
            {
                case FilterMode.Name:
                    return mod.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false;

                case FilterMode.Autor:
                    foreach (var author in mod.Metadata.Authors)
                    {
                        if (author.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    return false;

                case FilterMode.Path:
                    return mod.Path?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false;

                case FilterMode.PackageId:
                    return mod.PackageId?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false;
            }

            return true;
        }

        public ModKind Filter
        {
            get => filter;
            set
            {
                filter = value;
                Refresh();
            }
        }

        public FilterMode FilterMode
        {
            get => filterMode;
            set
            {
                filterMode = value;
                Refresh();
            }
        }

        public string SearchString
        {
            get => searchString;
            set
            {
                searchString = value;
                Refresh();
            }
        }

        public void Refresh()
        {
            list.Refresh();
        }
    }
}