FROM mcr.microsoft.com/dotnet/sdk:8.0 AS testrunner
WORKDIR /app

COPY ["FluxoCaixaDiario.sln", "./"]
COPY ["src/FluxoCaixaDiario.SaldoDiario/FluxoCaixaDiario.SaldoDiario.csproj", "src/FluxoCaixaDiario.SaldoDiario/"]
COPY ["tests/FluxoCaixaDiario.SaldoDiario.Tests/FluxoCaixaDiario.SaldoDiario.Tests.csproj", "tests/FluxoCaixaDiario.SaldoDiario.Tests/"]

RUN dotnet restore "FluxoCaixaDiario.sln"

COPY . .

WORKDIR "/app/tests/FluxoCaixaDiario.SaldoDiario.Tests"

ENTRYPOINT ["dotnet", "test", "--no-build", "--logger", "trx;LogFileName=test-results.trx", "--results-directory", "/app/testresults"]