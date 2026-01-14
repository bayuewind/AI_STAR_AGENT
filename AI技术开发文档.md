# 星露谷 AI 智能体开发文档（Decision AI + 执行器框架 v1）

> 目标：构建一个“像人一样游玩星露谷”的智能体系统。
> 本文仅覆盖 **决策 AI 框架（LLM）+ 执行器结构（Executor）**。
> **具体工具实现（寻路/交互/购买等）由你后续接入。**

---

## 1. 总体目标与边界

### 1.1 系统目标

* 输入自然语言目标（如“去皮埃尔买 10 个防风草种子并回农场”）
* AI 能生成可执行计划，并根据执行结果**分支、重试、等待、重规划**
* 执行器能在游戏 tick 中稳定运行，避免卡死/无限重试
* 可扩展到多智能体（AI 小镇）

### 1.2 系统边界（强约束）

* LLM 不直接控制“每一步走哪一格”、不直接操作 UI 细节
* LLM **只输出严格 JSON**（可被执行器解析）
* 执行器负责：

  * 调度工具调用
  * 管理计划运行状态
  * 收集工具结果并驱动转移
  * 触发重规划（replan）与预算限制（budget）

---

## 2. 系统分层架构

系统由三层闭环构成：

1. **Perception 感知层**（你实现）
2. **Decision AI 决策层（LLM）**（本文定义协议/输入输出）
3. **Executor 执行层**（本文定义结构/状态机/调度规则）

```
Game -> Perception Snapshot -> LLM Decision(JSON) -> Executor -> Tool Calls -> Game
                                          ^                               |
                                          |----------- Tool Results ------|
```

---

## 3. 核心数据流

### 3.1 每轮决策/执行的标准流程

1. 执行器从游戏获取 `PerceptionSnapshot`
2. 执行器判断：

   * 是否已有 `plan_graph`
   * 是否需要 replan（失败/超时/环境变化/卡住）
3. 若需要规划：执行器构造 `LLMInputEnvelope` 调用 LLM
4. LLM 返回 `DecisionJSON`（包含 plan_graph）
5. 执行器进入 Running：

   * 取当前 node
   * tool_call → dispatcher Begin/Poll
   * 得到 ToolResult → 匹配 transition → 跳转到下一个 node
6. 若走到 `llm_replan` 节点或达到阈值：回到 3

---

## 4. 决策协议（Decision JSON Protocol v1）

### 4.1 设计原则

* **图执行（Plan Graph）**优于纯列表：支持分支/重试/降级策略

* 计划必须包含：

  * `start_node_id`
  * `nodes[]`（每个节点包含 tool、args、timeout、transitions）
  * 若失败：必须能跳转到 recover/replan

### 4.2 LLM 输出：Decision JSON（规范）

> LLM 每次仅输出一个 JSON 对象，字段稳定可解析。

#### 顶层结构（必须字段）

* `protocol_version`: `"agent-json-v1"`
* `tick_id`: int（回显输入 tick_id）
* `agent_id`: string
* `mode`: `"observe" | "plan" | "act" | "recover"`
* `goal`: {id, text, priority, deadline_game_time?}
* `plan_graph`: {plan_id, start_node_id, nodes[]}
* `constraints`: {max_replans, max_total_ticks, max_gold_spend}

#### Node 类型（必须支持）

* `tool_call`：调用工具
* `recover`：恢复策略节点（仍可用 tool_call 表达）
* `llm_replan`：要求执行器重新调用 LLM
* `finish_goal`：完成目标

#### Transition 匹配表达式（建议）

* `"ok"`：tool_result.ok == true
* `"timeout"`：节点超时
* `"err:*"`：任意错误
* `"err:code1|err:code2"`：错误码在集合内
* `"ok|err:*|timeout"`：逻辑或（用于 CloseMenu 这类无关紧要步骤）

---

## 5. LLM 输入包（LLMInputEnvelope 规范）

执行器喂给 LLM 的输入建议统一为：

