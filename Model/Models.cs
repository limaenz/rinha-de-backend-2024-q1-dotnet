using System.ComponentModel.DataAnnotations.Schema;

namespace rinha_de_backend_2024_q1_dotnet.Model;

#region request

[Table("Transacao")]
public sealed record Transacao(int Valor, string Tipo, string Descricao, DateTime RealizadoEm, int IdCliente);

public record struct TransacaoModel(int Valor, string Tipo, string Descricao);

#endregion

#region response

public sealed class Cliente
{
    public int Id { get; set; }
    public int Limite { get; set; }
    public int SaldoInicial { get; set; }
}

public record struct Extrato(Saldo Saldo, IEnumerable<Transacao> UltimasTransacoes);

public record struct Saldo(int Total, DateTime DataExtrato, int Limite);

public sealed record TransacoesResponse(int limite, int saldo);

#endregion