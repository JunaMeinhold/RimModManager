namespace RimModManager.TextureOptimizer
{
    using Hexa.NET.KittyUI.Graphics.Imaging;

    public struct TextureItem
    {
        public ImageSource Texture;
        public string Path;
        public string Destination;

        public TextureItem(ImageSource texture, string path, string destination)
        {
            Texture = texture;
            Path = path;
            Destination = destination;
        }
    }
}