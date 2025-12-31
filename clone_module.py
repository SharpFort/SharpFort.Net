import os
import shutil

# 配置
source_dir = "module/rbac"
target_dir = "module/casbin-rbac"
old_str = "Yi.Framework.Rbac"
new_str = "Yi.Framework.CasbinRbac"
old_short = "Rbac"
new_short = "CasbinRbac"

def clone_and_rename():
    # 1. 检查并清理目标目录
    if os.path.exists(target_dir):
        print(f"Cleaning existing target directory: {target_dir}")
        shutil.rmtree(target_dir)
    
    # 2. 复制目录
    print(f"Copying {source_dir} to {target_dir}...")
    shutil.copytree(source_dir, target_dir)

    # 3. 遍历并处理文件内容 (先处理内容，避免文件被重命名后找不到)
    print("Replacing content in files...")
    for root, dirs, files in os.walk(target_dir):
        for file in files:
            # 跳过二进制文件和非源码文件
            if not file.endswith(('.cs', '.csproj', '.json', '.xml', '.sln', '.md', '.conf')):
                continue
                
            file_path = os.path.join(root, file)
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # 替换命名空间和引用
                new_content = content.replace(old_str, new_str)
                # 替换简写 (如 YiFrameworkRbacModule -> YiFrameworkCasbinRbacModule)
                new_content = new_content.replace(f"YiFramework{old_short}", f"YiFramework{new_short}")
                # 替换 YiRbacDbContext -> YiCasbinRbacDbContext
                new_content = new_content.replace(f"Yi{old_short}DbContext", f"Yi{new_short}DbContext")

                
                if new_content != content:
                    with open(file_path, 'w', encoding='utf-8') as f:
                        f.write(new_content)
            except Exception as e:
                print(f"Skipping file {file_path}: {e}")

    # 4. 重命名文件和文件夹
    # 注意：os.walk 是自顶向下的，重命名文件夹会导致遍历中断或路径失效
    # 所以我们需要先收集所有需要重命名的路径，然后按深度排序（先改子文件/文件夹，再改父文件夹）
    print("Renaming files and directories...")
    
    paths_to_rename = []
    for root, dirs, files in os.walk(target_dir):
        for file in files:
            if old_str in file or old_short in file:
                paths_to_rename.append(os.path.join(root, file))
        for dir in dirs:
            if old_str in dir or old_short in dir:
                paths_to_rename.append(os.path.join(root, dir))
    
    # 按路径长度降序排序，确保先处理深层路径
    paths_to_rename.sort(key=lambda x: len(x), reverse=True)
    
    for path in paths_to_rename:
        dir_name, file_name = os.path.split(path)
        # 优先替换长名称，防止短名称误伤
        new_file_name = file_name.replace(old_str, new_str)
        # 如果长名称没变，尝试替换短名称 (如 YiFrameworkRbacModule.cs)
        if new_file_name == file_name:
             new_file_name = new_file_name.replace(f"YiFramework{old_short}", f"YiFramework{new_short}")
        
        # 如果只是单纯包含 Rbac (比如 CASBIN-RBAC 这种不需要改的)，需要小心
        # 这里假设我们的命名规范比较好，不做过度匹配
        
        if new_file_name != file_name:
            new_path = os.path.join(dir_name, new_file_name)
            os.rename(path, new_path)
            print(f"Renamed: {path} -> {new_path}")

    print("Clone and rename completed successfully.")

if __name__ == "__main__":
    clone_and_rename()
