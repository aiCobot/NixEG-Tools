# EVision 授权系统逆向分析

> 分析时间：2026-11-05
> 目标：EVision 机器视觉软件（版本 10.3.2）
> 语言：C++ / Qt 6（原生 PE，非 .NET）
> 授权核心：`EVision.Quality.dll`

---

## 一、整体架构

```
┌──────────────────────┐          ┌──────────────────────┐
│    用户授权.exe        │          │    EVision.Main.exe    │
│    (授权管理客户端)     │          │    (主程序)            │
└──────────┬───────────┘          └──────────┬───────────┘
           │                                  │
           │ 隐式链接                          │ 隐式链接
           ▼                                  ▼
     ┌─────────────────────────────────────────────┐
     │         EVision.Quality.dll (授权核心)        │
     │         类: EQuality : public QObject          │
     └─────────────────────────────────────────────┘
           │
           ├── JSON 配置 → C:\Users\Public\Documents\MVS\Data\
           ├── QCryptographicHash 签名校验
           ├── QTimer 3,500,000ms (≈58分钟) 持续重检
           ├── IsDebuggerPresent 反调试
           └── CreateFileA 反时间回拨 (SetFileTime)
```

---

## 二、EVision.Quality.dll 导出 API

调用约定：`__cdecl`，类：`EQuality`（继承 QObject）

| Ord | RVA | 签名 | 输入 | 输出 | 谁在调 | 用途 |
|-----|-----|------|------|------|--------|------|
| 1 | 0x1630 | `EQuality()` | - | 实例 | Main + 授权 | 构造 |
| 2 | 0x17C0 | `~EQuality()` | - | - | Main + 授权 | 析构 |
| 4 | 0x1AF0 | `ChanageData()` (注意拼写) | - | void | 内部 | 状态变更 |
| 5 | 0x**1CC0** | `CheckQuality()` | - | **bool** | Main + 授权 | **核心授权校验入口** |
| 6 | 0x**2410** | `CheckTimer_timeout_Slot()` | - | void | 内部 QTimer | 周期性重检回调 |
| 7 | 0x**2730** | `Close_ContinuCheck()` | - | void | Main (推测) | 停止定时器 |
| 8 | 0x2740 | `GetEndDate()` | - | QString | Main | 获取授权到期日期 |
| 9 | 0x2800 | `GetFeedbackCode()` | - | QString | 授权.exe | 读取反馈码 |
| 10 | 0x28A0 | `GetMachineCode()` | - | QString | 授权.exe | 读取机器码 |
| 11 | 0x2940 | `GetRegDate()` | - | QString | Main | 获取授权开始日期 |
| 12 | 0x29E0 | `GetRegisterCode(bool?)` | - | QString | 未见外部调用 | 读已存注册码 |
| 13 | 0x41E0 | `SetRegisterCode(QString)` | **注册码字符串** | bool | 授权.exe | 设置正式注册码 |
| 14 | 0x40C0 | `SetOnTrialCode(QString)` | **试用码字符串** | bool | 授权.exe | 设置试用码 |
| 15 | 0x40B0 | `Open_ContinuCheck()` | - | void | Main | 启动 QTimer 持续校验 |

**注意**：`Is_AlrTrial` 不是导出 API，只是 DLL 内部的条件分支名字符串。

---

## 三、CheckQuality 逻辑（从汇编还原）

```
CheckQuality() RVA 0x1CC0
  │
  ├── 读注册码存储 (JSON 文件)
  │   └── C:\Users\Public\Documents\MVS\Data\*.json
  │
  ├── 机器码比对
  │   └── wmic csproduct get uuid → 自定义 Base32 编码 (16 chars, 字母表 A-Z 去掉 I/O/Q/U)
  │
  ├── QCryptographicHash 签名验证
  │   ├── 盐值: "jkvision2021" (硬编码于 .rdata)
  │   ├── 有效期模板: ":N:JKVISION" / "JKVISION:N:" (N=1/3/5/7/15/30/60/90/180/365/999)
  │   └── 算法: 标准 SHA256 (拼接顺序待确认)
  │
  ├── EndUseDate vs QDate::currentDate()
  │   ├── 7 天容差 (已过期 7 天内仍通过)
  │   └── 超 7 天 → false
  │
  ├── 反时间回拨
  │   └── CreateFileA + SetFileTime 对比文件时间戳
  │
  └── 反调试
      ├── IsDebuggerPresent → TerminateProcess
      └── QueryPerformanceCounter 检查时间差
```

---

## 四、三者关系（机器码 / 反馈码 / 注册码）

### 4.1 数据流

