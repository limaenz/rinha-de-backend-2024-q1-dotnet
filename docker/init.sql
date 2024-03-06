CREATE UNLOGGED TABLE cliente (
    id SERIAL PRIMARY KEY,
    limite INTEGER NOT NULL,
    saldo INTEGER NOT NULL DEFAULT 0
);

CREATE UNLOGGED TABLE transacao (
    id SERIAL PRIMARY KEY,
    idCliente INTEGER NOT NULL,
    valor INTEGER NOT NULL,
    tipo CHAR(1) NOT NULL,
    descricao VARCHAR(10) NOT NULL,
    realizadoEm TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_transacao_idCliente ON transacao (idCliente);
CREATE INDEX idx_idCliente_realizadoEm ON transacao (idCliente, realizadoEm DESC);

DO $$
BEGIN
INSERT INTO cliente (id, limite, saldo)
VALUES 
    (1, 1000 * 100, 0),
    (2, 800 * 100, 0),
    (3, 10000 * 100, 0),
    (4, 100000 * 100, 0),
    (5, 5000 * 100, 0);
END;
$$;

CREATE OR REPLACE FUNCTION realizar_credito(
    id_cliente INT,
    novo_valor INT,
    descricao_cd VARCHAR(10))
RETURNS TABLE (
	novo_saldo INT,
	possui_erro BOOL,
	mensagem VARCHAR(20))
LANGUAGE plpgsql 
AS $$
BEGIN
    PERFORM pg_advisory_xact_lock(id_cliente);

    INSERT INTO transacao (valor, tipo, descricao, realizadoEm, idcliente)
    VALUES (novo_valor, 'c', descricao_cd, NOW(), id_cliente);

    RETURN QUERY
    UPDATE cliente
    SET saldo = saldo + novo_valor
    WHERE id = id_cliente
	RETURNING saldo, FALSE, 'ok'::VARCHAR(20);
END;
$$;

CREATE OR REPLACE FUNCTION realizar_debito(
    id_cliente INT,
    novo_valor INT,
    descricao_db VARCHAR(10))
RETURNS TABLE (
	novo_saldo INT,
	possui_erro BOOL,
	mensagem VARCHAR(20))
LANGUAGE plpgsql
AS $$
DECLARE
    saldo_cliente INT;
    limite_cliente INT; 
BEGIN
    PERFORM pg_advisory_xact_lock(id_cliente);

    SELECT saldo, limite
    INTO saldo_cliente, limite_cliente 
    FROM cliente WHERE id = id_cliente;

    IF saldo_cliente - novo_valor >= limite_cliente * -1 THEN 
        INSERT INTO transacao (valor, tipo, descricao, realizadoEm, idcliente)
        VALUES (novo_valor, 'd', descricao_db, NOW(), id_cliente);

        UPDATE cliente
        SET saldo = saldo - novo_valor
        WHERE id = id_cliente;

        RETURN QUERY SELECT saldo, FALSE, 'ok'::VARCHAR(20) FROM cliente WHERE id = id_cliente;
    ELSE
        RETURN QUERY SELECT saldo, TRUE, 'saldo insuficiente'::VARCHAR(20) FROM cliente WHERE id = id_cliente;
    END IF;
END;
$$;


