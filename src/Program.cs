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

    if (!transacao.Valida())
        return Results.UnprocessableEntity();

    await using (conn)
    {
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = $"select criartransacao($1, $2, $3)";
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(transacao.Tipo == "c" ? transacao.Valor : transacao.Valor * -1);
        cmd.Parameters.AddWithValue(transacao.Descricao);

        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            throw new InvalidOperationException("Could not read from db.");

        var record = reader.GetFieldValue<object[]>(0);

        if (record.Length == 1)
        {
            var failureCode = (int)record[0];
            if (failureCode == -1)
                return Results.NotFound();
            else if (failureCode == -2)
                return Results.UnprocessableEntity();
            else
                throw new InvalidOperationException("Invalid failure code.");
        }

        var (saldo, limite) = ((int)record[0], -1 * (int)record[1]);
        return Results.Ok(new TransacoesResponse(limite, saldo));
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
        SELECT valor, descricao, realizadoEm FROM transacao WHERE idCliente = $1 ORDER BY id DESC LIMIT 10
        ";

        using var reader = await cmd.ExecuteReaderAsync();
        List<TransacoesExtratoResponse> transacoesRealizadas = new(10);

        while (await reader.ReadAsync())
        {
            int valor = reader.GetInt32(0);

            transacoesRealizadas.Add(new(
                Math.Abs(valor),
                valor < 0 ? "d" : "c",
                reader.GetString(1),
                reader.GetDateTime(2)
            ));

        }

        return Results.Ok(new Extrato(new Saldo(saldo, DateTime.UtcNow, clientes[id]), transacoesRealizadas));
    }
});

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();