```
厂商那边                          用户这边
┌──────────────────────────┐      ┌─────────────────────────────┐
│ 天数: 15/30/90/...        │      │                             │
│         ↓                 │      │  用户授权.exe                 │
│ 生成反馈码 (8字母 A-Z)    │      │    ├─ GetMachineCode()        │
│         ↓                 │      │    │    └─ wmic uuid →       │
│ 机器码 + 反馈码            │      │    │      "ARPMNIZARMJHIDEB" │
│ + 天数 + 私钥 jkvision2021 │      │    │                         │
│   ↓                       │      │    └─ SetRegisterCode()       │
│ 签名算法                   │      │        └─ QCryptographicHash │
│   ↓                       │      │           本地重算 → 比对 ✅  │
│ 注册码 (32 hex, UUID样式)  │      │                             │
└─────────────┬────────────┘      └─────────────────────────────┘
              │                             │
              │ 注册码                      │ 写入 JSON → EndUseDate
              ▼                             │
        ┌─────────┐                         │
        │ 注册码   │─────────────────────────┘
        └─────────┘
```

### 4.2 样本（同一机器码）

| 机器码 | 反馈码 | 注册码 | 天数 |
|--------|--------|--------|------|
| ARPMNIZARMJHIDEB | EZAECAXS | 0C8F9CD8-9606956B-19416286-8E58AC62 | 15 |
| ARPMNIZARMJHIDEB | MWRSPNIJ | 7AD8CB37-39007F48-7AB6379A-2C3C7764 | 30 |
| ARPMNIZARMJHIDEB | NUVMNECA | 27B4CD1B-8256BE6D-193E2FB3-455522DC | 90 |

规律：
- 相同机器码 + 相同反馈码 → **相同注册码**（样本 2 和 4 完全一致）
- 注册码里 **天数都是 15 的倍数**
- 反馈码是 8 字母 A-Z，编码了天数信息

### 4.3 为什么有反馈码？

1. **双重签名**：反馈码是厂商对"天数"的签名，防止用户自己改日期
2. **授权可撤销**：厂商想改策略（延长/缩短/永久），只需换反馈码
3. **注册码 = f(机器码, 反馈码, 天数, 私钥)**，用户无法伪造

---

## 五、Main.exe 调用关系

```
EVision.Main.exe 导入的 Quality API:
  CheckQuality         → 启动门禁 + 持续校验
  OpenQualityUi        → 菜单里弹"授权管理"
  GetRegDate / GetEndDate → 界面显示有效期
  Open_ContinuCheck    → 开 58 分钟 QTimer
  Close_ContinuCheck   → (推测) 停定时器

启动流程 (0x14000160F):
  mov rcx, [rbp-0x38]
  call CheckQuality        ← 唯一分流点
  test al, al
  jne  0x1400016BA         ← true → 继续初始化
  ... 拼错误消息、返回 0    ← false → 阻断

GetEndDate 两处调用:
  0x140031365 → 日期字符串解析成 QDate
  0x140032072 → QDate 比较 + GetRegDate

持续校验:
  Open_ContinuCheck() → QTimer::start(3,500,000ms) ≈ 58 分钟
  → CheckTimer_timeout_Slot() → 再次调 CheckQuality
```

---

## 六、Patch 清单

### 6.1 已验证安全的 3 个补丁

| # | 函数 | RVA | 原始字节 | Patch 后 | 效果 |
|---|------|-----|---------|---------|------|
| ① | CheckQuality | **0x1CC0** | `48 89 5c 24 08 55` | `b0 01 c3 90 90 90` | **主门禁直接放行**（所有调用返回 true） |
| ② | Open_ContinuCheck | **0x40B0** | `48 8B 49 10 BA 80`... | `c3 90 90 90 90 90 90 90 90 90` | **58 分钟定时器不启动** |
| ③ | CheckTimer_timeout_Slot | **0x2410** | `40 55 53 56 57 48 8d 6c...` | `c3 90 90 90 90 90 90 90 90 90` | **定时器回调直接 ret**（双重保险） |

**备份文件**：`EVision.Quality.dll.bak`（每次 patch 前先从 .bak 恢复原始）

### 6.2 失败的 patch （不要用）

| # | 函数 | 方案 | 后果 | 原因 |
|---|------|------|------|------|
| ❌ | GetEndDate | 强制返回空 QString | 启动崩溃 | .rdata 标志位判断走错误分支，内部指针未初始化 |
| ❌ | GetEndDate | 跳过逻辑直接 call QString::QString("2036-12-31") | 启动崩溃 | patch 起点选错，跳过了 `[rsp+8]=rbx` 保存指令，后面 mov rbx,[rsp+0xA0] 拿到脏值 |

### 6.3 GetEndDate 补丁失败根因详解

