# VSAutoLoader
Visual Studio 自动刷新工程下引用文件，默认引用当前目录下所有工程目录下的CS文件，默认去掉目录bin、obj

1. `.loader` 配置
   * `ExcludeFolder` : 不包含刷新的目录
   * `ExcludeFile` : 不包含刷新的文件
   * `IncludeFile` : 特殊包含的文件

    ```
    若都为空则包含所有文件
    ```