### 5.1 输入字段

* `tick_id`
* `agent_id`
* `goal_stack[]`：目标栈（可扩展多目标）
* `world`：当前感知快照（时间/地点/背包/金币/NPC/天气/菜单态等）
* `executor_state`：

  * `current_plan_id`
  * `current_node_id`
  * `last_tool_results`（最近 1~3 个）
  * `stuck_counter` / `replan_count`
* `memory`（可选，但建议）：

  * `facts[]`（检索出的相关事实）
  * `skills[]`（工具能力描述 schema）

### 5.2 最重要的输入纪律

* world 必须结构化，尽量不要塞自然语言
* last_tool_results 必须包含 `error.code` 与关键 telemetry，否则 LLM 无法稳定恢复

---

## 6. 执行器结构（Executor Framework）

### 6.1 模块划分

#### (A) AgentRuntime（每个 AI 一个实例）

职责：

* 保存 goal_stack / 当前 plan_graph / 当前 node / 计数器
* 提供 `Update(snapshot)`，由 SMAPI Tick 驱动

#### (B) PlannerClient（LLM 调用器）

职责：

* 组装输入（PromptBuilder）
* 调用 LLM（HTTP/本地推理都行）
* 解析输出为 PlanGraph（PlanParser）
* 必须：异常保护（JSON 修复/重试/超时）

#### (C) PlanGraphRunner（图执行器）

职责：

* 执行 node
* 处理 Begin/Poll 工具调用
* 超时检测
* transition 匹配
* 进入 replan / done

#### (D) ToolDispatcher（工具调度器接口）

职责：

* 定义 Begin / Poll / Cancel
* 输出 ToolResult（结构化）

> 你会替换成真实 SMAPI 工具实现，这里只定义接口与结果格式。

#### (E) BudgetManager + StuckDetector（安全与鲁棒性）

职责：

* 限制最大重规划次数、最大总 ticks、最大金币花费
* 检测卡住（位置不变/重复错误码/warp_fail 连续出现）

---

## 7. 执行器状态机（必须实现）

### 7.1 状态定义

* `Idle`：无目标
* `NeedPlan`：有目标但无计划
* `Running`：按计划执行
* `WaitingTool`：工具执行中（NavigateTo 常用）
* `NeedReplan`：进入重规划
* `Done`：目标完成

### 7.2 转移条件（建议）

* Idle → NeedPlan：goal_stack 非空
* NeedPlan → Running：成功获取 plan_graph
* Running → WaitingTool：Begin tool_call 后等待结果
* WaitingTool → Running：获得 tool_result 并跳转节点
* Running/WaitingTool → NeedReplan：

  * 命中 `llm_replan` 节点
  * Budget/Stuck 触发
  * transition 无匹配
* Running → Done：命中 `finish_goal`

---

## 8. ToolResult 返回格式（执行器与工具的契约）

### 8.1 ToolResult（强制结构）

* `action_node_id`
* `tool`
* `ok`：bool
* `error`: { `code`, `detail` }（ok=false 时必须提供）
* `telemetry`: { location, tile, time, … }（尽可能提供）

示例：

```json
{
  "action_node_id": "a_buy",
  "tool": "ShopBuy",
  "ok": false,
  "error": { "code": "menu_not_open", "detail": "no active shop menu" },
  "telemetry": { "location": "SeedShop", "tile": { "x": 5, "y": 17 }, "time": "Spring 5 10:58" }
}
```

### 8.2 错误码（建议枚举）

导航相关：

* `unreachable`
* `warp_failed`
* `blocked_dynamic`
* `stuck`

交互/商店相关：

* `closed`
* `npc_missing`
* `menu_not_open`
* `item_not_found`
* `insufficient_gold`

通用：

* `timeout`
* `internal_error`

---

## 9. TransitionMatcher 规则（执行器关键组件）

### 9.1 匹配优先级

建议执行器按 transitions 列表顺序匹配，第一个命中即跳转。

