CREATE TABLE IF NOT EXISTS cliente (
    id INTEGER PRIMARY KEY,
    limite INTEGER NOT NULL,
    saldo INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS transacao (
    id INTEGER PRIMARY KEY,
    idCliente INTEGER NOT NULL,
    valor INTEGER NOT NULL,
    tipo CHAR(1) NOT NULL,
    descricao VARCHAR(10) NOT NULL,
    realizada_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (idCliente) REFERENCES cliente(id)
);

CREATE INDEX idx_cliente_id_saldo ON cliente (id, saldo);
CREATE INDEX idx_transacao_idCliente ON transacao (idCliente);
CREATE INDEX idx_cliente_id_saldo_limite ON cliente (id, saldo, limite);
CREATE INDEX idx_realizado_em ON transacao (realizado_em);

INSERT INTO cliente (id, limite, valor)
VALUES 
    (1, 1000 * 100, 0),
    (2, 800 * 100, 0),
    (3, 10000 * 100, 0),
    (4, 100000 * 100, 0),
    (5, 5000 * 100, 0);

