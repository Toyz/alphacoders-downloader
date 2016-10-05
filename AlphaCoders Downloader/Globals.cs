namespace AlphaCoders_Downloader
{
    public static class Globals
    {
        public enum SearchModes
        {
            search,
            sub_category,
            category
        }

        public static string base_url { get; } = "https://wall.alphacoders.com/api2.0/get.php?";
    }
}
