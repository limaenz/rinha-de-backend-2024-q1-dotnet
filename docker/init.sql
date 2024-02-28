CREATE UNLOGGED TABLE cliente (
    id SERIAL PRIMARY KEY,
    limite INTEGER NOT NULL,
    saldo INTEGER NOT NULL
);

CREATE UNLOGGED TABLE transacao (
    id SERIAL PRIMARY KEY,
    idCliente INTEGER NOT NULL,
    valor INTEGER NOT NULL,
    tipo CHAR(1) NOT NULL,
    descricao VARCHAR(10) NOT NULL,
    realizadoEm TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (idCliente) REFERENCES cliente(id)
);


CREATE INDEX idx_cliente_id_saldo ON cliente (id, saldo);
CREATE INDEX idx_transacao_idCliente ON transacao (idCliente);
CREATE INDEX idx_cliente_id_saldo_limite ON cliente (id, saldo, limite);

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
