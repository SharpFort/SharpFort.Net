using Mapster;
using SqlSugar;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DistributedLocking;
using SharpFort.Ai.Application.Contracts.Dtos.AiKms;
using SharpFort.Ai.Application.Contracts.IServices;
using SharpFort.Ai.Domain.Entities;
using SharpFort.Ai.Domain.RAG;
using SharpFort.Ai.Domain.Shared.Enums;
using SharpFort.Ai.Domain.AiGateWay;
using SharpFort.Ai.Domain.Shared.Dtos;
using SharpFort.SqlSugarCore.Abstractions;

namespace SharpFort.Ai.Application.Services;

public class AiKmsService : ApplicationService, IAiKmsService
{
    private readonly ISqlSugarRepository<AiKms, Guid> _kmsRepository;
    private readonly ISqlSugarRepository<AiKmsDetail, Guid> _detailRepository;
    private readonly ISqlSugarRepository<AiModel, Guid> _modelRepository;
    private readonly ISqlSugarRepository<AiProvider, Guid> _providerRepository;
    private readonly IAbpDistributedLock _distributedLock;

    public AiKmsService(
        ISqlSugarRepository<AiKms, Guid> kmsRepository,
        ISqlSugarRepository<AiKmsDetail, Guid> detailRepository,
        ISqlSugarRepository<AiModel, Guid> modelRepository,
        ISqlSugarRepository<AiProvider, Guid> providerRepository,
        IAbpDistributedLock distributedLock)
    {
        _kmsRepository = kmsRepository;
        _detailRepository = detailRepository;
        _modelRepository = modelRepository;
        _providerRepository = providerRepository;
        _distributedLock = distributedLock;
    }

