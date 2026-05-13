using Volo.Abp.ObjectMapping;

namespace SharpFort.Mapster
{
    /// <summary>
    /// Mapster对象映射器
    /// 实现IObjectMapper接口，提供对象映射功能
    /// </summary>
    /// <remarks>
    /// 构造函数
    /// </remarks>
    /// <param name="autoObjectMappingProvider">自动对象映射提供程序</param>
    public sealed class MapsterObjectMapper(IAutoObjectMappingProvider autoObjectMappingProvider) : IObjectMapper
    {
        /// <summary>
        /// 获取自动对象映射提供程序
        /// </summary>
        public IAutoObjectMappingProvider AutoObjectMappingProvider { get; } = autoObjectMappingProvider;

        /// <summary>
        /// 将源对象映射到目标类型
        /// </summary>
        /// <typeparam name="TSource">源类型</typeparam>
        /// <typeparam name="TDestination">目标类型</typeparam>
        /// <param name="source">源对象</param>
        /// <returns>映射后的目标类型实例</returns>
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            return AutoObjectMappingProvider.Map<TSource, TDestination>(source!);
        }

        /// <summary>
        /// 将源对象映射到现有的目标对象
        /// </summary>
        /// <typeparam name="TSource">源类型</typeparam>
        /// <typeparam name="TDestination">目标类型</typeparam>
        /// <param name="source">源对象</param>
        /// <param name="destination">目标对象</param>
        /// <returns>映射后的目标对象</returns>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            return AutoObjectMappingProvider.Map(source, destination);
        }
    }
}
