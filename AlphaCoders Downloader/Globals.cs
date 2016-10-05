namespace AlphaCoders_Downloader
{
    public static class Globals
    {
        public enum SearchModes
        {
            search,
            sub_category,
            category,
            group
        }

        public enum Operator
        {
            max,
            equal,
            min
        }
        
        public enum Sort
        {
            newest,
            rating,
            views,
            favorites
        }

        public static string base_url { get; } = "https://wall.alphacoders.com/api2.0/get.php?";
    }
}
