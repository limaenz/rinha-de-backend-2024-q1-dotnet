SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

CREATE UNLOGGED TABLE cliente (
    id SERIAL PRIMARY KEY,
    limite INTEGER NOT NULL,
    saldo INTEGER NOT NULL DEFAULT 0
);

CREATE UNLOGGED TABLE transacao (
    id SERIAL PRIMARY KEY,
    idCliente INTEGER NOT NULL,
    valor INTEGER NOT NULL,
    descricao VARCHAR(10) NOT NULL,
    realizadoEm TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_transacao_idCliente ON transacao (idCliente ASC);

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

CREATE OR REPLACE FUNCTION criartransacao(
  IN id_cliente integer,
  IN valor integer,
  IN descricao varchar(10)
) RETURNS RECORD AS $$
DECLARE
  clienteencontrado cliente%rowtype;
  ret RECORD;
BEGIN
  SELECT * FROM cliente
  INTO clienteencontrado
  WHERE id = id_cliente;

  IF not found THEN
    --raise notice'Id do Cliente % não encontrado.', idcliente;
    SELECT -1 INTO ret;
    RETURN ret;
  END IF;

  UPDATE cliente
    SET saldo = saldo + valor
    WHERE id = id_cliente AND (valor > 0 OR saldo + valor >= limite)
    RETURNING saldo, limite
    INTO ret;
  raise notice'Ret: %', ret;
  IF ret.limite is NULL THEN
    SELECT -2 INTO ret;
    RETURN ret;
  END IF;
  INSERT INTO transacao (valor, descricao, idCliente)
    VALUES (valor, descricao, id_cliente);
  RETURN ret;
END;$$ LANGUAGE plpgsql;


