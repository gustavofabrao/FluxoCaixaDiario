FROM mcr.microsoft.com/dotnet/sdk:8.0 AS testrunner
WORKDIR /app

COPY ["FluxoCaixaDiario.sln", "./"]
COPY ["src/FluxoCaixaDiario.Lancamentos/FluxoCaixaDiario.Lancamentos.csproj", "src/FluxoCaixaDiario.Lancamentos/"]
COPY ["tests/FluxoCaixaDiario.Lancamentos.Tests/FluxoCaixaDiario.Lancamentos.Tests.csproj", "tests/FluxoCaixaDiario.Lancamentos.Tests/"]

RUN dotnet restore "FluxoCaixaDiario.sln"

COPY . .

WORKDIR "/app/tests/FluxoCaixaDiario.Lancamentos.Tests"

ENTRYPOINT ["dotnet", "test", "--no-build", "--logger", "trx;LogFileName=test-results.trx", "--results-directory", "/app/testresults"]