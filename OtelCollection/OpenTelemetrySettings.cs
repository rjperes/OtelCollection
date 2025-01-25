using OpenTelemetry.Exporter;
using System.ComponentModel.DataAnnotations;

namespace OtelCollection
{
    public class OpenTelemetrySettings
    {
        [Url]
        public string? BaseUrl { get; set; }

        [EnumDataType(typeof(OtlpExportProtocol))]
        public string? Protocol { get; set; }

        public string? ServiceName { get; set; }

        public List<string> Metrics { get; } = new List<string>();

        public List<string> Sources { get; } = new List<string>();
    }
}
