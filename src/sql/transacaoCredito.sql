BEGIN TRANSACTION;
INSERT INTO transacao
(
    valor,
    tipo,
    descricao,
    realizadoEm,
    idcliente
)
VALUES
    (@Valor, @Tipo, @Descricao, datetime('now'), @Id);
UPDATE cliente
SET saldo = (saldo - @Valor)
WHERE id = @Id;
SELECT saldo
FROM cliente
WHERE id = @Id;
COMMIT TRANSACTION;