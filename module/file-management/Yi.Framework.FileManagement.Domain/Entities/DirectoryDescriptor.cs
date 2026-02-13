using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Yi.Framework.FileManagement.Domain.Shared.Consts;

namespace Yi.Framework.FileManagement.Domain.Entities
{
    /// <summary>
    /// 目录描述符聚合根
    /// 管理虚拟文件夹的层级结构
    /// </summary>
    [SugarTable(FileManagementConsts.DbTablePrefix + "directory_descriptor")]
    [SugarIndex($"index_ParentId", nameof(ParentId), OrderByType.Asc)]
    [SugarIndex($"index_Name", nameof(Name), OrderByType.Asc)]
    public class DirectoryDescriptor : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        #region 构造函数

        public DirectoryDescriptor() { }

        /// <summary>
        /// 创建目录描述符
        /// </summary>
        /// <param name="id">目录ID (UUID7)</param>
        /// <param name="name">目录名称</param>
        /// <param name="parentId">父级目录ID (null 为根目录)</param>
        public DirectoryDescriptor(Guid id, string name, Guid? parentId = null)
            : base(id)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(name, nameof(name));

            Name = name;
            ParentId = parentId;
        }

        #endregion

        #region 核心属性

        /// <summary>
        /// 主键 (UUID7)
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public override Guid Id { get; protected set; }

        /// <summary>
        /// 租户ID
        /// </summary>
        public Guid? TenantId { get; protected set; }

        /// <summary>
        /// 目录名称
        /// </summary>
        [SugarColumn(Length = FileManagementConsts.MaxDirectoryNameLength)]
        public string Name { get; protected set; }

        /// <summary>
        /// 父级目录ID (null 表示根目录)
        /// </summary>
        public Guid? ParentId { get; protected set; }

        #endregion

        #region 导航属性

        /// <summary>
        /// 父级目录
        /// </summary>
        [Navigate(NavigateType.OneToOne, nameof(ParentId))]
        public DirectoryDescriptor? Parent { get; set; }

        /// <summary>
        /// 子目录集合
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(ParentId))]
        public List<DirectoryDescriptor> Children { get; set; } = new();

        /// <summary>
        /// 目录下的文件集合
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(FileDescriptor.DirectoryId))]
        public List<FileDescriptor> Files { get; set; } = new();

        #endregion

        #region 业务方法

        /// <summary>
        /// 重命名目录
        /// </summary>
        public void Rename(string newName)
        {
            Volo.Abp.Check.NotNullOrWhiteSpace(newName, nameof(newName));
            Name = newName;
        }

        /// <summary>
        /// 移动到指定父级目录
        /// </summary>
        public void MoveTo(Guid? newParentId)
        {
            if (newParentId == Id)
            {
                throw new Volo.Abp.BusinessException("FileManagement:Directory:CannotBeOwnParent", "目录不能作为自己的父目录");
            }
            ParentId = newParentId;
        }

        #endregion
    }
}
