using System.Data.SQLite;
using System.Text.Json;

using rinha_de_backend_2024_q1_dotnet.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddScoped<SQLiteConnection>(_ => new SQLiteConnection("DataSource=rinha.db;"));

var app = builder.Build();

var clientes = new Dictionary<int, int>
{
    { 1, 1000 * 100 },
    { 2, 800 * 100 },
    { 3, 10000 * 100 },
    { 4, 100000 * 100 },
    { 5, 5000 * 100 }
};

app.MapPost("/clientes/{id}/transacoes", async (int id, TransacaoRequest transacao, SQLiteConnection conn) =>
{
    if (!clientes.ContainsKey(id))
        return Results.NotFound("Cliente não encontrado.");

    if (!transacao.Valida())
        return Results.UnprocessableEntity("Transação inválida.");

    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        string transacaoSql = transacao.Tipo == "c" ? "transacaoCredito.sql" : "transacaoDebito.sql";
        using var scriptTransacao = new StreamReader($"sql/{transacaoSql}");

        cmd.CommandText = await scriptTransacao.ReadToEndAsync();
        cmd.Parameters.AddWithValue("@Valor", transacao.Valor);
        cmd.Parameters.AddWithValue("@Tipo", transacao.Tipo);
        cmd.Parameters.AddWithValue("@Descricao", transacao.Descricao);
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        if (transacao.Tipo == "d")
        {
            if (reader.GetBoolean(0))
                return Results.UnprocessableEntity("Saldo inconsistente.");

            await reader.NextResultAsync();
            await reader.ReadAsync();
        }

        return Results.Ok(new TransacoesResponse(clientes[id], reader.GetInt32(0)));
    }
});

app.MapGet("/clientes/{id}/extrato", async (int id, SQLiteConnection conn) =>
{
    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
        select saldo from cliente where id = @id;
        select valor, tipo, descricao, realizadoEm from transacao where idCliente = @id ORDER BY realizado_em DESC LIMIT 10;
        ";
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        var saldo = reader.GetInt32(0);

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