import http from 'k6/http';
import { check, group, sleep, fail } from 'k6';
import { SharedArray } from 'k6/data';
import { Trend } from 'k6/metrics';

const BASE_URL_SALDO_DIARIO = __ENV.BASE_URL_SALDO_DIARIO_API || 'https://localhost:4445';
const IDENTITY_SERVER_URL = __ENV.IDENTITY_SERVER_URL || 'https://localhost:4435';

// Use as variáveis do ambiente
const LOGIN_PAGE_URL = `${IDENTITY_SERVER_URL}/Account/Login`;
const LOGIN_FORM_POST_URL = `${IDENTITY_SERVER_URL}/Account/Login`;

const TEST_USERNAME = __ENV.K6_USERNAME || 'opah-admin';
const TEST_PASSWORD = __ENV.K6_PASSWORD || 'Admin123!';

const saldoDiarioTrend = new Trend('saldo_diario_latencia_api');

const datasParaConsulta = new SharedArray('datas_para_consulta', function () {
    const data = [];
    const endDate = new Date();
    for (let i = 0; i < 30; i++) {
        const date = new Date(endDate);
        date.setDate(endDate.getDate() - i);
        data.push(date.toISOString().split('T')[0]);
    }
    return data;
});

let isLoggedIn = false;

export const options = {
    insecureSkipTLSVerify: true,
    stages: [
        { duration: '30s', target: 20 },
        { duration: '5m', target: 50 },
        { duration: '30s', target: 0 },
    ],
    thresholds: {
        http_req_failed: ['rate<0.05'],
        http_req_duration: ['p(95)<1000'],
        'http_req_duration{scenario:login_flow}': ['p(95)<2000'],
        'http_req_duration{scenario:consulta_saldo_diario}': ['p(95)<1000'],
        'saldo_diario_latencia_api': ['p(95)<1000'],
    },
    ext: {
        loadimpact: {
            scenario: 'consulta_saldo_diario',
        },
    },
};

function loginWithCookies() {
    group('Fluxo de Login IdentityServer', () => {
        let res = http.get(LOGIN_PAGE_URL, {
            tags: { scenario: 'login_flow' }
        });

        check(res, { 'GET Login Page: Status 200': (r) => r.status === 200 });

        if (res.status !== 200) {
            fail(`Falha crítica ao carregar página de login. Status: ${res.status}. Body: ${res.body}`);
        }

        check(res, {
            'GET Login Page: Contém formulário de login (username/password)': (r) => r.body.includes('name="username"') && r.body.includes('name="password"'),
            'GET Login Page: Contém formulário de login (form action)': (r) => r.body.includes('<form action="/Account/Login" method="post">')
        });

        const requestVerificationToken = res.html().find('input[name="__RequestVerificationToken"]').val();
        
        const loginPayload = {
            username: TEST_USERNAME,
            password: TEST_PASSWORD,
            rememberLogin: 'false',
            '__RequestVerificationToken': requestVerificationToken
        };

        const postParams = {
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            tags: { scenario: 'login_flow' }
        };

        res = http.post(LOGIN_FORM_POST_URL, loginPayload, postParams);

        const loginSuccess = check(res, {
            'POST Login: Status final 200 ou 302': (r) => r.status === 200 || r.status === 302,
            'POST Login: Possui cookies de autenticação': (r) => {
                const cookies = Object.keys(r.cookies);
                const hasAuthCookie = cookies.some(cookieName => 
                    cookieName.includes('.AspNetCore.Identity') || 
                    cookieName.includes('Identity.Application') ||
                    cookieName.includes('idsrv.session')
                );
                return hasAuthCookie;
            },
        });

        if (loginSuccess) {
            isLoggedIn = true;
        } else {
            isLoggedIn = false;
            fail(`Falha crítica de login, não é possível prosseguir. Status: ${res.status}. Body: ${res.body}`);
        }
    });
}

export default function () {
    if (__VU == 0 && !isLoggedIn) { // __VU == 0 para garantir que o login inicial só ocorra uma vez por VUs
        loginWithCookies();
        sleep(2);
    } else if (!isLoggedIn) {
        // Se não conseguiu logar na primeira tentativa tenta novamente 
        sleep(1);
        return; 
    }
    
    group('Consulta de Saldo Diário', () => {
        const dataAleatoria = datasParaConsulta[Math.floor(Math.random() * datasParaConsulta.length)];
        const url = `${BASE_URL_SALDO_DIARIO}/api/v1/saldo-diario/${dataAleatoria}`;

        const params = {
            tags: { scenario: 'consulta_saldo_diario' },
        };
        
        const res = http.get(url, params);

        check(res, {
            'Status 200': (r) => r.status === 200,
            'Corpo da resposta não é vazio': (r) => r.body && r.body.length > 0, // Adicionado r.body para segurança
            'Contém saldo': (r) => {
                try {
                    const jsonBody = JSON.parse(r.body);
                    return jsonBody.hasOwnProperty('balance');
                } catch (e) {
                    return false; // Falha na parse do JSON
                }
            },
            'Não é 401/403 (Sessão Válida)': (r) => r.status !== 401 && r.status !== 403,
        });
        
        saldoDiarioTrend.add(res.timings.duration);
        
        sleep(1);
    });
}