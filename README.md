# Quicker Lite

Quicker Lite 是一个 Windows 桌面效率工具原型，灵感来自 Quicker。程序常驻托盘，单击鼠标中键弹出动作面板，上方是通用动作，下方根据当前软件进程名显示专属动作。

## 功能

- 中键弹出动作面板，并可按 exe 禁用中键弹窗，避免影响三维软件等需要中键操作的程序。
- 通用动作：记事本、计算器、资源管理器、项目目录、网页、PowerShell、翻译、剪贴板编辑。
- 当前软件动作：按进程名区分，例如 `explorer.exe`、`chrome.exe`、`msedge.exe`。
- 输入翻译：支持源语言/目标语言选择，调用 Google 翻译公开端点。
- 截图翻译：框选屏幕区域，使用 Windows OCR 识别后自动翻译。
- 剪贴板编辑：读取文本剪贴板，编辑后写回。
- Explorer 局域网共享：将当前资源管理器文件夹临时通过 HTTP 分享到局域网。
- 托盘菜单：重新加载配置、管理禁用列表、显示面板、退出。

## 环境要求

- Windows 10/11。
- .NET 9 SDK，用于源码构建。
- Windows OCR 组件可用，用于截图翻译。
- 网络可访问 `https://translate.googleapis.com/`，用于翻译功能。

## 从源码运行

进入项目目录：

```powershell
cd F:\code\quicker_my\QuickerLite
```

还原并运行：

```powershell
dotnet run
```

程序启动后会常驻系统托盘。单击鼠标中键即可呼出动作面板。

## 发布安装

生成可运行目录：

```powershell
cd F:\code\quicker_my\QuickerLite
dotnet publish -c Debug -o F:\code\quicker_my\QuickerLite\dist
```

发布完成后运行：

```powershell
F:\code\quicker_my\QuickerLite\dist\QuickerLite.exe
```

也可以把整个 `dist` 文件夹复制到其他电脑上运行。请保留 `dist` 目录内的所有文件，包括 `Assets`、`WinRT.Runtime.dll`、`Microsoft.Windows.SDK.NET.dll` 等依赖文件。

## 配置动作

动作配置文件是：

```text
QuickerLite\actions.json
```

发布后运行目录也会包含一份：

```text
QuickerLite\dist\actions.json
```

常用字段：

- `global`：通用栏动作。
- `apps`：按 exe 分组的当前软件动作。
- `disabledApps`：禁用中键弹窗的 exe 列表。

完整动作说明见：

```text
QuickerLite\ACTIONS.md
```

## 中键禁用与恢复

如果某个软件需要中键操作，例如 Blender、Maya、3ds Max：

1. 在该软件中单击中键打开面板。
2. 点击当前栏右侧的“禁用”。
3. 之后该软件中的中键会完全放行，不再弹出 Quicker Lite。

恢复方式：

1. 右键系统托盘中的 Quicker Lite 图标。
2. 点击“管理禁用列表”。
3. 勾选要恢复的 exe，点击“恢复选中”，或点击“全部恢复”。

## 局域网共享

在资源管理器中打开一个本地文件夹，单击中键，在当前栏点击“局域网共享”。

程序会启动临时 HTTP 文件服务，并复制访问地址，例如：

```text
http://192.168.1.20:8080/
```

其他局域网设备可用浏览器打开该地址浏览和下载文件。

右键“局域网共享”动作可：

- 启动/重新共享当前文件夹
- 停止共享
- 编辑 IP 和端口

第一次使用时，Windows 防火墙可能会询问是否允许访问网络，建议允许“专用网络”。

## 翻译

通用栏点击“翻译”打开输入翻译窗口。

- `Enter`：翻译。
- `Shift + Enter`：换行。
- 源语言默认自动识别。
- 目标语言会记住上次选择。

右键“翻译”可选择：

- 输入翻译
- 截图翻译

截图翻译会进入全屏框选模式，框选区域后使用 Windows OCR 识别文字并自动翻译。

## 剪贴板编辑

通用栏点击“剪贴板”打开编辑窗口。

- 如果剪贴板是文本，会自动填入并全选。
- 点击“确定”写回剪贴板。
- `Ctrl + Enter` 写回剪贴板。
- `Esc` 取消。
- 普通 `Enter` 用于换行。

## 设置应用图标

当前应用图标来自：

```text
QuickerLite\Assets\AppIcon.png
QuickerLite\Assets\AppIcon.ico
```

`QuickerLite.csproj` 中通过 `ApplicationIcon` 将 ico 嵌入 exe，同时窗口和托盘也使用该图标。

## Git 使用

初始化仓库并提交：

```powershell
git init
git add .
git commit -m "Initial Quicker Lite app"
```

如果已经有远程仓库：

```powershell
git remote add origin https://github.com/<your-user>/<your-repo>.git
git branch -M main
git push -u origin main
```

如果 Windows 资源管理器中 exe 图标没有立即刷新，可能是图标缓存导致。可以重启资源管理器、改文件名或重启电脑后再看。
