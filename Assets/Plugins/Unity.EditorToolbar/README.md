# Editor Toolbar

定制编辑器工具条







## 使用

1.  打开工具条设置窗口，选择菜单`Window/General/Toolbar`
2. 勾选工具条 `Enabled` 



### 默认属性

- Enabled

  是否开启该工具条，默认为false

- Position

  工具条位置，位于`Play` 按钮左边或者右边

- Order

  工具条位置排序





## 已包含的工具

### OpenFileOrFolderTool

快捷打开文件或文件夹

1. 点击按钮`◣`扩展菜单，选择菜单 `Settings` 打开设置窗口
2. 点击 `Add Folder/Folder` 添加文件夹
3. 点击工具按钮图标弹出菜单，新加的文件夹会在菜单里
4. 如果文件夹路径在`Assets`目录下，则`Ping`该目录，否则弹出文件夹窗口



#### 打开文件

1. 勾选 `File`
2. `Include File` 过滤正则表达式，`.*` 表示全部



### PlayTool

不论当前处在哪个场景，先切换到启动场景后再运行，默认启动场景为第一个



#### 扩展菜单

菜单包含 `Build Settings` 已添加的场景



#### 指定启动场景

1. 按住 `control+ ◣`
2. 选择启动场景



### OpenPreviousSceneTool

打开上一次打开的场景



### CommandLineTool

配置快捷命令

