/**
 * Cashflow - Teste de Carga dos Lançamentos
 * 
 * Testa endpoints de lançamentos:
 * - POST /api/lancamentos (criar)
 * - GET /api/lancamentos (listar)
 * - GET /api/lancamentos/{id} (obter por ID)
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Métricas customizadas
const errorRate = new Rate('errors');
const createLatency = new Trend('lancamento_create_latency');
const listLatency = new Trend('lancamento_list_latency');
const lancamentosCriados = new Counter('lancamentos_criados');

// Configuração do teste
export const options = {
    scenarios: {
        // Cenário misto: criar e listar
        mixed_workload: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '30s', target: 20 },   // Ramp up
                { duration: '2m', target: 50 },    // Carga sustentada
                { duration: '30s', target: 100 },  // Pico
                { duration: '1m', target: 100 },   // Mantém pico
                { duration: '30s', target: 0 },    // Ramp down
            ],
            gracefulRampDown: '30s',
        },
    },
    thresholds: {
        // Tempo de resposta para criação
        'lancamento_create_latency': ['p(95)<150'],  // 95% < 150ms
        
        // Tempo de resposta para listagem
        'lancamento_list_latency': ['p(95)<100'],    // 95% < 100ms
        
        // Taxa de erro geral
        'http_req_failed': ['rate<0.05'],            // < 5% de falhas
        'errors': ['rate<0.05'],
    },
};

// Configurações
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const lancamentosIds = [];

// Gera dados aleatórios para lançamento
function gerarLancamento() {
    const tipos = [0, 1]; // 0 = Credito, 1 = Debito
    const descricoes = [
        'Venda de produto',
        'Pagamento de fornecedor',
        'Serviço prestado',
        'Conta de luz',
        'Aluguel',
        'Salário',
        'Comissão',
        'Manutenção',
    ];
    
    return {
        valor: Math.round((Math.random() * 1000 + 10) * 100) / 100,
        tipo: tipos[Math.floor(Math.random() * tipos.length)],
        data: new Date().toISOString(),
        descricao: descricoes[Math.floor(Math.random() * descricoes.length)],
    };
}

export default function () {
    // 70% das requisições são leituras, 30% são escritas
    const isWrite = Math.random() < 0.3;
    
    if (isWrite) {
        group('Criar Lançamento', function () {
            criarLancamento();
        });
    } else {
        group('Listar Lançamentos', function () {
            listarLancamentos();
        });
    }
    
    sleep(0.5);
}

function criarLancamento() {
    const payload = JSON.stringify(gerarLancamento());
    
    const params = {
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
        },
        tags: { name: 'CreateLancamento' },
    };

    const startTime = Date.now();
    const response = http.post(`${BASE_URL}/api/lancamentos`, payload, params);
    const latency = Date.now() - startTime;

    createLatency.add(latency);

    const success = check(response, {
        'status is 201': (r) => r.status === 201,
        'response time < 150ms': (r) => r.timings.duration < 150,
        'has id in response': (r) => {
            try {
                const body = JSON.parse(r.body);
                if (body.id) {
                    lancamentosIds.push(body.id);
                    return true;
                }
                return false;
            } catch {
                return false;
            }
        },
    });

    if (success) {
        lancamentosCriados.add(1);
    }
    
    errorRate.add(!success);
}

function listarLancamentos() {
    const pagina = Math.floor(Math.random() * 5) + 1;
    const url = `${BASE_URL}/api/lancamentos?pagina=${pagina}&tamanhoPagina=10`;
    
    const params = {
        headers: {
            'Accept': 'application/json',
        },
        tags: { name: 'ListLancamentos' },
    };

    const startTime = Date.now();
    const response = http.get(url, params);
    const latency = Date.now() - startTime;

    listLatency.add(latency);

    const success = check(response, {
        'status is 200': (r) => r.status === 200,
        'response time < 100ms': (r) => r.timings.duration < 100,
        'has items array': (r) => {
            try {
                const body = JSON.parse(r.body);
                return Array.isArray(body.items) || Array.isArray(body.Items);
            } catch {
                return false;
            }
        },
    });

    errorRate.add(!success);
}

export function setup() {
    console.log(`🚀 Iniciando teste de carga - Lançamentos`);
    console.log(`📍 URL Base: ${BASE_URL}`);
    
    const healthCheck = http.get(`${BASE_URL}/health`);
    if (healthCheck.status !== 200) {
        console.error(`❌ API não está disponível!`);
        return { healthy: false };
    }
    
    console.log(`✅ API está saudável`);
    return { healthy: true };
}

export function teardown(data) {
    console.log(`\n📊 Teste de Lançamentos finalizado`);
    console.log(`   - Workload: 70% leituras, 30% escritas`);
}