    public async Task<PagedResultDto<AiKmsDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        RefAsync<int> total = 0;
        var items = await _kmsRepository._DbQueryable
            .Where(t => !t.IsDeleted)
            .OrderByDescending(x => x.CreationTime)
            .ToPageListAsync(input.SkipCount, input.MaxResultCount, total);
        var dtos = items.Adapt<List<AiKmsDto>>();
        foreach (var dto in dtos)
        {
            dto.AiKmsDetailList = (await _detailRepository._DbQueryable
                .Where(t => !t.IsDeleted && t.KmsId == dto.Id)
                .OrderByDescending(x => x.CreationTime)
                .ToListAsync()).Adapt<List<AiKmsDetailDto>>();
        }
        return new PagedResultDto<AiKmsDto>(total, dtos);
    }

    public async Task<List<AiKmsDto>> GetAllListAsync()
    {
        var items = await _kmsRepository._DbQueryable
            .Where(t => !t.IsDeleted)
            .OrderByDescending(x => x.CreationTime).ToListAsync();
        return items.Adapt<List<AiKmsDto>>();
    }

    public async Task<AiKmsDto> GetAsync(Guid id)
    {
        var entity = await _kmsRepository.GetByIdAsync(id);
        var dto = entity.Adapt<AiKmsDto>();
        dto.AiKmsDetailList = (await _detailRepository._DbQueryable
            .Where(t => !t.IsDeleted && t.KmsId == id)
            .OrderByDescending(x => x.CreationTime)
            .ToListAsync()).Adapt<List<AiKmsDetailDto>>();
        return dto;
    }

    public async Task<AiKmsDto> CreateAsync(AiKmsDto input)
    {
        var entity = input.Adapt<AiKms>();
        entity.IsDeleted = false;
        await _kmsRepository.InsertAsync(entity);
        foreach (var detail in input.AiKmsDetailList)
        {
            var detailEntity = detail.Adapt<AiKmsDetail>();
            detailEntity.KmsId = entity.Id;
            detailEntity.IsDeleted = false;
            await _detailRepository.InsertAsync(detailEntity);
        }
        return entity.Adapt<AiKmsDto>();
    }

    public async Task<AiKmsDto> UpdateAsync(Guid id, AiKmsDto input)
    {
        var entity = await _kmsRepository.GetByIdAsync(id);
        input.Adapt(entity);
        await _kmsRepository.UpdateAsync(entity);
        return entity.Adapt<AiKmsDto>();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _kmsRepository.DeleteAsync(id);
    }

    public async Task ProcessKmssVectorDataAsync(Guid? kmsId = null)
    {
        var lockKey = $"SharpFort.Ai.ProcessKmssVectorData:{kmsId}";
        await using var handle = await _distributedLock.TryAcquireAsync(lockKey);
        if (handle == null) return;

        try
        {
            var query = _detailRepository._DbQueryable
                .Where(t => !t.IsDeleted && t.Status == ImportKmsStatus.Loading);
            if (kmsId.HasValue)
                query = query.Where(t => t.KmsId == kmsId.Value);

            var data = await query.ToListAsync();
            var kmssList = await _kmsRepository._DbQueryable
                .Where(t => !t.IsDeleted && data.Select(d => d.KmsId).Distinct().Contains(t.Id))
                .ToListAsync();

            if (data.Count == 0 || kmssList.Count == 0) return;

            foreach (var item in data)
            {
                try
                {
                    var kmss = kmssList.FirstOrDefault(t => t.Id == item.KmsId);
                    if (kmss == null) continue;

                    string content = item.Content ?? "";
                    string fileName = item.ContentName ?? "";

                    if (!string.IsNullOrEmpty(content))
                    {
                        // Get embedding model and provider
                        if (kmss.AiModelId == null) continue;
                        var aiModel = await _modelRepository._DbQueryable
                            .Where(t => t.Id == kmss.AiModelId.Value)
                            .FirstAsync();
                        if (aiModel == null) continue;

                        var provider = await _providerRepository._DbQueryable
                            .Where(t => t.Id == aiModel.AiProviderId)
                            .FirstAsync();

                        // Create embedding adapter
                        var embeddingService = LazyServiceProvider.LazyGetRequiredService<ITextEmbeddingService>();
                        var modelDescribe = new AiModelDescribe
                        {
                            Endpoint = provider?.Endpoint ?? "",
                            ModelId = aiModel.ModelId ?? "",
                            ApiKey = provider?.ApiKey ?? "",
                            HandlerName = aiModel.HandlerName ?? ""
                        };
                        using var embeddingAdapter = new EmbeddingGatewayAdapter(embeddingService, aiModel.ModelId ?? "", modelDescribe);

                        // Create vector storage using repository's SqlSugar client
                        using var storageService = new PgVectorStorageService(_kmsRepository._Db);

                        // Process document
                        var documentProcessor = new DocumentProcessor(
                            chunkSize: kmss.MaxTokensPerParagraph,
                            chunkOverlap: kmss.OverlappingTokens);

                        var cleanedContent = documentProcessor.CleanDocument(content);
                        var chunks = documentProcessor.ChunkByParagraph(cleanedContent)
                            .Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

                        var allChunks = new List<DocumentChunkDto>();
                        for (int i = 0; i < chunks.Count; i++)
                        {
                            allChunks.Add(new DocumentChunkDto
                            {
                                Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + i,
                                Content = chunks[i],
                                SourceFile = fileName,
                                Title = fileName.ToLower()
                                    .Replace(".txt", "").Replace(".word", "")
                                    .Replace(".pdf", "").Replace(".markdown", "").Replace(".html", ""),
                                Category = kmss.Name ?? "",
                                ChunkIndex = i,
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                        }

                        ulong embeddingValueSize = (ulong)(aiModel.EmbeddingValueSize > 0 ? aiModel.EmbeddingValueSize : 2048);

                        foreach (var chunk in allChunks)
                        {
                            chunk.ContentVector = await embeddingAdapter.GetEmbeddingAsync(chunk.Content);
                        }

                        var isOk = await storageService.AddDataAsync("AiKms_" + kmss.Id.ToString("N"), allChunks, embeddingValueSize);
                        if (isOk)
                        {
                            item.Status = ImportKmsStatus.Success;
                            item.ContentName = fileName;
                        }
                    }
                    else
                    {
                        item.Status = ImportKmsStatus.Success;
                        item.ContentName = fileName;
                    }

                    item.LastModificationTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    item.Status = ImportKmsStatus.Fail;
                    item.ErrorMessage = ex.Message;
                    item.LastModificationTime = DateTime.Now;
                }
            }

            await _detailRepository._Db.Updateable(data).ExecuteCommandAsync();
        }
        catch (Exception)
        {
            // Log error
        }
    }
}
