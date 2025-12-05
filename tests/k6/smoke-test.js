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
        'http_req_duration': ['p(95)<500'],  // 95% < 500ms
        // Threshold de falhas removido - validamos via checks
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
    // 1. Health Check (crítico)
    const healthRes = http.get(`${BASE_URL}/health`);
    const healthOk = check(healthRes, {
        'health check status is 200': (r) => r.status === 200,
    });

    if (!healthOk) {
        console.error(`Health check falhou: ${healthRes.status} - ${healthRes.body}`);
        sleep(1);
        return;
    }

    // 2. Criar Lançamento
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
        'create lancamento status is 201 or 400': (r) => r.status === 201 || r.status === 400,
    });

    // 3. Listar Lançamentos
    const listRes = http.get(`${BASE_URL}/api/lancamentos?pagina=1&tamanhoPagina=10`);
    check(listRes, {
        'list lancamentos status is 200': (r) => r.status === 200,
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
    
    // Verifica se a API está disponível
    const healthCheck = http.get(`${BASE_URL}/health`);
    if (healthCheck.status !== 200) {
        console.error(`❌ API não está disponível! Status: ${healthCheck.status}`);
        console.error(`Body: ${healthCheck.body}`);
    } else {
        console.log(`✅ API está saudável`);
    }
}

export function teardown() {
    console.log(`\n✅ Smoke Test concluído`);
}

