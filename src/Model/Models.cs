using System.Text.Json.Serialization;

namespace rinha_de_backend_2024_q1_dotnet.Models;

#region request

public sealed class TransacaoRequest
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

public record struct TransacaoModel(int Valor, string Tipo, string Descricao);

#endregion

#region response

public sealed class Cliente
{
    [JsonPropertyName("id")] // Mapeando para o campo 'id' do banco de dados
    public int Id { get; set; }
        
    [JsonPropertyName("limite")] // Mapeando para o campo 'limite' do banco de dados
    public int Limite { get; set; }
        
    [JsonPropertyName("saldo")] // Mapeando para o campo 'saldo' do banco de dados
    public int Saldo { get; set; }
}

// public record struct Extrato(Saldo Saldo, IEnumerable<Transacao> UltimasTransacoes);

public record struct Saldo(int Total, DateTime DataExtrato, int Limite);

public sealed record TransacoesResponse(int limite, int saldo);

#endregion

[JsonSerializable(typeof(TransacaoRequest))]
[JsonSerializable(typeof(TransacoesResponse))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Cliente))]

public partial class SourceGenerationContext
    : JsonSerializerContext
{
}