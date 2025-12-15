namespace RimModManager
{
    using Hexa.NET.KittyUI.Web;
    using System.Threading.Tasks;

    public static class HttpClientExtensions
    {
        public static async Task DownloadFileAsync(this HttpClient client, string url, string destination)
        {
            using var fs = File.Create(destination);
            await client.DownloadAsync(url, fs);
        }
    }
}
