// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace ColumnDisplaySamples;

// Extracts information from captured BenchmarkDotNet log
internal static class LogParser
{
    // LogCapture that needs to be added to your config(s)
    public static readonly LogCapture LogCapture = new();
    public sealed class SummaryParts
    {
        public List<OutputLine> Environment = [];
        public List<OutputLine> Host = []; // [Host] part of all runtimes
        public List<OutputLine> Runtimes = [];
        public List<OutputLine> CommonValues = [];
        public List<OutputLine> Table = [];
        public List<OutputLine> Errors = [];
        public List<OutputLine> Warnings = [];
        public List<OutputLine> Hints = [];
        public List<OutputLine> Legend = [];
        public List<OutputLine> Diagnosers = [];
    }

    [SuppressMessage("Performance", "CA1851:Possible multiple enumerations of 'IEnumerable' collection",
        Justification = "Mostly false positive. Ienumerable result gets reassigned after partial enumeration.")]
    public static List<SummaryParts> GetSummaries()
    {
        List<SummaryParts> summaries = [];
        OutputLine emptyLine = new() { Kind = LogKind.Default, Text = Environment.NewLine };
        IEnumerable<OutputLine> captured = [.. LogCapture.CapturedOutput];
        // Using the original IReadOnlyList<> and an index would be more efficient, but Skip/Take
        // is more readable and there aren't really any performance here issues to worry about.
        while (captured.Any())
        {
            captured = captured.SkipWhile(ol => ol.Text != "// * Summary *").Skip(1)
                .SkipWhile(ol => ol.Text.Length == 0 || ol.Text == Environment.NewLine);
            if (!captured.Any()) break;
            SummaryParts parts = new();
            summaries.Add(parts);

            parts.Environment.AddRange(captured.TakeWhile(ol => !ol.Text.StartsWith("  ", StringComparison.Ordinal)));
            captured = captured.Skip(parts.Environment.Count);

            OutputLine oline = captured.First();
            captured = captured.Skip(1).SkipWhile(ol => ol.Text.Length == 0 || ol.Text == Environment.NewLine);
            IEnumerable<string> lines = oline.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            if (lines.First().StartsWith("  [Host]", StringComparison.Ordinal))
            {
                parts.Host.Add(new OutputLine() { Kind = oline.Kind, Text = lines.First() + Environment.NewLine });
                lines = lines.Skip(1);
            }
            parts.Runtimes.AddRange(
                lines.Select(line => new OutputLine() { Kind = oline.Kind, Text = line + Environment.NewLine }));
            if (parts.Runtimes.Count > 0) parts.Runtimes.Add(emptyLine);

            parts.CommonValues.AddRange(
                captured.TakeWhile(ol => ol.Kind == LogKind.Info
                                   || ol.Text.Length == 0 || ol.Text == Environment.NewLine));
            captured = captured.Skip(parts.CommonValues.Count);

            parts.Table.AddRange(captured.TakeWhile(ol => !ol.Text.StartsWith("//", StringComparison.Ordinal)));
            captured = captured.Skip(parts.Table.Count);

            while (captured.Any() && !captured.First().Text.StartsWith("// ***", StringComparison.Ordinal))
            {
                captured = captured.SkipWhile(ol => !ol.Text.StartsWith("// *", StringComparison.Ordinal));
                List<OutputLine>? target = null;
                string head = captured.FirstOrDefault().Text ?? string.Empty;
                bool skipHead = true;
                switch (head)
                {
                    case "// * Legends *": target = parts.Legend; break;
                    case "// * Errors *": target = parts.Errors; break;
                    case "// * Warnings *": target = parts.Warnings; break;
                    case "// * Hints *": target = parts.Hints; break;
                    default:
                        skipHead = false;
                        if (head.StartsWith("// * Diagnostic Output", StringComparison.Ordinal))
                        {
                            target = parts.Diagnosers;
                        }
                        break;
                }
                if (skipHead)
                {
                    captured = captured.Skip(1).
                        SkipWhile(ol => ol.Text.Length == 0 || ol.Text == Environment.NewLine);
                }
                if (target is not null)
                {
                    int prevCount = target.Count;
                    target.AddRange(captured.TakeWhile(ol => !ol.Text.StartsWith("//", StringComparison.Ordinal)));
                    captured = captured.Skip(target.Count - prevCount);
                }
            }
        }
        return summaries;
    }
}
