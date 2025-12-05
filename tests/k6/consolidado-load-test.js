/**
 * Cashflow - Teste de Carga do Consolidado Diário
 * 
 * Requisitos não-funcionais:
 * - 50 requisições por segundo em produção
 * - Testamos com 55 RPS (10% acima)
 * - Tempo máximo de resposta: 100ms (P95)
 * - Taxa de erro máxima: 5%
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// Métricas customizadas
const errorRate = new Rate('errors');
const consolidadoLatency = new Trend('consolidado_latency');

// Configuração do teste
export const options = {
    scenarios: {
        // Cenário 1: Rampa de aquecimento
        warmup: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [
                { duration: '30s', target: 10 },  // Sobe para 10 VUs
            ],
            gracefulRampDown: '10s',
            exec: 'consolidadoTest',
        },
        // Cenário 2: Carga sustentada (55 RPS)
        sustained_load: {
            executor: 'constant-arrival-rate',
            rate: 55,                    // 55 requisições por segundo
            timeUnit: '1s',
            duration: '2m',              // 2 minutos de carga sustentada
            preAllocatedVUs: 100,        // VUs pré-alocados
            maxVUs: 200,                 // Máximo de VUs
            startTime: '30s',            // Inicia após warmup
            exec: 'consolidadoTest',
        },
        // Cenário 3: Pico de carga (teste de stress)
        spike: {
            executor: 'ramping-arrival-rate',
            startRate: 55,
            timeUnit: '1s',
            stages: [
                { duration: '30s', target: 100 },  // Sobe para 100 RPS
                { duration: '1m', target: 100 },   // Mantém 100 RPS
                { duration: '30s', target: 55 },   // Volta para 55 RPS
            ],
            preAllocatedVUs: 200,
            maxVUs: 300,
            startTime: '2m30s',          // Inicia após sustained_load
            exec: 'consolidadoTest',
        },
    },
    thresholds: {
        // Tempo de resposta
        'http_req_duration': ['p(95)<100'],           // 95% < 100ms
        'http_req_duration': ['p(99)<200'],           // 99% < 200ms
        'consolidado_latency': ['p(95)<100'],         // Métrica customizada
        
        // Taxa de erro
        'http_req_failed': ['rate<0.05'],             // < 5% de falhas
        'errors': ['rate<0.05'],                      // < 5% de erros
        
        // Disponibilidade
        'http_reqs': ['rate>50'],                     // > 50 req/s
    },
};

// Configurações
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

// Datas para teste (últimos 30 dias)
function getRandomDate() {
    const today = new Date();
    const daysAgo = Math.floor(Math.random() * 30);
    const date = new Date(today);
    date.setDate(date.getDate() - daysAgo);
    return date.toISOString().split('T')[0];
}

// Teste principal: GET /api/consolidado/{data}
export function consolidadoTest() {
    const data = getRandomDate();
    const url = `${BASE_URL}/api/consolidado/${data}`;
    
    const params = {
        headers: {
            'Accept': 'application/json',
        },
        tags: { name: 'GetConsolidado' },
    };

    const startTime = Date.now();
    const response = http.get(url, params);
    const latency = Date.now() - startTime;

    // Registra latência customizada
    consolidadoLatency.add(latency);

    // Validações
    const success = check(response, {
        'status is 200': (r) => r.status === 200,
        'response time < 100ms': (r) => r.timings.duration < 100,
        'has valid JSON': (r) => {
            try {
                const body = JSON.parse(r.body);
                return body !== null;
            } catch {
                return false;
            }
        },
        'has saldo field': (r) => {
            try {
                const body = JSON.parse(r.body);
                return 'saldo' in body || 'Saldo' in body;
            } catch {
                return false;
            }
        },
    });

    // Registra erro se falhou
    errorRate.add(!success);

    // Pequena pausa para simular comportamento real
    sleep(0.1);
}

// Setup: executado uma vez antes do teste
export function setup() {
    console.log(`🚀 Iniciando teste de carga`);
    console.log(`📍 URL Base: ${BASE_URL}`);
    console.log(`📊 Meta: 55 RPS, P95 < 100ms`);
    
    // Verifica se a API está disponível
    const healthCheck = http.get(`${BASE_URL}/health`);
    if (healthCheck.status !== 200) {
        console.error(`❌ API não está disponível! Status: ${healthCheck.status}`);
        return { healthy: false };
    }
    
    console.log(`✅ API está saudável`);
    return { healthy: true, startTime: Date.now() };
}

// Teardown: executado uma vez após o teste
export function teardown(data) {
    if (!data.healthy) {
        console.error('❌ Teste abortado - API não estava disponível');
        return;
    }
    
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`\n📊 Teste finalizado em ${duration.toFixed(2)}s`);
    console.log(`\n🎯 Requisitos não-funcionais:`);
    console.log(`   - 50 RPS em produção (testado com 55 RPS)`);
    console.log(`   - Tempo máximo: 100ms (P95)`);
    console.log(`   - Taxa de erro: < 5%`);
}

