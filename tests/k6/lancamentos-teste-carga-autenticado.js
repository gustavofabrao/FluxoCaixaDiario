import http from 'k6/http';
import { check, group, sleep, fail } from 'k6';
import { SharedArray } from 'k6/data';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const BASE_URL = 'https://localhost:53036';
const BASE_URL_LOGIN = 'https://localhost:4435';

const LOGIN_PAGE_URL = `${BASE_URL_LOGIN}/Account/Login`;
const LOGIN_FORM_POST_URL = `${BASE_URL_LOGIN}/Account/Login`;

const TEST_USERNAME = __ENV.K6_USERNAME || 'opah-admin';
const TEST_PASSWORD = __ENV.K6_PASSWORD || 'Admin123!';

const ids = new SharedArray('ids', function () {
    const data = [];
    for (let i = 0; i < 100; i++) {
        data.push(uuidv4());
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
        'http_req_duration{scenario:insercao_lancamentos}': ['p(95)<1000'],
    },
    ext: {
        loadimpact: {
            scenario: 'insercao_lancamentos',
        },
    },
};

function loginWithCookies() {
    group('Fluxo de Login IdentityServer', () => {
        let res = http.get(LOGIN_PAGE_URL, {
            tags: { scenario: 'login_flow' }
        });

        check(res, {
            'GET Login Page: Status 200': (r) => r.status === 200,
            'GET Login Page: Contém formulário de login': (r) => r.body.includes('name="username"') && r.body.includes('name="password"')
        });

        if (res.status !== 200) {
            fail('Falha crítica ao carregar página de login.');
        }

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
            fail('Falha crítica de login, não é possível prosseguir.');
        }
    });
}

export default function () {
    if (!isLoggedIn) { 
        loginWithCookies();
        sleep(2);
    }

    if (!isLoggedIn) {
        sleep(1);
        return;
    }
    
    group('Criação do Lançamento Autenticado', () => {
        const url = `${BASE_URL}/api/v1/transacoes`;
        const randomId = ids[Math.floor(Math.random() * ids.length)];

        const payload = JSON.stringify({
            contaId: randomId,
            tipo: Math.random() < 0.5 ? 0 : 1,
            valor: parseFloat((Math.random() * 1000).toFixed(2)),
            dataLancamento: new Date().toISOString(),
            descricao: `Transação de teste de carga - ${uuidv4()}`
        });

        const params = {
            headers: {
                'Content-Type': 'application/json',
            },
            tags: { scenario: 'insercao_lancamentos' },
        };

        const res = http.post(url, payload, params);

        check(res, {
            'Status 200/201': (r) => r.status === 200 || r.status === 201,
            'Corpo da resposta não é vazio': (r) => r.body.length > 0,
            'Não é 401/403 (Sessão Válida)': (r) => r.status !== 401 && r.status !== 403,
        });
        
        sleep(1);
    });
}