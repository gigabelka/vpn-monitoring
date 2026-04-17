# VPN Monitor

Приложение для Windows, которое постоянно следит за VPN-соединениями,
живёт в системном трее и показывает каскадные уведомления при
подключении/отключении.

---

## Структура проекта

```
VpnMonitoring/
├── App.xaml / App.xaml.cs          — точка входа, трей, инициализация
├── Models/
│   └── VpnEvent.cs                 — модель события (Connected / Disconnected)
├── Core/
│   ├── RasInterop.cs               — P/Invoke для Windows RAS API
│   ├── VpnMonitoringService.cs        — опрос RAS API + виртуальных адаптеров
│   └── TrayIconFactory.cs          — генерация иконки трея через GDI+
└── Notifications/
    ├── NotificationWindow.xaml     — всплывающее окно (WPF, без рамки)
    ├── NotificationWindow.xaml.cs  — логика анимации и закрытия
    └── NotificationManager.cs      — стек уведомлений, позиционирование
```

---

## Требования

|     |                              |
| --- | ---------------------------- |
| SDK | .NET 8 SDK                   |
| OS  | Windows 10 / 11 (x64)        |
| IDE | Visual Studio 2022 или Rider |

---

## Сборка и запуск

```bash
# Разработка
dotnet run

# Публикация в единый .exe без зависимостей
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Итоговый файл: `bin\Release\net8.0-windows\win-x64\publish\VpnMonitoring.exe`

---

## Автозапуск с Windows

Добавьте запись в реестр (запустите один раз от имени пользователя):

```csharp
// Вставьте в App.OnStartup() после первого запуска
var key = Registry.CurrentUser.OpenSubKey(
    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true)!;
key.SetValue("VpnMonitoring", Environment.ProcessPath!);
```

Или просто создайте ярлык в `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup`.

---

## Как это работает

### Мониторинг VPN

`VpnMonitoringService` опрашивает два источника каждые 2 секунды:

1. **RAS API** (`rasapi32.dll` → `RasEnumConnections`)  
   Определяет встроенные Windows VPN: PPTP, L2TP/IPsec, SSTP, IKEv2.

2. **Виртуальные сетевые адаптеры** (`NetworkInterface.GetAllNetworkInterfaces`)  
   Определяет WireGuard, OpenVPN, TAP-адаптеры по ключевым словам в имени/описании.

При обнаружении изменения вызывается событие `VpnEventOccurred`.

### Каскадные уведомления

`NotificationManager` ведёт список открытых окон.  
Каждое новое окно появляется снизу-справа (слот 0, у панели задач).  
Предыдущие окна сдвигаются вверх с плавной анимацией.  
При закрытии окна остальные опускаются вниз.

---

## Кастомизация

| Что                                  | Где                                                              |
| ------------------------------------ | ---------------------------------------------------------------- |
| Интервал опроса                      | `new VpnMonitoringService(pollIntervalMs: 2000)` в `App.xaml.cs` |
| Ключевые слова виртуальных адаптеров | `VirtualVpnKeywords` в `VpnMonitoringService.cs`                 |
| Размер / цвет уведомления            | `NotificationWindow.xaml`                                        |
| Отступы / высота уведомления         | `WindowHeight`, `ScreenMargin` в `NotificationManager.cs`        |
| Позиция стека                        | `ComputeTop()` в `NotificationManager.cs`                        |

---

## Следующие шаги (за рамками скелета)

- [ ] Логирование событий в файл (`Serilog` / `Microsoft.Extensions.Logging`)
- [ ] История событий — окно-журнал по двойному клику на трей
- [ ] Настройки (список игнорируемых соединений, интервал опроса)
- [ ] Поддержка нескольких мониторов (определять рабочую область активного экрана)
- [ ] Автозапуск с выбором через UI
- [ ] Installer (`WiX` или `Inno Setup`)
