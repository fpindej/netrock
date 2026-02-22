var url = args.Length > 0 ? args[0] : "http://localhost:8080/health/live";

try
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    using var response = await client.GetAsync(url);
    return response.IsSuccessStatusCode ? 0 : 1;
}
catch
{
    return 1;
}
