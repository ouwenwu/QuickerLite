# Quicker Lite 动作说明

这份文档用于在其他电脑上复现当前 Quicker Lite 的动作配置。实际动作配置文件是 `actions.json`，程序启动时会把它复制到输出目录，并从输出目录读取运行时配置。

## 配置文件结构

```json
{
  "disabledApps": [],
  "global": [],
  "apps": {}
}
```

- `disabledApps`：禁用中键弹窗的软件进程名列表，例如 `["blender.exe", "maya.exe"]`。这些软件中单击中键会完全放行给原软件，不弹出 Quicker Lite。
- `global`：通用栏动作。无论当前在哪个软件，弹出的上栏都会显示这些动作。
- `apps`：当前软件栏动作。键是进程名，例如 `explorer.exe`、`chrome.exe`、`msedge.exe`；值是该软件专属动作列表。

每个动作都使用下面的字段：

```json
{
  "title": "动作名称",
  "icon": "显示图标或文字",
  "type": "动作类型",
  "target": "动作目标",
  "args": "可选参数"
}
```

支持的 `type`：

- `process`：启动程序。`target` 是 exe 名或完整路径，`args` 是可选命令行参数。
- `folder`：打开文件夹。`target` 是文件夹路径，支持环境变量如 `%USERPROFILE%`。
- `file`：打开文件。`target` 是文件路径，使用系统默认程序打开。
- `url`：打开网址。`target` 是 URL，使用系统默认浏览器打开。
- `shell`：通过 `cmd.exe /c` 执行命令。`target` 是命令主体，`args` 会拼接在命令后面。
- `translate`：Quicker Lite 内置动作。左键打开输入翻译窗口；右键显示“输入翻译”和“截图翻译”。截图翻译会框选屏幕区域，OCR 识别文字后自动送入翻译窗口。
- `clipboardEdit`：Quicker Lite 内置动作。打开剪贴板文本编辑窗口，修改后写回剪贴板。
- `lanShare`：Quicker Lite 内置动作。用于把资源管理器当前文件夹临时共享到局域网，其他设备可通过浏览器访问。

## 通用栏动作

### 记事本

```json
{
  "title": "记事本",
  "icon": "📝",
  "type": "process",
  "target": "notepad.exe"
}
```

- 作用：打开 Windows 记事本。
- 依赖：Windows 自带 `notepad.exe`。
- 迁移说明：通常不需要修改。只要目标电脑是 Windows，`notepad.exe` 一般在系统路径中。

### 计算器

```json
{
  "title": "计算器",
  "icon": "🧮",
  "type": "process",
  "target": "calc.exe"
}
```

- 作用：打开 Windows 计算器。
- 依赖：Windows 自带计算器。
- 迁移说明：通常不需要修改。如果目标电脑精简过系统组件，可能需要从 Microsoft Store 安装计算器。

### 资源管理器

```json
{
  "title": "资源管理器",
  "icon": "📂",
  "type": "process",
  "target": "explorer.exe"
}
```

- 作用：打开 Windows 文件资源管理器。
- 依赖：Windows 自带 `explorer.exe`。
- 迁移说明：通常不需要修改。

### 项目目录

```json
{
  "title": "项目目录",
  "icon": "🧰",
  "type": "folder",
  "target": "F:\\code\\quicker_my"
}
```

- 作用：打开当前 Quicker Lite 项目所在目录。
- 依赖：目标路径必须存在。
- 迁移说明：在其他电脑上需要把 `target` 改成新的项目目录，例如 `D:\\tools\\quicker_my`。如果想减少路径差异，也可以改成 `%USERPROFILE%\\Documents\\quicker_my` 这种环境变量路径。

### Quicker

```json
{
  "title": "Quicker",
  "icon": "🌐",
  "type": "url",
  "target": "https://getquicker.net/"
}
```

- 作用：用默认浏览器打开 Quicker 官网。
- 依赖：目标电脑有默认浏览器，并且能访问该网址。
- 迁移说明：通常不需要修改。

### PowerShell

```json
{
  "title": "PowerShell",
  "icon": "⌨",
  "type": "process",
  "target": "powershell.exe"
}
```

- 作用：打开 Windows PowerShell。
- 依赖：Windows 自带 `powershell.exe`。
- 迁移说明：通常不需要修改。如果想打开新版 PowerShell 7，可以把 `target` 改成 `pwsh.exe`，前提是目标电脑已安装 PowerShell 7。

### 翻译

```json
{
  "title": "翻译",
  "icon": "译",
  "type": "translate",
  "target": "input"
}
```

