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
    (@Valor, 'c', @Descricao, NOW(), @Id);

UPDATE cliente
SET saldo = (saldo + @Valor)
WHERE id = @Id
RETURNING saldo;

COMMIT;
