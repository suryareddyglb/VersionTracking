using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Model for package.json data (add more properties as needed)
public class PackageJson
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public Dictionary<string, string>? Dependencies { get; set; }
}

// Collection class to store results
public class RepoPackageCollection : List<(string Repo, PackageJson? Package)> { }

class Program
{
    static async Task Main(string[] args)
    {
        // List of GitHub repos in "owner/repo" format
        string ownerName = "suryareddyglb";
        string token = Environment.GetEnvironmentVariable("MYTOKEN");
        var repos = new List<string>
        {
            "Life-Cycle-Hook---NgAfterContentChecked",
            "inetCustomerData",
        };

        var collection = new RepoPackageCollection();
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var rawJsonCollection = new List<(string Repo, string Json)>();

        foreach (var repo in repos)
        {
            var apiUrl = $"https://api.github.com/repos/{ownerName}/{repo}/contents/package.json";
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                request.Headers.UserAgent.ParseAdd("DotNetApp"); // GitHub API requires User-Agent

                var response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"GitHub API error: {response.StatusCode}");

                var apiJson = await response.Content.ReadAsStringAsync();

                // Parse the API response to get the base64 content
                using var doc = JsonDocument.Parse(apiJson);
                var root = doc.RootElement;
                var contentBase64 = root.GetProperty("content").GetString();
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(contentBase64));

                rawJsonCollection.Add((repo, json));

                var package = JsonSerializer.Deserialize<PackageJson>(json,
                               new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                collection.Add((repo, package));
                Console.WriteLine($"Fetched package.json for {repo}: {package?.Name} {package?.Version}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch package.json for {repo}: {ex.Message}");
                collection.Add((repo, null));
                rawJsonCollection.Add((repo, string.Empty));
            }
        }

        var filterKeys = new HashSet<string>
        {
            "@angular/compiler",
            "@angular/core",
            "@angular/forms"
        };

        Console.WriteLine("\nFiltered Dependencies:");
        foreach (var (repo, package) in collection)
        {
            if (package?.Dependencies != null)
            {
                Console.WriteLine($"\n{repo}: ");
                foreach (var dep in package.Dependencies)
                {
                    if (filterKeys.Contains(dep.Key))
                    {
                        var cleanValue = dep.Value.TrimStart('^', '~');
                        Console.WriteLine($"{dep.Key} = {cleanValue}");
                    }
                }
            }
        }

        // Insert into PostgreSQL
        //string connectionString = Environment.GetEnvironmentVariable("PG_CONNECTION_STRING");
        //var inserter = new PostgresInserter(connectionString);
        //await inserter.InsertDependenciesAsync(collection, filterKeys);

        Console.ReadLine();
    }

}