- 作用：打开 Quicker Lite 内置输入翻译窗口。
- 左键行为：打开输入翻译窗口。
- 右键行为：显示“输入翻译”和“截图翻译”菜单。
- 输入翻译：输入文本后按 `Enter` 翻译，`Shift+Enter` 换行。
- 截图翻译：进入全屏半透明框选模式，拖拽选择文字区域；松开鼠标后进行 Windows OCR，识别出的文字会自动填入输入框并触发翻译；按 `Esc` 取消。
- 语言选择：源语言默认“自动识别”，目标语言默认“中文”，目标语言会记住上次选择。
- 翻译接口：使用 Google 翻译公开端点，源语言和目标语言按窗口下拉框动态传入。
- 依赖：目标电脑需要能访问 `https://translate.googleapis.com/`，并且系统可用 Windows OCR。
- 迁移说明：不需要改 `target`。如果目标电脑网络无法访问 Google 翻译，该动作会显示翻译失败提示。
- 当前限制：OCR 结果以纯文本形式送入翻译窗口，不做版面还原。

### 剪贴板

```json
{
  "title": "剪贴板",
  "icon": "✎",
  "type": "clipboardEdit",
  "target": "text"
}
```

- 作用：编辑当前剪贴板中的文本内容。
- 左键行为：打开“编辑剪贴板”窗口。
- 自动填入：如果当前剪贴板是文字，会自动填入文本框并全选。
- 非文本剪贴板：如果当前剪贴板不是文字，文本框为空，并显示提示“当前剪贴板不是文字，可直接输入新内容”。
- 保存方式：
  - 点击“确定”保存到剪贴板并关闭窗口。
  - 按 `Ctrl+Enter` 保存到剪贴板并关闭窗口。
  - 按 `Esc` 取消关闭，不修改剪贴板。
- 编辑方式：普通 `Enter` 用于换行，文本框支持多行、自动换行和滚动条。
- 依赖：Windows 文本剪贴板。
- 迁移说明：不需要改 `target`。这是 Quicker Lite 内置动作，只要程序版本支持 `clipboardEdit` 即可复现。
- 当前限制：只处理纯文本，不处理图片、文件列表或富文本格式。

## 当前软件栏动作

### explorer.exe：桌面

```json
{
  "title": "桌面",
  "icon": "🖥",
  "type": "folder",
  "target": "%USERPROFILE%\\Desktop"
}
```

- 显示条件：在 `explorer.exe` 上单击中键打开面板时显示。
- 作用：打开当前用户桌面文件夹。
- 依赖：`%USERPROFILE%\\Desktop` 存在。
- 迁移说明：使用环境变量，通常不需要修改。

### explorer.exe：下载

```json
{
  "title": "下载",
  "icon": "📥",
  "type": "folder",
  "target": "%USERPROFILE%\\Downloads"
}
```

- 显示条件：在 `explorer.exe` 上单击中键打开面板时显示。
- 作用：打开当前用户下载文件夹。
- 依赖：`%USERPROFILE%\\Downloads` 存在。
- 迁移说明：使用环境变量，通常不需要修改。

### explorer.exe：用户目录

```json
{
  "title": "用户目录",
  "icon": "👤",
  "type": "folder",
  "target": "%USERPROFILE%"
}
```

- 显示条件：在 `explorer.exe` 上单击中键打开面板时显示。
- 作用：打开当前 Windows 用户目录。
- 依赖：`%USERPROFILE%` 环境变量。
- 迁移说明：使用环境变量，通常不需要修改。

### explorer.exe：命令行

```json
{
  "title": "命令行",
  "icon": "▣",
  "type": "process",
  "target": "cmd.exe"
}
```

- 显示条件：在 `explorer.exe` 上单击中键打开面板时显示。
- 作用：打开 Windows 命令提示符。
- 依赖：Windows 自带 `cmd.exe`。
- 迁移说明：通常不需要修改。如果想打开 PowerShell，可以改成 `powershell.exe`；如果想打开 Windows Terminal，可以改成 `wt.exe`。

### explorer.exe：局域网共享

```json
{
  "title": "局域网共享",
  "icon": "⇄",
  "type": "lanShare",
  "target": "currentFolder"
}
```

- 显示条件：在 `explorer.exe` 上单击中键打开面板时显示。
- 作用：把当前资源管理器窗口所在文件夹启动为临时 HTTP 文件服务。
- 左键行为：
  - 如果当前没有共享服务，会获取当前 Explorer 文件夹并启动共享。
  - 默认地址格式为 `http://局域网IP:8080/`。
  - 启动成功后会弹出共享地址，并把地址复制到剪贴板。
  - 如果已经有共享服务，再次点击会提示停止共享或重新共享当前文件夹。
