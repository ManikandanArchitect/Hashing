using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");
builder.Services.AddHttpClient();

var app = builder.Build();

var ring = new ConsistentHashRing(100);

// Docker service names
var nodeA = new Node("A", "http://node-a:8080");
var nodeB = new Node("B", "http://node-b:8080");
var nodeC = new Node("C", "http://node-c:8080");

ring.AddNode(nodeA);
ring.AddNode(nodeB);
ring.AddNode(nodeC);

app.MapPost("/put", async (KeyValueRequest request, IHttpClientFactory factory) =>
{
    var nodes = ring.GetReplicas(request.Key, 2);
    var client = factory.CreateClient();

    foreach (var node in nodes)
    {
        try
        {
            await client.PostAsJsonAsync($"{node.Address}/put", request);
        }
        catch
        {
            Console.WriteLine($"Failed writing to {node.Id}");
        }
    }

    return Results.Ok($"Stored in {string.Join(",", nodes.Select(n => n.Id))}");
});

app.MapGet("/get/{key}", async (string key, IHttpClientFactory factory) =>
{
    var nodes = ring.GetReplicas(key, 2);
    var client = factory.CreateClient();

    foreach (var node in nodes)
    {
        try
        {
            var response = await client.GetAsync($"{node.Address}/get/{key}");

            if (response.IsSuccessStatusCode)
            {
                var value = await response.Content.ReadAsStringAsync();
                return Results.Ok(value);
            }
        }
        catch
        {
            Console.WriteLine($"Read failed from {node.Id}");
        }
    }

    return Results.NotFound();
});

app.Run();

record KeyValueRequest(string Key, string Value);

public class Node
{
    public string Id { get; }
    public string Address { get; }

    public Node(string id, string address)
    {
        Id = id;
        Address = address;
    }
}

public class ConsistentHashRing
{
    private readonly SortedDictionary<long, Node> _ring = new();
    private readonly int _virtualNodeCount;

    public ConsistentHashRing(int virtualNodeCount)
    {
        _virtualNodeCount = virtualNodeCount;
    }

    public void AddNode(Node node)
    {
        for (int i = 0; i < _virtualNodeCount; i++)
        {
            string virtualNodeKey = $"{node.Id}-VN{i}";
            long hash = ComputeHash(virtualNodeKey);
            _ring[hash] = node;
        }
    }

    public Node GetNode(string key)
    {
        long hash = ComputeHash(key);

        foreach (var entry in _ring)
        {
            if (entry.Key >= hash)
                return entry.Value;
        }

        return _ring.First().Value;
    }

    private long ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToInt64(bytes, 0) & long.MaxValue;
    }

    public List<Node> GetReplicas(string key, int replicationFactor)
    {
        var result = new List<Node>();
        var visited = new HashSet<string>();

        long hash = ComputeHash(key);

        var keys = _ring.Keys.ToList();

        int startIndex = keys.FindIndex(k => k >= hash);
        if (startIndex == -1)
            startIndex = 0;

        int index = startIndex;

        while (result.Count < replicationFactor)
        {
            var node = _ring[keys[index]];

            if (!visited.Contains(node.Id))
            {
                result.Add(node);
                visited.Add(node.Id);
            }

            index = (index + 1) % keys.Count;

            if (visited.Count == _ring.Values.Select(n => n.Id).Distinct().Count())
                break;
        }

        return result;
    }

}
