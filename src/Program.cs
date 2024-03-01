using System.Text.Json;

using Npgsql;

using rinha_de_backend_2024_q1_dotnet.Models;

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

app.MapPost("/clientes/{id}/transacoes", async (int id, TransacaoRequest transacao, NpgsqlConnection conn) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound("Cliente não encontrado.");

    if (!transacao.Valida())
        return Results.UnprocessableEntity("Transação inválida.");

    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        string transacaoSql = transacao.Tipo == "c"
        ? await File.ReadAllTextAsync("config/transacaoCredito.sql")
        : await File.ReadAllTextAsync("config/transacaoDebito.sql");

        cmd.CommandText = transacaoSql;
        cmd.Parameters.AddWithValue("@Valor", transacao.Valor);
        cmd.Parameters.AddWithValue("@Descricao", transacao.Descricao ?? string.Empty);
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        if (transacao.Tipo == "d")
        {
            if (reader.GetInt32(0) != 0)
                return Results.UnprocessableEntity("Saldo inconsistente.");

            await reader.NextResultAsync();
            await reader.ReadAsync();
        }

        return Results.Ok(new TransacoesResponse(clientes[id], reader.GetInt32(0)));
    }
});

app.MapGet("/clientes/{id}/extrato", async (int id, NpgsqlConnection conn) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound("Cliente não encontrado.");

    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
        select saldo from cliente where id = @id;
        select valor, tipo, descricao, realizadoEm from transacao where idCliente = @id ORDER BY realizadoEm DESC LIMIT 10;
        ";

        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        int saldo = reader.GetInt32(0);

        await reader.NextResultAsync();

        List<TransacoesExtratoResponse> transacoesRealizadas = new(10);
        while (await reader.ReadAsync())
        {
            transacoesRealizadas.Add(new TransacoesExtratoResponse(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDateTime(3)
            ));
        }

        return Results.Ok(new Extrato(new Saldo(saldo, DateTime.UtcNow, clientes[id]), transacoesRealizadas));
    }
});

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();