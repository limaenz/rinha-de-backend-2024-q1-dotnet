BEGIN;

-- Realiza o bloqueio pessimista na tabela cliente
SELECT CASE
           WHEN
               (
                   SELECT saldo + @Valor FROM cliente WHERE id = @Id FOR UPDATE
               ) <
               (
                   SELECT limite FROM cliente WHERE id = @Id FOR UPDATE
               ) THEN
               1
           ELSE
               0
       END AS condicao;

-- Insere a transação
INSERT INTO transacao
(
    valor,
    tipo,
    descricao,
    realizadoEm,
    idcliente
)
VALUES
    (@Valor, @Tipo, @Descricao, NOW(), @Id);

-- Atualiza o saldo do cliente
UPDATE cliente
SET saldo = saldo - @Valor
WHERE id = @Id
  AND (saldo + @Valor >= limite)
RETURNING saldo;

COMMIT;
