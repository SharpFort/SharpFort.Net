using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Images;

namespace Yi.Framework.Ai.Domain.AiGateWay;

public interface IImageService
{
    /// <summary>Creates an image given a prompt.</summary>
    /// <param name="imageCreate"></param>
    /// <param name="aiModelDescribe"></param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<ImageCreateResponse> CreateImage(
        ImageCreateRequest imageCreate,
        AiModelDescribe? aiModelDescribe = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates an edited or extended image given an original image and a prompt.
    /// </summary>
    /// <param name="imageEditCreateRequest"></param>
    /// <param name="aiModelDescribe"></param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<ImageCreateResponse> CreateImageEdit(
        ImageEditCreateRequest imageEditCreateRequest,
        AiModelDescribe? aiModelDescribe = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a variation of a given image.</summary>
    /// <param name="imageEditCreateRequest"></param>
    /// <param name="aiModelDescribe"></param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<ImageCreateResponse> CreateImageVariation(
        ImageVariationCreateRequest imageEditCreateRequest,
        AiModelDescribe? aiModelDescribe = null,
        CancellationToken cancellationToken = default);
}
