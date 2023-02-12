// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace ColumnDisplaySamples
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // Normally, we would just use
            //      BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
            // but that will sort types alphabetically and we want to maintain a certain order.
            BenchmarkSwitcher switcher = BenchmarkSwitcher.FromTypes(new[] {
                typeof(JobColumnSample1_RunModes_BDNDefault),
                typeof(JobColumnSample1_RunModes_JobColumnSelectionProvider),
                typeof(JobColumnSample2_Runtimes_BDNDefault),
                typeof(JobColumnSample2_Runtimes_JobColumnSelectionProvider),
                typeof(ParamColumnSample_BDNDefault),
                typeof(ParamColumnSample_CombinedParamsColumn_Default),
                typeof(ParamColumnSample_CombinedParamsColumn_Custom),
                typeof(ParamColumnSample_RecyclableParamsColumnProvider_Default),
                typeof(ParamColumnSample_RecyclableParamsColumnProvider_Custom),
                typeof(ParamWrapperSample),
            });

            // All samples use an exclusive local config (ConfigUnionRule.AlwaysUseLocal).
            // You can still specify filters on the command line, but all other options won't have any effect.
            // Without filters, BenchmarkSwitcher will give you a numbered list of benchmark types to choose from.
            // While the help text doesn't explicitly say so, you can also enter an * (asterisk) to run all benchmarks.
            List<Summary> summaries = switcher.Run(args, config: null!).ToList();

            // Repeat all summaries at the and with some annotations
            PrintSamplesOverview(summaries);
        }

        // Prints an overview of all sample results at the end of the log.
        // A bit like BDN's --join option w/o the actual joining ;)
        private static void PrintSamplesOverview(List<Summary> summaries)
        {
            if (summaries.Count <= 1)
                return;

            // Append to both console and logfile
            using StreamLogger streamLogger = new(summaries.First().LogFilePath, append: true);
            ILogger logger = ImmutableConfigBuilder.Create(
                ManualConfig.CreateEmpty().AddLogger(SampleBase.SampleConfigBase.Console, streamLogger)
                ).GetCompositeLogger();

            List<LogParser.SummaryParts> captured = LogParser.GetSummaries();
            if (captured.Count != summaries.Count)
            {
                throw new InvalidOperationException("Mismatching count between returned and captured summaries.");
            }
            bool hasOverviewHeader = false;
            string lastGroupDescription = "";
            for (int i = 0; i < summaries.Count; i++)
            {
                LogParser.SummaryParts parts = captured[i];
                Type type = summaries[i].BenchmarksCases.First().Descriptor.Type;
                string groupDescription = type.Name;
                string variantDescription = string.Empty;
                if (Activator.CreateInstance(type) is SampleBase sample)
                {
                    groupDescription = sample.SampleGroupDescription;
                    variantDescription = sample.SampleVariantDescription;
                }
                if (groupDescription != lastGroupDescription)
                {
                    if (!hasOverviewHeader)
                    {
                        logger.WriteLine();
                        logger.WriteLineHeader("// ***** Samples Overview *****");
                        logger.WriteLine();
                        parts.Environment.ForEach(ol => logger.Write(ol.Kind, ol.Text));
                        parts.Host.ForEach(ol => logger.Write(ol.Kind, ol.Text));
                        hasOverviewHeader = true;
                    }
                    logger.WriteLine();
                    logger.WriteLineHeader($"// *** {groupDescription} ***");
                    logger.WriteLine();
                    parts.Runtimes.ForEach(ol => logger.Write(ol.Kind, ol.Text));
                    lastGroupDescription = groupDescription;
                }
                if (variantDescription.Length != 0)
                {
                    logger.WriteLineHeader($"// {variantDescription}");
                    logger.WriteLine();
                }
                parts.CommonValues.ForEach(ol => logger.Write(ol.Kind, ol.Text));
                parts.Table.ForEach(ol => logger.Write(ol.Kind, ol.Text));
                parts.Legend.ForEach(ol => logger.Write(ol.Kind, ol.Text));
            }
        }
    }
}
