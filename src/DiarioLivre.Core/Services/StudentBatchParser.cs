using System.Text.RegularExpressions;

namespace DiarioLivre.Core.Services;

/// <summary>
/// Represents a student line parsed from pasted text or CSV/TXT lists.
/// </summary>
public sealed record ParsedStudentEntry(int? RollNumber, string FullName);

/// <summary>
/// Pure domain parser for batch student roster import (plain lines, numbered lines, or CSV).
/// </summary>
public sealed partial class StudentBatchParser
{
    private static readonly HashSet<string> HeaderTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "nome",
        "nome completo",
        "aluno",
        "aluna",
        "aluno(a)",
        "estudante",
        "name",
        "full name",
        "fullname",
        "student",
        "numero",
        "número",
        "nº",
        "chamada",
        "roll",
        "rollnumber"
    };

    [GeneratedRegex(@"^\s*(\d{1,3})\s*(?:[-–—.)\];,\t]+|\s+)\s*(.+?)\s*$")]
    private static partial Regex NumberedLineRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultiWhitespaceRegex();

    public IReadOnlyList<ParsedStudentEntry> Parse(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Array.Empty<ParsedStudentEntry>();
        }

        string[] lines = rawText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        var results = new List<ParsedStudentEntry>();
        bool checkedHeader = false;

        foreach (string rawLine in lines)
        {
            string trimmed = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (!checkedHeader)
            {
                checkedHeader = true;
                if (LooksLikeHeader(trimmed))
                {
                    continue;
                }
            }

            Match match = NumberedLineRegex().Match(trimmed);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int roll) && roll > 0)
            {
                string cleanedName = CleanName(match.Groups[2].Value);
                if (!string.IsNullOrWhiteSpace(cleanedName))
                {
                    results.Add(new ParsedStudentEntry(roll, cleanedName));
                }

                continue;
            }

            string plainName = CleanName(trimmed);
            if (!string.IsNullOrWhiteSpace(plainName))
            {
                results.Add(new ParsedStudentEntry(null, plainName));
            }
        }

        return results;
    }

    private static bool LooksLikeHeader(string line)
    {
        string[] parts = line.Split(new[] { ';', ',', '\t' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        return parts.All(p => HeaderTokens.Contains(p.Trim('"', '\'')));
    }

    private static string CleanName(string rawName)
    {
        string unquoted = rawName.Trim().Trim('"', '\'').Trim();
        int sepIndex = unquoted.IndexOfAny(new[] { ';', '\t' });
        if (sepIndex > 0)
        {
            unquoted = unquoted[..sepIndex].Trim();
        }

        return MultiWhitespaceRegex().Replace(unquoted, " ");
    }
}
