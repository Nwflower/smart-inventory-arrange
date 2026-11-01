# Smart Inventory Arrange / 智能排列

《深空当铺 / Probably Stolen》模组：在容器右键的原生「整理」菜单里多出两种智能排列，让同类物品集中、方向统一、旋转合法。

基于 [xiepeixuan/SmartInventoryArrange](https://github.com/xiepeixuan/SmartInventoryArrange) v3.17.10 的 MIT 算法移植，适配游戏正式版的官方 Mod API。需要游戏正式版（运行时版本 0.46D）。

[English](#english)

## 功能

**智能排列**
同类物品集中，容器、机器和大件优先，同类方向尽量统一。空间不足时尝试合法旋转，并让可移动小件与其它物品贴边紧凑排列，而不是塞进固定物品之间的行/列夹缝。

**智能排列（不动固定物品）**
游戏内用图钉固定的物品（`ItemPinHelper` / `PINNED_TAG`）保持原坐标与朝向，其余物品绕开它们整理。内部储物窗口容器和 `powerblock` 不算固定对象。

配置项 `keep_fixed_items` 默认开启，只控制第二种模式的额外固定规则，不影响原生锁定规则。

## 安全边界

- **不碰物品本身**：不删除、复制或重新生成物品，不改数量、品质、耐久、鉴定标记和容器内容物。
- **只在全部合法时应用**：后台只计算托管布局，提交前在主线程复核物品实例、父容器、状态、坐标、方向、占格和容器几何。只有全部物品都有合法目标位置才应用，应用失败恢复完整快照。
- **宁可不整理**：高密度或不规则的库存不保证得到更好看的布局；没有完整的更好方案时保留原布局。
- **沿用原版判定**：`InventorySortHelper.MaySort/CanSort`、`GameItem.MayRemove`、`CheckShapePlacement`、`CheckInventorySlotStillValid`。
- **需要一项权限**：要读游戏私有的菜单字段，因此声明 `reflection-external` 能力，首次启用时游戏会请你确认。诊断只写官方 Mod 数据目录下的 `Logs/`，不声明文件能力。

## 安装

自己构建后，把产物（`manifest.xml`、DLL、`UPSTREAM-LICENSE.txt`、`Books`/`Localization`/`LootTables` 占位目录）复制到正式版游戏的 `Mods/smart_inventory_arrange/`，在游戏「模组」菜单里启用并重启。

遇到问题时按 `F8` 打开控制台，输入 `mod-scan smart_inventory_arrange` 查看加载状态。

## 语言

目前界面菜单项与配置文案只有中文，暂未提供其它语言的本地化表。

## 来源与许可

算法与搜索策略移植自 [xiepeixuan/SmartInventoryArrange](https://github.com/xiepeixuan/SmartInventoryArrange) v3.17.10（MIT），那部分版权归原作者，许可原文保留在 [UPSTREAM-LICENSE.txt](UPSTREAM-LICENSE.txt)。与官方 Mod API 的对接、菜单补丁和本仓库其余代码由本项目完成。

本项目以 [Functional Source License 1.1（MIT Future License）](LICENSE) 发布：可以自由使用、修改、分发，也可以拿去做自己的东西，但**不能把它做成与原版功能相同或近似的商业产品或服务出售**。每个版本发布满两年后，自动转为 MIT 许可。游戏本体、Unity 与官方 SDK 不在本仓库授权范围内。

## 构建与校验

```powershell
dotnet build SmartInventoryArrange.csproj -c Release
```

构建只写入本工程的 `bin/`，不会自动安装。

---

## English

A *Probably Stolen* mod that adds two smart-arrange entries to the vanilla sort menu on containers: pack similar items together, unify orientation, and rotate items only where the shape rules allow it.

Ported from the MIT-licensed algorithm of [xiepeixuan/SmartInventoryArrange](https://github.com/xiepeixuan/SmartInventoryArrange) v3.17.10 and adapted to the game's official mod API. Requires the full release of the game (runtime version 0.46D).

**Features**

- **Smart arrange** — similar items together, machines and large containers first, matching orientations where possible, legal rotations when space is tight, with small items tucked against their neighbours instead of into the gaps between pinned items.
- **Smart arrange (keep pinned items)** — items pinned with the in-game pin (`ItemPinHelper` / `PINNED_TAG`) keep their exact position and rotation; everything else arranges around them. `keep_fixed_items` (on by default) controls only this extra rule.

**Safety**

Nothing is deleted, duplicated or regenerated: counts, quality, durability, appraisal flags and container contents stay untouched. The layout is computed off-thread and re-validated on the main thread before anything is applied, and a complete snapshot is restored if applying fails. Only an all-legal layout is used; otherwise the original layout stays.

**Permission** — it reads private menu fields, so it declares the `reflection-external` capability and the game asks for confirmation on first enable. Diagnostics go to `Logs/` inside the mod's data folder via the official API.

**Install** — build it yourself, copy the output into `Mods/smart_inventory_arrange/`, enable it in the Mods menu and restart. `F8` → `mod-scan smart_inventory_arrange` shows the load state.

**Language** — the menu entries and settings text are Chinese only for now.

**Credits** — algorithm ported under MIT from [xiepeixuan/SmartInventoryArrange](https://github.com/xiepeixuan/SmartInventoryArrange); the original license text ships as [UPSTREAM-LICENSE.txt](UPSTREAM-LICENSE.txt).

**License** — [Functional Source License 1.1 (MIT Future License)](LICENSE): use, modify and redistribute freely, but do not sell it as a commercial product or service that substitutes for it. Each release becomes MIT-licensed two years after it ships.
