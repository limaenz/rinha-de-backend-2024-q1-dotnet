BEGIN TRANSACTION;
SELECT CASE
           WHEN
               (
                   SELECT saldo + @Valor FROM cliente WHERE id = @Id
               ) <
               (
                   SELECT limite FROM cliente WHERE id = @Id
               ) THEN
               1
           ELSE
               0
           END AS condicao;
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
SET saldo = (saldo + @Valor)
WHERE id = @Id
  AND (saldo + @Valor >= limite);
SELECT saldo
FROM cliente
WHERE id = @Id;
COMMIT TRANSACTION;