```
原 GetEndDate 汇编 (RVA 0x2740):
  0x2740: mov qword ptr [rsp + 8], rbx    ← 保存 rbx
  0x2745: push rdi
  0x2746: sub rsp, 0x90
  0x274D: mov rdi, rdx                     ← patch 起点应该在这之后，但之前错选了 0x274D
  0x2750: lea rcx, [rsp + 0x40]
  0x2755: call 0x1800060f0
  ...
  0x27DE: mov rax, rdi
  0x27E1: mov rbx, qword ptr [rsp + 0xA0]  ← 这里用 rsp+0xA0 恢复 rbx
  0x27E9: add rsp, 0x90
  0x27F0: pop rdi
  0x27F1: ret

如果 patch 从 0x2740 开始覆盖:
  - 跳过了 [rsp+8]=rbx，rbx 没存
  - 但 prologue 没完整跑，[rsp+0xA0] 也是脏的
  - 0x27E1 把脏值塞回 rbx → ret 栈帧错乱 → 崩溃
```

**正确做法**（下次如果要搞）：prologue 让它完整跑完，从 0x2750 开始 patch，然后 jmp 到 0x27DE。

---

## 七、"-2天" 弹窗分析

### 7.1 来源

Main.exe 自己做了一套独立于 CheckQuality 的日期显示/提醒逻辑：

```
GetEndDate() → 返回 DLL 内部存的 EndUseDate（已过期）
currentDate() → QDate::currentDate()
天数差 = GetEndDate() - currentDate() = -2
→ Main 内部的定时器判断"剩余天数 < 阈值" → 弹窗
```

### 7.2 为什么 patch CheckQuality 不够

| 层级 | 补丁状态 | 效果 |
|------|---------|------|
| CheckQuality → true | ✅ 已打 | **能进主界面** |
| Open_ContinuCheck → ret | ✅ 已打 | **不会 58 分钟重检** |
| GetEndDate 返回值 | ❌ 没 patch | 仍返回过期日期 → Main 算出来还是负数 |

### 7.3 三个解决方向

| 方案 | 难度 | 风险 |
|------|------|------|
| **A. patch Main.exe 的日期差判断** | 中 | 需要 PDB 定位具体分支 |
| **B. 正确 patch GetEndDate**（prologue 后 0x2750 起 patch） | 中 | 上次失败是 patch 起点错了，可以再试 |
| **C. 生成合法注册码写入 JSON** | 高 | 需要先破解签名算法（QCryptographicHash 拼接顺序未知） |

---

## 八、机器码编码格式

```
wmic csproduct get uuid → "31444C35-4434-4539-5A4C-E070EAC65BCD"
                         (原始 UUID, 不是最终机器码)

机器码: "ARPMNIZARMJHIDEB"
  - 16 个 A-Z 字母 (无 I/O/Q/U)
  - 16 × 5 bit = 80 bit = 10 字节
  - = 多个硬件信息组合后 → 自定义 Base32 编码
```

---

## 九、文件位置速查

| 文件 | 路径 | 备注 |
|------|------|------|
| EVision.Quality.dll (已 patch) | `Xsoft1/bin/EVision.Quality.dll` | 三个补丁 |
| EVision.Quality.dll (原版备份) | `Xsoft1/bin/EVision.Quality.dll.bak` | patch 前先从这恢复 |
| EVision.Main.pdb | `Xsoft1/bin/EVision.Main.pdb` | 19MB，含完整函数名 |
| 授权存储 | `C:\Users\Public\Documents\MVS\Data\` | 首次注册后生成 |
| 日志 | `C:\Users\Public\Documents\MVS\MVS_Log\` | FeatureBag.log / FirmwareUp.log |

---

## 十、相关项目

- **REA (Reverse Engineer Anything)**：github.com/morluto/rea，30万+ stars，多平台逆向框架
- **JK License 授权码生成器**：本仓库 `JK license/` 目录
- **Trae + MCP**：把 REA 配成 Trae 的 MCP server，可以用 AI 做逆向

---

## 附录：Patch 脚本（Python）

```python
"""一键从 .bak 恢复并打三个安全补丁"""
import pefile, shutil

dll = r'EVision.Quality.dll'
bak = r'EVision.Quality.dll.bak'

# 每次都从 .bak 恢复
shutil.copyfile(bak, dll)
pe = pefile.PE(dll)

def patch(rva, data, desc):
    with open(dll, 'r+b') as f:
        f.seek(pe.get_offset_from_rva(rva))
        f.write(data)
    print(f'✓ {desc} @0x{rva:04X}: {data.hex(" ")}')

# Patch 1: CheckQuality → true
patch(0x1CC0, bytes([0xB0,0x01,0xC3]) + b'\x90'*3,
      'CheckQuality → mov al,1; ret')

# Patch 2: Open_ContinuCheck → ret
patch(0x40B0, b'\xC3' + b'\x90'*9,
      'Open_ContinuCheck → ret')

# Patch 3: CheckTimer_timeout → ret
patch(0x2410, b'\xC3' + b'\x90'*9,
      'CheckTimer_timeout_Slot → ret')

print('完成')
```

---

*本文档基于 2026-11-05 对 EVision 10.3.2 的逆向分析，仅供学习研究使用。*
