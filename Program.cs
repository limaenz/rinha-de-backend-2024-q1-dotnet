using System.Text.Json;

using Dapper;
using Dapper.Contrib.Extensions;

using rinha_de_backend_2024_q1_dotnet.Model;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddScoped<DbContext>(_ => new DbContext(connectionString));

var app = builder.Build();

app.MapPost("/clientes/{id}/transacoes", async (HttpContext context, DbContext dbContext) =>
{
    //recupera body da requisicao
    using var reader = new StreamReader(context.Request.Body);
    var requestBody = await reader.ReadToEndAsync();

    //opcao do serialize com case insensitive true
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    //deserialize
    var transacaoData = JsonSerializer.Deserialize<TransacaoModel>(requestBody, options);

    //recupera id da rota
    int id = Convert.ToInt32(context.Request.RouteValues["id"]);

    //Cria a transacao do cliente com data realizada
    var transacao = new Transacao(
        Valor: transacaoData.Valor,
        Tipo: transacaoData.Tipo,
        Descricao: transacaoData.Descricao,
        RealizadoEm: DateTime.Now,
        IdCliente: id
    );

    using var con = dbContext.GetConnection();

    //Recupera cliente pelo ID
    var cliente = await con.QueryFirstOrDefaultAsync<Cliente>(
        $"SELECT Id, Limite, SaldoInicial FROM Cliente WHERE Id = {id}"
    );

    //Valida se cliente existe
    if (cliente is null)
        return Results.NotFound("Cliente not found");

    //Adiciona transacao do cliente
    await con.InsertAsync(transacao);

    //Soma saldo novo do cliente
    var saldoNovo = cliente.SaldoInicial + transacao.Valor;

    //Atualiza saldo do cliente
    await con.QueryAsync($"UPDATE Cliente SET SaldoInicial = {saldoNovo} WHERE Id = {cliente.Id}");

    //Retorna response OK 200 com o limite do cliente e seu novo saldo
    return Results.Ok(new TransacoesResponse(cliente.Limite, saldoNovo));
});
// app.MapGet("/extrato-clientes", async (HttpContext context, DbContext dbContext) =>
// {

// });


app.UseHttpsRedirection();

app.Run();