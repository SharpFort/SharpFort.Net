namespace Yi.Framework.Bbs.Domain.Shared.Caches;

public class BbsAccessLogCacheItem
{
    public BbsAccessLogCacheItem(long number)
    {
        Number = number;
    }

    public long Number { get; set; }
    public DateTime LastModificationTime { get; set; }=DateTime.Now;
    
    public DateTime LastInsertTime { get; set; }=DateTime.Now;
}

public class BbsAccessLogCacheConst
{

    public const string Key = "BbsAccessLog";
}