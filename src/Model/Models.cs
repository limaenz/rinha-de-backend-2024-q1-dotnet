using System.Text.Json.Serialization;

namespace rinha_de_backend_2024_q1_dotnet.Models;

#region request

public sealed record TransacaoRequest
{
    public int Valor { get; set; }
    public string? Tipo { get; set; }
    public string? Descricao { get; set; }

    private readonly static string[] Tipos = ["c", "d"];

    public bool Valida()
        => Tipos.Contains(Tipo)
           && !string.IsNullOrEmpty(Descricao)
           && Descricao.Length <= 10
           && Valor > 0;
}

#endregion

#region response

public sealed record Extrato(Saldo Saldo, IEnumerable<TransacaoRequest> UltimasTransacoes);

public record struct Saldo(int Total, DateTime DataExtrato, int Limite);

public sealed record TransacoesResponse(int limite, int saldo);

#endregion

[JsonSerializable(typeof(TransacaoRequest))]
[JsonSerializable(typeof(TransacoesResponse))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(Extrato))]
[JsonSerializable(typeof(Saldo))]

public partial class SourceGenerationContext
    : JsonSerializerContext
{
}