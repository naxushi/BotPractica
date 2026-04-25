using System.Text.Json;

public static class Parser
{
    public static async Task<List<Post>> GetPosts()
    {
        var list = new List<Post>();

        using var http = new HttpClient();

        var json = await http.GetStringAsync("https://api.rss2json.com/v1/api.json?rss_url=https://t.me/s/sochi_live");

        var doc = JsonDocument.Parse(json);

        foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            var text = item.GetProperty("title").GetString();

            list.Add(new Post
            {
                Id = text,
                Text = text
            });
        }

        return list;
    }
}
