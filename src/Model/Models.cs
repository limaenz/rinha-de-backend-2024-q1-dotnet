using System.Text.Json.Serialization;

#region request

public record struct TransacaoRequest
{
    public object  Valor { get; set; }
    public string? Tipo { get; set; }
    public string? Descricao { get; set; }

    private readonly static string[] Tipos = ["c", "d"];

    public bool Valida()
        => Tipos.Contains(Tipo);
}

#endregion

#region response

public record struct Extrato(Saldo Saldo, IEnumerable<TransacoesExtratoResponse> UltimasTransacoes);

public record struct Saldo(int Total, DateTime DataExtrato, int Limite);

public record struct TransacoesResponse(int saldo, int limite);

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