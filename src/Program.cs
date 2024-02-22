using System.Data.SQLite;

using rinha_de_backend_2024_q1_dotnet.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SourceGenerationContext.Default);
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
    if (!clientes.ContainsKey((id)))
        return Results.NotFound("Cliente não encontrado.");

    if (!transacao.Valida())
        return Results.UnprocessableEntity("Cliente não encontrado.");

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

app.Run();