/**
 * Cashflow - Smoke Test
 * 
 * Teste rápido para validar se a API está funcionando.
 * Executar antes dos testes de carga.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 1,
    duration: '30s',
    thresholds: {
        'http_req_failed': ['rate<0.01'],    // < 1% de falhas
        'http_req_duration': ['p(95)<500'],  // 95% < 500ms
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
    // 1. Health Check
    const healthRes = http.get(`${BASE_URL}/health`);
    check(healthRes, {
        'health check status is 200': (r) => r.status === 200,
    });

    // 2. Listar Lançamentos
    const listRes = http.get(`${BASE_URL}/api/lancamentos?pagina=1&tamanhoPagina=10`);
    check(listRes, {
        'list lancamentos status is 200': (r) => r.status === 200,
    });

    // 3. Criar Lançamento
    const payload = JSON.stringify({
        valor: 100.50,
        tipo: 0,
        data: new Date().toISOString(),
        descricao: 'Smoke Test',
    });
    
    const createRes = http.post(`${BASE_URL}/api/lancamentos`, payload, {
        headers: { 'Content-Type': 'application/json' },
    });
    check(createRes, {
        'create lancamento status is 201': (r) => r.status === 201,
    });

    // 4. Obter Consolidado
    const hoje = new Date().toISOString().split('T')[0];
    const consolidadoRes = http.get(`${BASE_URL}/api/consolidado/${hoje}`);
    check(consolidadoRes, {
        'get consolidado status is 200': (r) => r.status === 200,
    });

    sleep(1);
}

export function setup() {
    console.log(`🔥 Smoke Test - Validação rápida da API`);
    console.log(`📍 URL: ${BASE_URL}`);
}

export function teardown() {
    console.log(`\n✅ Smoke Test concluído`);
}

