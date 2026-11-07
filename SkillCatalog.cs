using System.Text.RegularExpressions;

namespace ProfileMatcher;

public sealed class SkillCatalog
{
    private static readonly Dictionary<string, string[]> Definitions = new()
    {
        ["C#"] = ["C#", "C Sharp", "CSharp"],
        ["C++"] = ["C++"],
        ["ASP.NET Core"] = ["ASP.NET Core", "ASP NET Core"],
        ["SQL Server"] = ["SQL Server", "Microsoft SQL Server", "MSSQL"],
        ["Azure"] = ["Azure", "Microsoft Azure"],
        ["AWS"] = ["AWS", "Amazon Web Services"],
        ["Docker"] = ["Docker"],
        ["Java"] = ["Java"],
        ["JavaScript"] = ["JavaScript"],
        ["Python"] = ["Python"],
        ["React"] = ["React", "React.js"],
        [".NET"] = [".NET", "dotnet"]
    };

    public string Test(string skill)
    {
        return String.Empty;
    }
    private readonly Dictionary<string, string> aliases = Definitions
        .SelectMany(d => d.Value.Select(alias => (Alias: alias, Canonical: d.Key)))
        .ToDictionary(x => x.Alias, x => x.Canonical, StringComparer.OrdinalIgnoreCase);

    public string Normalize(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill)) throw new ArgumentException("Skill cannot be blank.");
        return aliases.TryGetValue(skill.Trim(), out var canonical)
            ? canonical : skill.Trim().ToUpperInvariant();
    }

    // Mention detection only. This intentionally does not infer experience or positive evidence.
    public string[] FindMentions(string text)
    {
        var occupied = new List<(int Start, int End)>();
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in aliases.OrderByDescending(x => x.Key.Length))
        {
            var pattern = @"(?<![\p{L}\p{N}_+#.])" + Regex.Escape(alias.Key)
                + @"(?![\p{L}\p{N}_+#])";
            foreach (Match match in Regex.Matches(text, pattern,
                         RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                         TimeSpan.FromSeconds(1)))
            {
                if (occupied.Any(p => match.Index < p.End && match.Index + match.Length > p.Start))
                    continue;
                occupied.Add((match.Index, match.Index + match.Length));
                found.Add(alias.Value);
            }
        }
        return found.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }
}
