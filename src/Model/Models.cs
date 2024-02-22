using System.Text.Json.Serialization;

namespace rinha_de_backend_2024_q1_dotnet.Models;

#region request

public record struct TransacaoRequest
{
    public int Valor { get; set; }
    public string? Tipo { get; set; }
    public string? Descricao { get; set; }

    private readonly static string[] Tipos = ["c", "d"];

    public bool Valida()
        => Tipos.Contains(Tipo ?? string.Empty)
           && !string.IsNullOrEmpty(Descricao)
           && Descricao.Length <= 10
           && Valor > 0;
}

#endregion

#region response

public record struct Extrato(Saldo Saldo, IEnumerable<TransacoesExtratoResponse> UltimasTransacoes);

public record struct Saldo(int Total, DateTime DataExtrato, int Limite);

public record struct TransacoesResponse(int limite, int saldo);

public record struct TransacoesExtratoResponse(int Valor, string Tipo, string Descricao, DateTime RealizadoEm);

#endregion

[JsonSerializable(typeof(TransacaoRequest))]
[JsonSerializable(typeof(TransacoesResponse))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(Extrato))]
[JsonSerializable(typeof(Saldo))]
[JsonSerializable(typeof(TransacoesExtratoResponse))]

public partial class SourceGenerationContext
    : JsonSerializerContext
{
}