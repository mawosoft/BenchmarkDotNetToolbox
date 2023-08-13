// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Loggers;

namespace ColumnDisplaySamples;

// Extracts information from captured BenchmarkDotNet log
public static class LogParser
{
    // LogCapture that needs to be added to your config(s)
    public static readonly LogCapture LogCapture = new();
    public class SummaryParts
    {
        public List<OutputLine> Environment = new();
        public List<OutputLine> Host = new(); // [Host] part of all runtimes
        public List<OutputLine> Runtimes = new();
        public List<OutputLine> CommonValues = new();
        public List<OutputLine> Table = new();
        public List<OutputLine> Errors = new();
        public List<OutputLine> Warnings = new();
        public List<OutputLine> Hints = new();
        public List<OutputLine> Legend = new();
        public List<OutputLine> Diagnosers = new();
    }

    public static List<SummaryParts> GetSummaries()
    {
        List<SummaryParts> summaries = new();
        OutputLine emptyLine = new() { Kind = LogKind.Default, Text = Environment.NewLine };
        IEnumerable<OutputLine> captured = LogCapture.CapturedOutput.ToList();
        // Using the original IReadOnlyList<> and an index would be more efficient, but Skip/Take
        // is more readable and there aren't really any performance here issues to worry about.
        while (captured.Any())
        {
            captured = captured.SkipWhile(ol => ol.Text != "// * Summary *").Skip(1)
                .SkipWhile(ol => ol.Text.Length == 0 || ol.Text == Environment.NewLine);
            if (!captured.Any()) break;
            SummaryParts parts = new();
            summaries.Add(parts);

            parts.Environment.AddRange(captured.TakeWhile(ol => !ol.Text.StartsWith("  ")));
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
                if (target != null)
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
