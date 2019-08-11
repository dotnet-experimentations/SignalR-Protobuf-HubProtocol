using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;

namespace Protobuf.Protocol.Microbenchmarks
{
    internal class BenchmarkConfiguration : ManualConfig
    {
        public BenchmarkConfiguration()
        {
            Add(StatisticColumn.P80,
                StatisticColumn.P85,
                StatisticColumn.P90,
                StatisticColumn.P95,
                StatisticColumn.P100);

            //Add(CsvMeasurementsExporter.Default);
            //Add(RPlotExporter.Default);
        }
    }
}

