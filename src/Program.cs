using System.Text.Json;

using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddNpgsqlDataSource(
    Environment.GetEnvironmentVariable("Connection_String")
);

var app = builder.Build();

var clientes = new Dictionary<int, int>
{
    { 1, 1000 * 100 },
    { 2, 800 * 100 },
    { 3, 10000 * 100 },
    { 4, 100000 * 100 },
    { 5, 5000 * 100 }
};

var functionTransaction = new Dictionary<string, string>
{
    { "c", "realizar_credito" },
    { "d", "realizar_debito" }
};

app.MapPost("/clientes/{id}/transacoes", async (int id, TransacaoRequest transacao, NpgsqlConnection conn) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    if (!transacao.Valida())
        return Results.UnprocessableEntity();

    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = $"select novo_saldo, possui_erro, mensagem from {functionTransaction[transacao.Tipo]}($1, $2, $3)";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(transacao.Valor);
        cmd.Parameters.AddWithValue(transacao.Descricao);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        if (reader.GetBoolean(1))
            return Results.UnprocessableEntity();

        return Results.Ok(new TransacoesResponse(clientes[id], reader.GetInt32(0)));
    }
});

app.MapGet("/clientes/{id}/extrato", async (int id, NpgsqlConnection conn) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound();

    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
SELECT c.saldo AS saldo_cliente, t.valor, t.tipo, t.descricao, t.realizadoEm
FROM cliente c
LEFT JOIN (
    SELECT idCliente, valor, tipo, descricao, realizadoEm
    FROM transacao
    WHERE idCliente = $1
    ORDER BY realizadoEm DESC
    LIMIT 10
) AS t ON c.id = t.idCliente
WHERE c.id = $1;

        ";

        cmd.Parameters.AddWithValue(id);

        using var reader = await cmd.ExecuteReaderAsync();

        List<TransacoesExtratoResponse> transacoesRealizadas = new(10);
        
        while (await reader.ReadAsync())
            transacoesRealizadas.Add(new(
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDateTime(4)
            ));

        return Results.Ok(new Extrato(new Saldo(reader.GetInt32(0), DateTime.UtcNow, clientes[id]), transacoesRealizadas));
    }
});

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();