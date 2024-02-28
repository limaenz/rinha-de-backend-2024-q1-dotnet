BEGIN;

INSERT INTO transacao
(
    valor,
    tipo,
    descricao,
    realizadoEm,
    idCliente
)
VALUES
    (@Valor, @Tipo, @Descricao, CURRENT_TIMESTAMP, @Id);

UPDATE cliente
SET saldo = (saldo - @Valor)
WHERE id = @Id
RETURNING saldo;

COMMIT;
