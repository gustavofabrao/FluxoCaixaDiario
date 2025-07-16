import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { Trend } from 'k6/metrics';

const BASE_URL = 'http://localhost:4445'; 
// const BASE_URL = 'https://localhost:57939';  // No Docker

const saldoDiarioTrend = new Trend('saldo_diario_latencia_api');

// Array de datas aleatórias para consultar
const datasParaConsulta = new SharedArray('datas_para_consulta', function () {
    const data = [];
    const endDate = new Date();
    // Datas nos últimos 30 dias
    for (let i = 0; i < 30; i++) {
        const date = new Date(endDate);
        date.setDate(endDate.getDate() - i);
        data.push(date.toISOString().split('T')[0]);
    }
    return data;
});

export const options = {
    // Cenário para simular aproximadamente 50 RPS
    stages: [
        { duration: '30s', target: 20 },  // Estágio de 20 VUs em 30 segundos
        { duration: '5m', target: 50 },   // 50 VUs por 5 minutos (carga sustentada)
        { duration: '30s', target: 0 },   // Estágio de 0 VUs em 30 segundos
    ],
	// Configuração da regra do teste para 95% de disponibilidade e 5% de falha
    thresholds: {
        http_req_failed: ['rate<0.05'], // Taxa de falha deve ser menor que 5%
        http_req_duration: ['p(95)<1000'], // 95% das requisições devem ser concluídas em menos de 1 segundo
        'http_req_duration{scenario:consulta_saldo_diario}': ['p(95)<1000'], // Latência específica para este cenário
        'saldo_diario_latencia_api': ['p(95)<1000'], // Métrica
    },
    ext: {
        loadimpact: {
            scenario: 'consulta_saldo_diario',
        },
    },
};

export default function () {
    group('Consulta de Saldo Diário', () => {
        const dataAleatoria = datasParaConsulta[Math.floor(Math.random() * datasParaConsulta.length)];
        const url = `${BASE_URL}/api/saldoDiario/${dataAleatoria}`;

        const res = http.get(url);

        check(res, {
            'Status 200': (r) => r.status === 200,
            'Corpo da resposta não é vazio': (r) => r.body.length > 0,
            'Contém saldo': (r) => JSON.parse(r.body).hasOwnProperty('balance'),
        });
        
        saldoDiarioTrend.add(res.timings.duration); // Duração da requisição à métrica
        
        sleep(1); // Sleep de 1 segundo entre as iterações de cada VU
    });
}