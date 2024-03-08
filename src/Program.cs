using System.Text.Json;

using Microsoft.AspNetCore.Http.Timeouts;

using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(60) });
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddNpgsqlDataSource(
    Environment.GetEnvironmentVariable("Connection_String")
);

var app = builder.Build();
app.UseRequestTimeouts();

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
        return Results.NotFound();

    if (transacao.Descricao is null or "" or { Length: > 10 })
        return Results.UnprocessableEntity();

    if (int.TryParse(transacao.Valor?.ToString(), out var valor) is false)
        return Results.UnprocessableEntity();

    if (!(valor > 0))
        return Results.UnprocessableEntity();

    if (!transacao.Valida())
        return Results.UnprocessableEntity();

    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = transacao.Tipo == "d"
        ? $"select saldoAtual, erro from realizar_debito($1, $2, $3)"
        : $"select saldoAtual, erro from realizar_credito($1, $2, $3)";

        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(valor);
        cmd.Parameters.AddWithValue(transacao.Descricao);

        using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();

        if (reader.GetBoolean(1))
            return Results.UnprocessableEntity();

        return Results.Ok(new TransacoesResponse(reader.GetInt32(0), clientes[id]));
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
        SELECT saldo
        FROM cliente
        WHERE id = $1
        ";

        cmd.Parameters.AddWithValue(id);
        var saldoResult = await cmd.ExecuteScalarAsync();
        int.TryParse(saldoResult.ToString(), out int saldo);

        cmd.CommandText = @"
        SELECT valor, tipo, descricao, realizadoEm FROM transacao WHERE idCliente = $1 ORDER BY id DESC LIMIT 10
        ";

        using var reader = await cmd.ExecuteReaderAsync();
        List<TransacoesExtratoResponse> transacoesRealizadas = new(10);

        while (await reader.ReadAsync())
        {
            int valor = reader.GetInt32(0);

            transacoesRealizadas.Add(new(
                Math.Abs(valor),
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