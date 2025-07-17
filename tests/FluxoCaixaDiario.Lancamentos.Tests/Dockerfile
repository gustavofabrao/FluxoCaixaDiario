FROM mcr.microsoft.com/dotnet/sdk:8.0 AS testrunner

WORKDIR /app

RUN rm -rf ./src/*/bin/ || true && \
    rm -rf ./src/*/obj/ || true && \
    rm -rf ./tests/*/bin/ || true && \
    rm -rf ./tests/*/obj/ || true && \
    dotnet nuget locals all --clear

COPY . .

RUN dotnet restore "FluxoCaixaDiario.sln"

WORKDIR /app/tests/FluxoCaixaDiario.Lancamentos.Tests

CMD ["dotnet", "test", "--logger", "trx;LogFileName=test-results.trx;ResultsDirectory=/app/testresults"]