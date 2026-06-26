using System.Xml.Linq;

namespace EDCBMonitorPlugin;

public sealed class EDCBApiClient
{
    private readonly HttpClient _http;

    public EDCBApiClient(string url = "http://localhost:5510/api/")
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(url)
        };
    }

    public async Task<XDocument> GetReserveInfoAsync()
        => XDocument.Parse(await _http.GetStringAsync("EnumReserveInfo"));

    public async Task<XDocument> GetTunerInfoAsync()
        => XDocument.Parse(await _http.GetStringAsync("EnumTunerReserveInfo"));

    public async Task<XDocument> GetRecInfoAsync()
        => XDocument.Parse(await _http.GetStringAsync("EnumRecInfo"));
}
