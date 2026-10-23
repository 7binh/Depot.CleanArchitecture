@echo off
setlocal enabledelayedexpansion

:: 1. Nhập tên Solution / Prefix dự án
set /p SOLUTION_NAME="Nhap ten Solution (vi du: TechSpherex.CleanArchitecture hoac DepotManagement): "
if "%SOLUTION_NAME%"=="" (
    echo Khong duoc de trong ten solution!
    pause
    exit /b
)

echo.
echo ==============================================
echo Dang khoi tao Solution va cac Project theo Clean Architecture...
echo ==============================================

:: 2. Tao file Solution (.sln)
dotnet new sln -n %SOLUTION_NAME%

:: 3. Tao 4 Project chinh trong thu muc src/
echo [1/4] Tao Project Domain...
dotnet new classlib -o src/%SOLUTION_NAME%.Domain

echo [2/4] Tao Project Application...
dotnet new classlib -o src/%SOLUTION_NAME%.Application

echo [3/4] Tao Project Infrastructure...
dotnet new classlib -o src/%SOLUTION_NAME%.Infrastructure

echo [4/4] Tao Project Api...
dotnet new webapi -o src/%SOLUTION_NAME%.Api

:: 4. Add tat ca Project vao Solution
dotnet sln add src/%SOLUTION_NAME%.Domain/%SOLUTION_NAME%.Domain.csproj
dotnet sln add src/%SOLUTION_NAME%.Application/%SOLUTION_NAME%.Application.csproj
dotnet sln add src/%SOLUTION_NAME%.Infrastructure/%SOLUTION_NAME%.Infrastructure.csproj
dotnet sln add src/%SOLUTION_NAME%.Api/%SOLUTION_NAME%.Api.csproj

echo.
echo ==============================================
echo Dang thiet lap References (Quy tac phu thuoc)...
echo ==============================================

:: Application -> Domain
dotnet add src/%SOLUTION_NAME%.Application/%SOLUTION_NAME%.Application.csproj reference src/%SOLUTION_NAME%.Domain/%SOLUTION_NAME%.Domain.csproj

:: Infrastructure -> Application, Domain
dotnet add src/%SOLUTION_NAME%.Infrastructure/%SOLUTION_NAME%.Infrastructure.csproj reference src/%SOLUTION_NAME%.Application/%SOLUTION_NAME%.Application.csproj
dotnet add src/%SOLUTION_NAME%.Infrastructure/%SOLUTION_NAME%.Infrastructure.csproj reference src/%SOLUTION_NAME%.Domain/%SOLUTION_NAME%.Domain.csproj

:: Api -> Application, Infrastructure
dotnet add src/%SOLUTION_NAME%.Api/%SOLUTION_NAME%.Api.csproj reference src/%SOLUTION_NAME%.Application/%SOLUTION_NAME%.Application.csproj
dotnet add src/%SOLUTION_NAME%.Api/%SOLUTION_NAME%.Api.csproj reference src/%SOLUTION_NAME%.Infrastructure/%SOLUTION_NAME%.Infrastructure.csproj

echo.
echo ==============================================
echo Dang tao cay thu muc con theo template cua mentor...
echo ==============================================

:: Xoa cac file Class1.cs mac dinh
del src\%SOLUTION_NAME%.Domain\Class1.cs 2>nul
del src\%SOLUTION_NAME%.Application\Class1.cs 2>nul
del src\%SOLUTION_NAME%.Infrastructure\Class1.cs 2>nul

:: Thu muc cho Domain
mkdir src\%SOLUTION_NAME%.Domain\Common
mkdir src\%SOLUTION_NAME%.Domain\Entities
type nul > src\%SOLUTION_NAME%.Domain\Common\.gitkeep
type nul > src\%SOLUTION_NAME%.Domain\Entities\.gitkeep

:: Thu muc cho Application
mkdir src\%SOLUTION_NAME%.Application\Abstractions
mkdir src\%SOLUTION_NAME%.Application\Features
type nul > src\%SOLUTION_NAME%.Application\Abstractions\.gitkeep
type nul > src\%SOLUTION_NAME%.Application\Features\.gitkeep

:: Thu muc cho Infrastructure
mkdir src\%SOLUTION_NAME%.Infrastructure\Persistence
mkdir src\%SOLUTION_NAME%.Infrastructure\Caching
mkdir src\%SOLUTION_NAME%.Infrastructure\Identity
mkdir src\%SOLUTION_NAME%.Infrastructure\Rules
mkdir src\%SOLUTION_NAME%.Infrastructure\Tenancy
type nul > src\%SOLUTION_NAME%.Infrastructure\Persistence\.gitkeep
type nul > src\%SOLUTION_NAME%.Infrastructure\Caching\.gitkeep
type nul > src\%SOLUTION_NAME%.Infrastructure\Identity\.gitkeep
type nul > src\%SOLUTION_NAME%.Infrastructure\Rules\.gitkeep
type nul > src\%SOLUTION_NAME%.Infrastructure\Tenancy\.gitkeep

:: Thu muc cho Api
mkdir src\%SOLUTION_NAME%.Api\Endpoints
mkdir src\%SOLUTION_NAME%.Api\Extensions
mkdir src\%SOLUTION_NAME%.Api\GrpcServices
mkdir src\%SOLUTION_NAME%.Api\Protos
type nul > src\%SOLUTION_NAME%.Api\Endpoints\.gitkeep
type nul > src\%SOLUTION_NAME%.Api\Extensions\.gitkeep
type nul > src\%SOLUTION_NAME%.Api\GrpcServices\.gitkeep
type nul > src\%SOLUTION_NAME%.Api\Protos\.gitkeep

:: Thu muc tests ngoai src
mkdir tests
type nul > tests\.gitkeep

echo.
echo ==============================================
echo HOAN TAT! Du an da duoc khoi tao thanh cong.
echo Mo file %SOLUTION_NAME%.sln de bat dau code.
echo ==============================================
pause
