using System.Net.Http;

public static class Parser
{
    public static async Task<List<string>> Parse(string url)
    {
        var list = new List<string>();

        try
        {
            var http = new HttpClient();
            var html = await http.GetStringAsync(url);

            var parts = html.Split("tgme_widget_message_text");

            foreach (var p in parts.Skip(1).Take(1))
            {
                list.Add("Новость из канала:\n" + url);
            }
        }
        catch { }

        return list;
    }
}