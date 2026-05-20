# System Monitor Agent

Windows Service-приложение на C#, которое периодически собирает диагностическую информацию о Windows-машине и отправляет отчёт в HTTP API POST-запросом.

## Возможности

- Запуск как Windows Service.
- Периодический сбор данных с настраиваемым интервалом, по умолчанию 30 секунд.
- Сбор hostname, IP-адресов, версии Windows, uptime, загрузки CPU, RAM, свободного места на дисках, списка запущенных процессов и наличия заданных процессов.
- Отправка отчёта в HTTP API через POST.
- Настраиваемый endpoint API, timeout HTTP-запроса, retry-политика и список процессов для контроля.
- Логирование старта, остановки, ошибок, retry-попыток и успешной отправки отчёта в файл.
- Запись событий в Windows Event Log.
- Healthcheck endpoint.
- Graceful shutdown через стандартный механизм `BackgroundService` и `CancellationToken`.

## Требования

- Windows.
- .NET SDK 10.0 для сборки или .NET Runtime 10.0 для запуска framework-dependent публикации.
- Права администратора для установки и удаления Windows Service.

## Конфигурация

Основной конфигурационный файл: `SystemMonitorAgent.Host/appsettings.json`.

Пример:

```json
{
  "FileLogging": {
    "Path": "logs/system-monitor-agent.log"
  },
  "HealthCheck": {
    "Urls": "http://localhost:5055"
  },
  "MonitoringOptions": {
    "ApiMonitoringOptions": {
      "Endpoint": "http://localhost:5000",
      "TimeoutInSeconds": 10,
      "RetryCount": 3,
      "RetryIntervalInSeconds": 3
    },
    "MonitoringProcesses": [
      "explorer",
      "notepad"
    ],
    "MonitoringIntervalInSeconds": 30,
    "SystemType": "Windows",
    "MonitoringMode": "ApiReport"
  }
}
```

Параметры:

- `FileLogging:Path` — путь к лог-файлу. Относительный путь считается от директории приложения.
- `HealthCheck:Urls` — URL, на котором приложение поднимает healthcheck endpoint.
- `MonitoringOptions:ApiMonitoringOptions:Endpoint` — адрес HTTP API, куда отправляется отчёт.
- `MonitoringOptions:ApiMonitoringOptions:TimeoutInSeconds` — timeout HTTP-запроса в секундах.
- `MonitoringOptions:ApiMonitoringOptions:RetryCount` — количество повторных попыток при временных ошибках API.
- `MonitoringOptions:ApiMonitoringOptions:RetryIntervalInSeconds` — пауза между retry-попытками в секундах.
- `MonitoringOptions:MonitoringProcesses` — список имён процессов, наличие которых нужно проверить.
- `MonitoringOptions:MonitoringIntervalInSeconds` — период сбора данных в секундах.

## Healthcheck

Endpoint доступен по адресу из `HealthCheck:Urls`:

```powershell
Invoke-RestMethod http://localhost:5055/health
```

Пример ответа:

```json
{
  "status": "Healthy",
  "timestamp": "2026-05-20T13:00:00.0000000+00:00"
}
```

## JSON, отправляемый в API

Фактические имена полей сериализуются из модели отчёта .NET JSON-сериализатором. Пример структуры:

```json
{
  "name": "Microsoft Windows 11 Pro",
  "version": "10.0.22631",
  "buildNumber": "22631",
  "architecture": "64-bit",
  "hostName": "DESKTOP-EXAMPLE",
  "uptime": "02.04:15:10",
  "cpuLoadPercent": 18.45,
  "ram": {
    "totalGb": 31.8,
    "usedGb": 12.4,
    "freeGb": 19.4,
    "usedPercent": 38.99
  },
  "ipAddresses": [
    {
      "networkInterfaceName": "Ethernet",
      "type": "IPv4",
      "address": "192.168.1.10"
    }
  ],
  "runningProcesses": [
    {
      "id": 1234,
      "name": "explorer",
      "memoryGb": 0.12
    }
  ],
  "presenceProcesses": {
    "explorer": true,
    "notepad": false
  },
  "freeDiskSpaces": {
    "C:\\": "120.55 GB (45.5 %)"
  }
}
```

## Сборка

```powershell
dotnet restore SystemMonitorAgent.sln
dotnet build SystemMonitorAgent.sln -c Release
```

## Публикация

Framework-dependent публикация:

```powershell
dotnet publish SystemMonitorAgent.Host/SystemMonitorAgent.Host.csproj -c Release -o ./publish
```

Self-contained публикация для Windows x64:

```powershell
dotnet publish SystemMonitorAgent.Host/SystemMonitorAgent.Host.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

## Запуск из консоли

```powershell
./publish/SystemMonitorAgent.Host.exe
```

Приложение поддерживает запуск в консольном режиме и как Windows Service. При запуске из консоли логи пишутся в консоль, файл и, при доступности источника, в Windows Event Log.

## Установка Windows Service

Выполнить PowerShell от имени администратора:

```powershell
./scripts/install-service.ps1 -BinaryPath "C:\\Path\\To\\publish\\SystemMonitorAgent.Host.exe"
```

Если запуск `.ps1` запрещён Execution Policy, можно использовать CMD-обёртку из командной строки, запущенной от имени администратора:

```cmd
scripts\install-service.cmd -BinaryPath "C:\Path\To\publish\SystemMonitorAgent.Host.exe"
```

CMD-обёртка выводит результат операции и не закрывает окно, пока пользователь не нажмёт любую клавишу.
Если `-BinaryPath` не передан и `publish\SystemMonitorAgent.Host.exe` ещё не существует, установочный скрипт автоматически выполнит публикацию приложения в папку `publish`.

Проверка статуса:

```powershell
Get-Service -Name "SystemMonitorAgent"
```

## Остановка и удаление Windows Service

Выполнить PowerShell от имени администратора:

```powershell
Stop-Service -Name "SystemMonitorAgent"
sc.exe delete "SystemMonitorAgent"
```

Или через скрипт:

```powershell
./scripts/uninstall-service.ps1
```

Если запуск `.ps1` запрещён Execution Policy, можно использовать CMD-обёртку из командной строки, запущенной от имени администратора:

```cmd
scripts\uninstall-service.cmd
```

CMD-обёртка выводит результат операции и не закрывает окно, пока пользователь не нажмёт любую клавишу.

## Логи

Путь к лог-файлу задаётся параметром `FileLogging:Path` в `appsettings.json`.

В лог пишутся:

- старт worker-а;
- остановка worker-а;
- ошибки циклов мониторинга;
- retry-попытки HTTP API;
- успешная отправка диагностического отчёта.

## Тесты

Запуск юнит-тестов:

```powershell
dotnet test SystemMonitorAgent.sln
```

## Проверка работоспособности

1. Указать тестовый HTTP endpoint в `MonitoringOptions:ApiMonitoringOptions:Endpoint`.
2. Запустить приложение из консоли или как Windows Service.
3. Проверить появление POST-запросов на стороне API.
4. Проверить лог-файл по пути из `FileLogging:Path`.

## Ограничения

- Сбор системной информации реализован для Windows.
- Для CPU используется Performance Counter, а для сведений об ОС и RAM — WMI.