### 9.2 表达式支持

* `ok`
* `timeout`
* `err:*`
* `err:xxx|err:yyy`
* `ok|timeout|err:*`（逻辑或）

### 9.3 无匹配处理

* 若 transitions 无一命中：强制进入 `NeedReplan`（防止卡死）

---

## 10. BudgetManager 与 StuckDetector（必备）

### 10.1 BudgetManager（建议阈值）

* `max_replans`: 3
* `max_total_ticks`: 4000（视你的 tick 频率调）
* `max_gold_spend`: 每个 goal 限额（例如 400g）
  触发即进入 `NeedReplan` 或 `AbortGoal`

### 10.2 StuckDetector（建议策略）

触发条件可组合：

* 玩家 tile 连续 N ticks 不变且当前工具为 NavigateTo
* 连续 K 次出现相同 error.code（如 warp_failed）
* 同一个 node_id 被重复执行超过 M 次

触发动作：

* 跳转到 recover 节点（如果 plan_graph 提供）
* 否则进入 `NeedReplan`

---

## 11. “去买种子”作为标准范例（系统行为）

目标：**去皮埃尔买 10 个防风草种子并回农场**

典型 plan_graph（摘要）：

* `a_nav_shop` → ok → `a_open_shop`
  `a_nav_shop` → err/timeout → `a_nav_recover`
* `a_open_shop` → ok → `a_buy`
  `a_open_shop` → closed/npc_missing → `a_wait_open`
* `a_buy` → ok → `a_close_menu`
  `a_buy` → menu_not_open → `a_open_shop`
  `a_buy` → insufficient_gold → `replan`
* `a_nav_farm` → ok → `done`
  失败则回 recover 或 replan

执行器按节点跑，工具结果回填后自动走转移，保证全流程鲁棒。

---

## 12. 工程落地建议（目录结构）

建议按以下目录组织（C#/.NET）：

```
Agent/
  AgentRuntime
  GoalModels
  PlanGraphModels
  DecisionProtocol

AI/
  PlannerClient
  PromptBuilder
  PlanParser

Executor/
  PlanGraphRunner
  TransitionMatcher
  BudgetManager
  StuckDetector

Tools/
  IToolDispatcher
  ToolDispatcherStub
  ToolResult

Memory/
  MemoryStore
  MemoryRetrieval (optional)
```

---

## 13. 最小可运行（MVP）里程碑

### 阶段 1：框架跑通（无需真实工具）

* ToolDispatcher 用 Stub 随机返回 ok/err
* 验证：

  * LLM 输出 JSON 是否稳定
  * transition 是否正确跳转
  * replan/budget/stuck 是否能触发

### 阶段 2：接入真实工具（你来实现）

* 逐个替换工具：

  * NavigateTo
  * Interact
  * ShopBuy
  * WaitUntil
* 重点观察：warp_failed / menu_not_open 的恢复链路是否稳定

### 阶段 3：扩展多目标与作息（AI 小镇前置）

* 引入 goal_stack（多目标权衡）
* Memory facts（营业时间、喜好、路线偏好）
* 多 AgentRuntime（每个 NPC/玩家一个）

---

## 14. 测试与调试建议

### 14.1 日志（强烈建议）

* 每次 node 执行：记录 `plan_id/node_id/tool/args`
* 每次结果：记录 `ok/error.code/telemetry`
* 每次 replan：记录原因（budget/stuck/no_transition）

### 14.2 可视化（推荐）

* 在 HUD 上显示：

  * 当前 goal
  * 当前 node_id
  * 最近错误码
  * stuck_counter / replan_count

---

## 15. 附：你需要对接的接口清单（你实现工具时用）

执行器对工具的最小接口期望：

* Begin(tool, args) → returns handle/task_id
* Poll(handle) → returns ToolResult | not_ready
* Cancel(handle) → best effort

工具必须能返回结构化错误码，且尽量携带 telemetry。

---

