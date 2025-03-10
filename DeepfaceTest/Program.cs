using System.Net.Http.Json;
using DeepfaceTest;
using Npgsql;
using Pgvector;


var connectionString = "Host=localhost;Port=55665;Username=myuser;Password=kjaXH243sbAk-d78ds;Database=photos";

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
await using var dataSource = dataSourceBuilder.Build();

var conn = dataSource.OpenConnection();

await using (var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", conn))
{
    await cmd.ExecuteNonQueryAsync();
}

conn.ReloadTypes();

await CreateDatabaseAsync(conn);

var reload = false;
if (reload)
{
    await ClearDatabaseAsync(conn);
    await GenerateEmbeddingsAllImages("C:\\Temp\\deepface\\dataset\\train\\", conn);
}

Console.WriteLine("Angelina");  
await GetNearestNeighborsAsync("/img1.jpg", conn);
Console.WriteLine("Jennifer");  
await GetNearestNeighborsAsync("/img53.jpg", conn);

static async Task GenerateEmbeddingsAllImages(string folder, NpgsqlConnection conn)
{
    foreach (var fullName in Directory.GetFiles(folder))
    {
        var file = fullName.Replace(folder, "/train/");
        await StoreEmbeddingsAsync(file, conn);
        Console.WriteLine("Done generating embedding images");
    }
}

static async Task StoreEmbeddingsAsync(string file, NpgsqlConnection conn)
{
    var generator = new EmbeddingGenerator();
    var response = await generator.GenerateAsync(file);
    Console.WriteLine(response);

    foreach (var result in response.results)
    {
        var embedding = new Vector(result.embedding);

        await using (var cmd = new NpgsqlCommand("INSERT INTO faces (file, embedding) VALUES ($1, $2)", conn))
        {
            cmd.Parameters.AddWithValue(file);
            cmd.Parameters.AddWithValue(embedding);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

static async Task GetNearestNeighborsAsync(string file, NpgsqlConnection conn)
{
    var generator1 = new EmbeddingGenerator();

    var r = await generator1.GenerateAsync(file);
    var embedding = new Vector(r.results.First().embedding);

    // <-> - L2 distance (nearest neighbors)
    // <=> - cosine distance
    await using var cmd =
        new NpgsqlCommand(@"
            SELECT embedding <-> $1, * 
            FROM faces 
            WHERE embedding <-> $1 < 1.15
            ORDER BY embedding <-> $1 
            "
            , conn);
    cmd.Parameters.AddWithValue(embedding);

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        //var vector =  (Vector)reader.GetValue(0);
        //Console.WriteLine(vector);
        Console.WriteLine($"{reader.GetValue(0)} - {reader.GetValue(3)}");
    }
}


static async Task CreateDatabaseAsync(NpgsqlConnection conn)
{
    await using var cmd = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS faces (" +
                                            "id serial PRIMARY KEY, embedding vector(4096), file VARCHAR(2000))", conn);
    await cmd.ExecuteNonQueryAsync();
}


static async Task ClearDatabaseAsync(NpgsqlConnection conn)
{
    await using var cmd = new NpgsqlCommand("DELETE FROM faces", conn);
    await cmd.ExecuteNonQueryAsync();
}