- 右键行为：
  - `启动/重新共享当前文件夹`：重新获取当前 Explorer 文件夹并启动共享。
  - `停止共享`：停止当前 HTTP 文件服务。
  - `编辑 IP 和端口`：打开设置窗口，保存预设 IP 和端口。
- 设置位置：`%APPDATA%\\QuickerLite\\lan-share-settings.json`。
- 默认设置：
  - IP：自动获取本机局域网 IPv4，例如 `192.168.x.x`、`10.x.x.x`、`172.16-31.x.x`。
  - 端口：`8080`。
- 依赖：
  - 当前窗口必须是本地文件夹形式的资源管理器窗口。
  - Windows 防火墙需要允许该程序接受局域网连接。
  - 目标端口不能被其他程序占用。
- 安全限制：
  - 只支持浏览和下载文件，不支持上传、删除或修改文件。
  - 服务会限制访问路径，禁止通过 `../` 访问共享根目录之外的文件。
- 迁移说明：
  - `actions.json` 中不需要写死 IP 和端口。
  - 到新电脑后第一次使用时，可右键该动作进入“编辑 IP 和端口”。
  - 如果目标电脑局域网 IP 改变，建议把 IP 留空或重新保存当前自动识别到的 IP。
  - 如果 `8080` 被占用，可改成 `8090`、`8888` 等其他端口。

### chrome.exe：新标签页

```json
{
  "title": "新标签页",
  "icon": "＋",
  "type": "url",
  "target": "https://www.google.com/"
}
```

- 显示条件：在 `chrome.exe` 上单击中键打开面板时显示。
- 作用：用默认浏览器打开 Google 首页。
- 依赖：目标电脑能访问 Google。
- 迁移说明：如果目标电脑不能访问 Google，可以把 `target` 改成其他搜索引擎或常用网址。

### chrome.exe：Quicker

```json
{
  "title": "Quicker",
  "icon": "🌐",
  "type": "url",
  "target": "https://getquicker.net/"
}
```

- 显示条件：在 `chrome.exe` 上单击中键打开面板时显示。
- 作用：打开 Quicker 官网。
- 依赖：默认浏览器和网络连接。
- 迁移说明：通常不需要修改。

### msedge.exe：新标签页

```json
{
  "title": "新标签页",
  "icon": "＋",
  "type": "url",
  "target": "https://www.bing.com/"
}
```

- 显示条件：在 `msedge.exe` 上单击中键打开面板时显示。
- 作用：用默认浏览器打开 Bing 首页。
- 依赖：默认浏览器和网络连接。
- 迁移说明：通常不需要修改。也可以改成自己的常用网址。

### msedge.exe：Quicker

```json
{
  "title": "Quicker",
  "icon": "🌐",
  "type": "url",
  "target": "https://getquicker.net/"
}
```

- 显示条件：在 `msedge.exe` 上单击中键打开面板时显示。
- 作用：打开 Quicker 官网。
- 依赖：默认浏览器和网络连接。
- 迁移说明：通常不需要修改。

## 在其他电脑复现

1. 安装 .NET 9 Desktop Runtime，或使用已经发布好的自包含版本。
2. 复制整个 `QuickerLite` 项目或输出目录。
3. 检查 `actions.json` 中的绝对路径，尤其是 `F:\\code\\quicker_my`。
4. 根据目标电脑的软件进程名调整 `apps` 的键，例如浏览器可能是 `chrome.exe`、`msedge.exe` 或其他进程名。
5. 启动 `QuickerLite.exe`。
6. 在需要保留中键操作的软件中，打开面板后点击“禁用”，或者手动把 exe 名加入 `disabledApps`。

## 添加新动作示例

打开指定文件夹：

```json
{
  "title": "素材库",
  "icon": "📁",
  "type": "folder",
  "target": "D:\\Assets"
}
```

打开带参数的程序：

```json
{
  "title": "VS Code",
  "icon": "⌘",
  "type": "process",
  "target": "code.exe",
  "args": "D:\\Project"
}
```

执行命令：

```json
{
  "title": "刷新DNS",
  "icon": "↻",
  "type": "shell",
  "target": "ipconfig",
  "args": "/flushdns"
}
```

为 Blender 添加当前软件栏动作：

```json
"blender.exe": [
  {
    "title": "素材库",
    "icon": "📁",
    "type": "folder",
    "target": "D:\\Assets"
  }
]
```

如果 Blender 需要中键进行三维视图操作，建议不要给它配置中键面板动作，而是在面板里点击“禁用”，让 `disabledApps` 记录 `blender.exe`。
