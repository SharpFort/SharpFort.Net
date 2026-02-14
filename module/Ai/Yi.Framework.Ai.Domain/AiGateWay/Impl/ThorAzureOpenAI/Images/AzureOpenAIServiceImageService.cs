using OpenAI.Images;
using Yi.Framework.Ai.Domain.AiGateWay;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi.Images;

namespace Yi.Framework.Ai.Domain.AiGateWay.Impl.ThorAzureOpenAI.Images;

public class AzureOpenAIServiceImageService(IHttpClientFactory httpClientFactory) : IImageService
{
    public async Task<ImageCreateResponse> CreateImage(ImageCreateRequest imageCreate, AiModelDescribe? options = null,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        var createClient = AzureOpenAIFactory.CreateClient(options);

        var client = createClient.GetImageClient(imageCreate.Model);
        imageCreate.Size??="1024x1024";
        // 将size字符串拆分为宽度和高度
        var size = imageCreate.Size.Split('x');
        if (size.Length != 2)
        {
            throw new ArgumentException("Size must be in the format of 'width x height'");
        }


        var response = await client.GenerateImageAsync(imageCreate.Prompt, new ImageGenerationOptions()
        {
            Quality = imageCreate.Quality == "standard" ? GeneratedImageQuality.Standard : GeneratedImageQuality.High,
            Size = new GeneratedImageSize(Convert.ToInt32(size[0]), Convert.ToInt32(size[1])),
            Style = imageCreate.Style == "vivid" ? GeneratedImageStyle.Vivid : GeneratedImageStyle.Natural,
            ResponseFormat =
                imageCreate.ResponseFormat == "url" ? GeneratedImageFormat.Uri : GeneratedImageFormat.Bytes,
            // User = imageCreate.User
            EndUserId = imageCreate.User
        }, cancellationToken);

        var ret = new ImageCreateResponse()
        {
            Results = new List<ImageCreateResponse.ImageDataResult>()
        };

        if (response.Value.ImageUri != null)
        {
            ret.Results.Add(new ImageCreateResponse.ImageDataResult()
            {
                Url = response.Value.ImageUri.ToString()
            });
        }
        else
        {
            ret.Results.Add(new ImageCreateResponse.ImageDataResult()
            {
                B64 = Convert.ToBase64String(response.Value.ImageBytes.ToArray())
            });
        }

        return ret;
    }

    public async Task<ImageCreateResponse> CreateImageEdit(ImageEditCreateRequest imageEditCreateRequest,
        AiModelDescribe? options = null,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        var url = AzureOpenAIFactory.GetEditImageAddress(options, imageEditCreateRequest.Model);
        
        var multipartContent = new MultipartFormDataContent();
        if (imageEditCreateRequest.User != null)
        {
            multipartContent.Add(new StringContent(imageEditCreateRequest.User), "user");
        }

        if (imageEditCreateRequest.ResponseFormat != null)
        {
            multipartContent.Add(new StringContent(imageEditCreateRequest.ResponseFormat), "response_format");
        }

        if (imageEditCreateRequest.Size != null)
        {
            multipartContent.Add(new StringContent(imageEditCreateRequest.Size), "size");
        }

        if (imageEditCreateRequest.N != null)
        {
            multipartContent.Add(new StringContent(imageEditCreateRequest.N.ToString()!), "n");
        }

        if (imageEditCreateRequest.Model != null)
        {
            multipartContent.Add(new StringContent(imageEditCreateRequest.Model!), "model");
        }

        if (imageEditCreateRequest.Mask != null)
        {
            multipartContent.Add(new ByteArrayContent(imageEditCreateRequest.Mask), "mask",
                imageEditCreateRequest.MaskName);
        }

        multipartContent.Add(new StringContent(imageEditCreateRequest.Prompt), "prompt");
        multipartContent.Add(new ByteArrayContent(imageEditCreateRequest.Image), "image",
            imageEditCreateRequest.ImageName);

        return await httpClientFactory.CreateClient().PostFileAndReadAsAsync<ImageCreateResponse>(
            url,
            multipartContent, cancellationToken);
    }

    public Task<ImageCreateResponse> CreateImageVariation(ImageVariationCreateRequest imageEditCreateRequest,
        AiModelDescribe? options = null,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        throw new NotImplementedException();
    }
}
