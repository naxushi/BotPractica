public static class Filter
{
    static string[] keywords = new[]
    {
        "вода",
        "свет",
        "электро",
        "газ",
        "жкх",
        "отключение",
        "тепло",
        "авария"
    };

    public static bool IsRelevant(Post post)
    {
        var text = post.Text.ToLower();

        return keywords.Any(k => text.Contains(k));
    }
}