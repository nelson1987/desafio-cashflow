/**
 * Cashflow - Stress Test
 * 
 * Teste para encontrar o limite da aplicação.
 * Aumenta gradualmente a carga até a API começar a falhar.
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const responseTime = new Trend('response_time');

export const options = {
    stages: [
        { duration: '1m', target: 50 },    // Aquecimento
        { duration: '2m', target: 100 },   // Carga normal
        { duration: '2m', target: 200 },   // Stress
        { duration: '2m', target: 300 },   // Alto stress
        { duration: '2m', target: 400 },   // Limite
        { duration: '3m', target: 0 },     // Recovery
    ],
    thresholds: {
        'errors': ['rate<0.10'],           // < 10% de erros (mais tolerante)
        'http_req_duration': ['p(95)<500'], // 95% < 500ms
    },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

function getRandomDate() {
    const today = new Date();
    const daysAgo = Math.floor(Math.random() * 30);
    const date = new Date(today);
    date.setDate(date.getDate() - daysAgo);
    return date.toISOString().split('T')[0];
}

export default function () {
    const data = getRandomDate();
    
    const startTime = Date.now();
    const response = http.get(`${BASE_URL}/api/consolidado/${data}`);
    const latency = Date.now() - startTime;
    
    responseTime.add(latency);

    const success = check(response, {
        'status is 200': (r) => r.status === 200,
        'response time < 500ms': (r) => r.timings.duration < 500,
    });

    errorRate.add(!success);
    
    sleep(0.1);
}

export function setup() {
    console.log(`💪 Stress Test - Encontrando o limite da API`);
    console.log(`📍 URL: ${BASE_URL}`);
    console.log(`📈 Carga: 50 → 100 → 200 → 300 → 400 VUs`);
}

export function teardown() {
    console.log(`\n📊 Stress Test concluído`);
    console.log(`   Use os resultados para identificar o ponto de ruptura.`);
}

