# Smart Inventory Arrange / 智能排列

- Mod ID：`smart_inventory_arrange`
- 路线：正式版 Mono + 官方 `IMod` + 小范围 Harmony 菜单补丁
- 目标游戏版本：`0.46`
- 算法来源：[xiepeixuan/SmartInventoryArrange](https://github.com/xiepeixuan/SmartInventoryArrange) v3.17.10，MIT；许可证保留在 `UPSTREAM-LICENSE.txt`

## 功能

在容器右键的原生整理菜单中增加：

1. **智能排列**：同类物品集中、容器/机器及大件优先、同类方向尽量统一，空间不足时尝试合法旋转。
2. **智能排列（不动固定物品）**：游戏内用图钉固定的物品（`ItemPinHelper` / `PINNED_TAG`）保持原坐标和朝向，其余物品绕开它们整理；不再把内部储物窗口容器或 `powerblock` 当作固定对象。

可移动小件不会再被塞进固定物品之间的行/列夹缝，而是优先与其它物品贴边、紧凑排列。

配置项 `keep_fixed_items` 默认开启，只控制第二种模式的额外固定规则，不影响原生锁定规则。

## 安全边界

- 不删除、复制或重新生成物品，不改数量、品质、耐久、鉴定标记或容器内容物。
- 使用原版 `InventorySortHelper.MaySort/CanSort`、`GameItem.MayRemove`、`CheckShapePlacement` 和 `CheckInventorySlotStillValid`。
- 后台只计算托管布局；提交前在主线程复核物品实例、父容器、状态、坐标、方向、占格和容器几何。
- 只有全部物品都有合法目标位置才应用；应用失败恢复完整快照。
- 高密度或不规则库存不保证得到更美观的新布局；没有更好完整方案时保留原布局。
- 诊断写入官方 Mod 数据目录下的 `Logs/`，使用 `ModHelper`，不声明额外文件能力。
- 需要读取游戏私有的菜单字段，因此声明了 `reflection-external` 能力，首次启用时游戏会请你确认。

## 构建与校验

从工作区根目录执行：

```powershell
python tools/psmod.py validate Modding/SmartInventoryArrange
dotnet build Modding/SmartInventoryArrange/SmartInventoryArrange.csproj -c Release
python tools/psmod.py validate Modding/SmartInventoryArrange/bin/Release/netstandard2.1 --package --route official
```

构建只写入本工程的 `bin/`，不会自动安装。安装时把输出中的 manifest、DLL、上游许可证和资源复制到正式版游戏 `Mods/smart_inventory_arrange/`。

实现说明与实机验收清单见 [项目档案](../../docs/projects/smart-inventory-arrange.md)。
