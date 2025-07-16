import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

// const BASE_URL = 'https://localhost:57939';  // No Docker
const BASE_URL = 'https://localhost:53036'; 

const LOGIN_ENDPOINT = `${BASE_URL}/api/auth/login`;
const TEST_USERNAME = __ENV.K6_USERNAME || 'opah-admin'; // para produção, usar variáveis de ambiente
const TEST_PASSWORD = __ENV.K6_PASSWORD || 'Admin123!';

const ids = new SharedArray('ids', function () {
    const data = [];
    for (let i = 0; i < 100; i++) {
        data.push(uuidv4());
    }
    return data;
});

let authToken = null;
let tokenExpiresAt = 0; 

export const options = {
	insecureSkipTLSVerify: true,
    // Cenário de rampa para simular aproximadamente 50 RPS
    stages: [
        { duration: '30s', target: 20 },  // Estágio de 20 VUs em 30 segundos
        { duration: '5m', target: 50 },   // 50 VUs por 5 minutos (carga sustentada)
        { duration: '30s', target: 0 },   // Estágio de 0 VUs em 30 segundos
    ],
	// Configuração da regra do teste para 95% de disponibilidade e 5% de falha
    thresholds: {
        http_req_failed: ['rate<0.05'], // Taxa de falha deve ser menor que 5%
        http_req_duration: ['p(95)<1000'], // 95% das requisições devem ser concluídas em menos de 1 segundo
        'http_req_duration{scenario:insercao_lancamentos}': ['p(95)<1000'], // Latência específica para este cenário
    },
    ext: {
        loadimpact: {
            scenario: 'insercao_lancamentos',
        },
    },
};

// --- FUNÇÃO DE LOGIN ---
function login() {
    console.log(`VU ${__VU}: Tentando login...`);
    const loginPayload = JSON.stringify({
        username: TEST_USERNAME,
        password: TEST_PASSWORD,
    });

    const loginParams = {
        headers: {
            'Content-Type': 'application/json',
        },
        tags: { scenario: 'login' }, 
    };

    const res = http.post(LOGIN_ENDPOINT, loginPayload, loginParams);

    check(res, {
        'Login: Status é 200': (r) => r.status === 200,
        'Login: Token recebido': (r) => r.json() && r.json().token !== undefined,
    });

    if (res.status === 200 && res.json() && res.json().token) {
        authToken = res.json().token;
        tokenExpiresAt = Date.now() + (60 * 60 * 1000) - (5 * 60 * 1000); // Expira em 1h
        console.log(`VU ${__VU}: Login bem-sucedido. Token obtido`);
    } else {
        console.error(`VU ${__VU}: Falha no login! Status: ${res.status}, Corpo: ${res.body}`);
        fail('Falha crítica de login, não é possível prosseguir'); 
    }
}

export default function () {
    group('Criação do Lançamento', () => {
        const url = `${BASE_URL}/api/lancamentos`;
        const randomId = ids[Math.floor(Math.random() * ids.length)];

        const payload = JSON.stringify({
            contaId: randomId,
            tipo: Math.random() < 0.5 ? 0 : 1,
            valor: parseFloat((Math.random() * 1000).toFixed(2)), // Valor aleatório
            dataLancamento: new Date().toISOString(),
            descricao: `Transação de teste de carga - ${uuidv4()}` // Descrição única
        });

        const params = {
            headers: {
                'Content-Type': 'application/json',
            },
        };

        const res = http.post(url, payload, params);

        check(res, {
            'Status 200/201': (r) => r.status === 200 || r.status === 201,
            'Corpo da resposta não é vazio': (r) => r.body.length > 0,
        });
        
        // Tempo de 'think time' entre as requisições
        sleep(1); // Sleep de 1 segundo entre as iterações de cada VU
    });
}