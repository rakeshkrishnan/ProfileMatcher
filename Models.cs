namespace ProfileMatcher;

public sealed record Requirement(string Label, string[] AnyOf, decimal Weight,
    bool Required, decimal? MinimumYears = null);
public sealed record Job(string Title, string Description, Requirement[] Requirements);
// Years are per skill, supplied from reviewed evidence; null means unknown.
public sealed record SkillEvidence(string Skill, string Evidence, decimal? Years = null);
public sealed record Profile(string Id, string Name, SkillEvidence[] Skills);
public enum MatchStatus { Satisfied, NotSatisfied, Unknown }
public sealed record RequirementMatch(string Requirement, decimal Weight, bool Required,
    MatchStatus Status, string Explanation, string? Evidence);
public sealed record MatchResult(string ProfileId, string Name, decimal Percentage,
    int RequiredSatisfied, int RequiredTotal, bool AllRequiredSatisfied,
    RequirementMatch[] Requirements);
