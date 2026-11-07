using System.Text.Json;
using System.Text.Json.Serialization;
using ProfileMatcher;

var json = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() }
};
try
{
    var catalog = new SkillCatalog();
    if (args.SequenceEqual(new[] { "--self-test" }))
    {
        SelfTests.Run();
        return 0;
    }
    if (args.Length == 2 && args[0] == "--suggest")
    {
        var text = File.ReadAllText(args[1]);
        var draft = new Job("REVIEW REQUIRED: suggested requirements", text,
            catalog.FindMentions(text).Select(s => new Requirement(s, [s], 1, false)).ToArray());
        Console.Error.WriteLine("Mention suggestions only. Review negation, alternatives, required status, weights and years before scoring.");
        Console.WriteLine(JsonSerializer.Serialize(draft, json));
        return 0;
    }
    if (args.Length != 0 && !(args.Length is 4 or 5 && args[0] == "--job"
        && args[2] == "--profiles" && (args.Length == 4 || args[4] == "--strict")))
        throw new ArgumentException("Usage: dotnet run -- [--job job.json --profiles profiles.json [--strict]] | --suggest jd.txt | --self-test");
    var jobPath = args.Length == 0 ? Path.Combine(AppContext.BaseDirectory, "samples/job.json") : args[1];
    var profilesPath = args.Length == 0 ? Path.Combine(AppContext.BaseDirectory, "samples/profiles.json") : args[3];
    var job = JsonSerializer.Deserialize<Job>(File.ReadAllText(jobPath), json)
        ?? throw new ArgumentException("Job JSON is empty.");
    var profiles = JsonSerializer.Deserialize<Profile[]>(File.ReadAllText(profilesPath), json)
        ?? throw new ArgumentException("Profile JSON is empty.");
    var results = new MatchingService(catalog).Rank(job, profiles, args.Contains("--strict"));
    Console.WriteLine(JsonSerializer.Serialize(new { job.Title, Results = results }, json));
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or IOException or JsonException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
