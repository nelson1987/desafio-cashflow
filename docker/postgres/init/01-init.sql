-- ============================================
-- Cashflow - Script de Inicialização do PostgreSQL
-- ============================================
-- Este script é executado automaticamente na
-- primeira inicialização do container
-- ============================================

-- Cria extensões úteis
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Schema para a aplicação
CREATE SCHEMA IF NOT EXISTS cashflow;

-- Tabela de Lançamentos
CREATE TABLE IF NOT EXISTS cashflow.lancamentos (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    valor DECIMAL(18, 2) NOT NULL CHECK (valor > 0),
    tipo SMALLINT NOT NULL CHECK (tipo IN (1, 2)), -- 1: Crédito, 2: Débito
    data TIMESTAMP NOT NULL,
    descricao VARCHAR(500) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Tabela de Saldos Consolidados (Cache/Materialized)
CREATE TABLE IF NOT EXISTS cashflow.saldos_consolidados (
    data DATE PRIMARY KEY,
    total_creditos DECIMAL(18, 2) NOT NULL DEFAULT 0,
    total_debitos DECIMAL(18, 2) NOT NULL DEFAULT 0,
    saldo DECIMAL(18, 2) NOT NULL DEFAULT 0,
    quantidade_lancamentos INT NOT NULL DEFAULT 0,
    processado_em TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índices para otimização de consultas
CREATE INDEX IF NOT EXISTS idx_lancamentos_data ON cashflow.lancamentos(data);
CREATE INDEX IF NOT EXISTS idx_lancamentos_tipo ON cashflow.lancamentos(tipo);
CREATE INDEX IF NOT EXISTS idx_lancamentos_data_tipo ON cashflow.lancamentos(data, tipo);

-- Comentários para documentação
COMMENT ON TABLE cashflow.lancamentos IS 'Tabela de lançamentos financeiros (débitos e créditos)';
COMMENT ON COLUMN cashflow.lancamentos.tipo IS '1 = Crédito (entrada), 2 = Débito (saída)';
COMMENT ON TABLE cashflow.saldos_consolidados IS 'Cache de saldos consolidados por dia';

-- Função para atualizar updated_at automaticamente
CREATE OR REPLACE FUNCTION cashflow.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger para atualizar updated_at
DROP TRIGGER IF EXISTS update_lancamentos_updated_at ON cashflow.lancamentos;
CREATE TRIGGER update_lancamentos_updated_at
    BEFORE UPDATE ON cashflow.lancamentos
    FOR EACH ROW
    EXECUTE FUNCTION cashflow.update_updated_at_column();

-- Dados de exemplo para desenvolvimento
INSERT INTO cashflow.lancamentos (valor, tipo, data, descricao) VALUES
    (1000.00, 1, '2024-01-15 10:00:00', 'Venda de produtos - Cliente A'),
    (500.00, 1, '2024-01-15 14:30:00', 'Serviço prestado'),
    (200.00, 2, '2024-01-15 16:00:00', 'Pagamento fornecedor'),
    (150.00, 2, '2024-01-16 09:00:00', 'Conta de luz'),
    (2500.00, 1, '2024-01-16 11:00:00', 'Venda grande - Cliente B'),
    (800.00, 2, '2024-01-16 15:00:00', 'Aluguel do mês'),
    (350.00, 1, '2024-01-17 10:30:00', 'Venda de serviço'),
    (120.00, 2, '2024-01-17 14:00:00', 'Material de escritório');

-- Consolida os saldos de exemplo
INSERT INTO cashflow.saldos_consolidados (data, total_creditos, total_debitos, saldo, quantidade_lancamentos)
SELECT 
    DATE(data) as data,
    SUM(CASE WHEN tipo = 1 THEN valor ELSE 0 END) as total_creditos,
    SUM(CASE WHEN tipo = 2 THEN valor ELSE 0 END) as total_debitos,
    SUM(CASE WHEN tipo = 1 THEN valor ELSE -valor END) as saldo,
    COUNT(*) as quantidade_lancamentos
FROM cashflow.lancamentos
GROUP BY DATE(data)
ON CONFLICT (data) DO UPDATE SET
    total_creditos = EXCLUDED.total_creditos,
    total_debitos = EXCLUDED.total_debitos,
    saldo = EXCLUDED.saldo,
    quantidade_lancamentos = EXCLUDED.quantidade_lancamentos,
    processado_em = CURRENT_TIMESTAMP;

-- Grant de permissões
GRANT USAGE ON SCHEMA cashflow TO cashflow;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA cashflow TO cashflow;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA cashflow TO cashflow;

-- Mensagem de conclusão
DO $$
BEGIN
    RAISE NOTICE '✅ Banco de dados Cashflow inicializado com sucesso!';
    RAISE NOTICE '📊 Tabelas criadas: lancamentos, saldos_consolidados';
    RAISE NOTICE '🔧 Índices criados para otimização';
    RAISE NOTICE '📝 Dados de exemplo inseridos';
END $$;

