using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;
using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Yi.Framework.Core.Enums;

namespace Yi.Framework.CasbinRbac.Domain.Entities
{
    [SugarTable("casbin_sys_file_storage")]
    public class FileStorage : AggregateRoot<Guid>, IAuditedObject
    {
        public FileStorage()
        {
        }

        /// <summary>
        /// 创建文件
        /// </summary>
        /// <param name="fileId">文件标识id</param>
        /// <param name="fileName">文件名</param>
        /// <param name="fileSize">文件大小</param>
        public FileStorage(Guid fileId, string fileName, decimal fileSize)
        {
            this.Id = fileId;
            this.FileSize = fileSize;
            this.FileName = fileName;

            var type = GetFileType();

            var savePath = GetSaveFilePath();
            var filePath = Path.Combine(savePath, this.FileName);
            this.FilePath = filePath;
        }

        /// <summary>
        /// 检测目录是否存在，不存在便创建
        /// </summary>
        public void CheckDirectoryOrCreate()
        {
            var savePath = GetSaveDirPath();
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
        }

        /// <summary>
        /// 文件类型
        /// </summary>
        /// <returns></returns>
        public FileTypeEnum GetFileType()
        {
            var extension = Path.GetExtension(this.FileName)?.ToLowerInvariant();
            return extension switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" or ".svg" or ".ico" => FileTypeEnum.image,
                _ => FileTypeEnum.file
            };
        }

        /// <summary>
        /// 获取文件mime
        /// </summary>
        /// <returns></returns>
        public string GetMimeMapping()
        {
            var provider = new FileExtensionContentTypeProvider();
            if (provider.TryGetContentType(this.FileName, out var contentType))
            {
                return contentType;
            }
            return "application/octet-stream";
        }

        /// <summary>
        /// 落库目录路径
        /// </summary>
        /// <returns></returns>
        public string GetSaveDirPath()
        {
            return $"wwwroot/{GetFileType()}";
        }

        /// <summary>
        /// 落库文件路径
        /// </summary>
        /// <returns></returns>
        public string GetSaveFilePath()
        {
            string savefileName = GetSaveFileName();
            return Path.Combine(GetSaveDirPath(), savefileName);
        }

        /// <summary>
        /// 获取保存的文件名
        /// </summary>
        /// <returns></returns>
        public string GetSaveFileName()
        {
            return this.Id.ToString() + Path.GetExtension(this.FileName);
        }

        /// <summary>
        /// 检测目录，并返回缩略图的保存路径
        /// </summary>
        /// <param name="isCheak">是否检测并创建缩略图目录（默认 false）</param>
        /// <returns>缩略图文件的完整保存路径</returns>
        /// <remarks>
        /// 使用场景：
        /// 1. 图片文件生成缩略图时需要获取保存路径
        /// 2. 通过 isCheak=true 自动创建缩略图目录（如果不存在）
        ///
        /// 实现逻辑：
        /// - 缩略图统一存储在 wwwroot/thumbnail 目录
        /// - 文件名使用与原文件相同的格式：{FileId}{Extension}
        ///
        /// 注意：参数名 isCheak 存在拼写错误（应为 isCheck），但为保持兼容性未修正
        /// </remarks>
        public string GetAndCheakThumbnailSavePath(bool isCheak=false)
        {
            string thumbnailPath = $"wwwroot/{FileTypeEnum.thumbnail}";
            if (isCheak)
            {
                if (!Directory.Exists(thumbnailPath))
                {
                    Directory.CreateDirectory(thumbnailPath);
                } 
            }
            return Path.Combine(thumbnailPath, GetSaveFileName());
        }


        /// <summary>
        /// 获取查询文件时使用的保存路径（支持缩略图路径）
        /// </summary>
        /// <param name="isThumbnail">是否获取缩略图路径（null 或 false 返回原文件路径，true 返回缩略图路径）</param>
        /// <returns>文件的完整保存路径</returns>
        /// <remarks>
        /// 使用场景：
        /// 1. 前端请求文件时根据需求返回原图或缩略图路径
        /// 2. 图片预览功能（缩略图加载快，点击后查看原图）
        ///
        /// 实现逻辑：
        /// - isThumbnail=true：返回 wwwroot/thumbnail/{FileId}{Extension}
        /// - isThumbnail=null/false：返回 wwwroot/{FileType}/{FileId}{Extension}
        /// </remarks>
        public  string? GetQueryFileSavePath(bool? isThumbnail)
        {
            string fileSavePath;
            //如果为缩略图，需要修改路径
            if (isThumbnail is true)
            {
                fileSavePath = this.GetAndCheakThumbnailSavePath();
            }
            else
            {
                fileSavePath = this.GetSaveFilePath();
            }
            return fileSavePath;
        }
        
        /// <summary>
        /// 文件大小 
        ///</summary>
        [SugarColumn(ColumnName = "FileSize")]
        public decimal FileSize { get; internal set; }

        /// <summary>
        /// 文件名 
        ///</summary>
        [SugarColumn(ColumnName = "FileName")]
        public string FileName { get; internal set; }

        /// <summary>
        /// 文件路径 
        ///</summary>
        [SugarColumn(ColumnName = "FilePath")]
        public string FilePath { get; internal set; }

        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }

        public Guid? LastModifierId { get; set; }

        public DateTime? LastModificationTime { get; set; }
    }
}
