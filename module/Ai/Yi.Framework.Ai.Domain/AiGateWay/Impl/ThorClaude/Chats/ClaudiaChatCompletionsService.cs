using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Yi.Framework.Ai.Domain.Shared.Dtos;
using Yi.Framework.Ai.Domain.Shared.Dtos.Anthropic;
using Yi.Framework.Ai.Domain.Shared.Dtos.OpenAi;

namespace Yi.Framework.Ai.Domain.AiGateWay.Impl.ThorClaude.Chats;

public sealed class ClaudiaChatCompletionsService(
    IHttpClientFactory httpClientFactory,
    ILogger<ClaudiaChatCompletionsService> logger)
    : IChatCompletionService
{
    public List<ThorChatChoiceResponse> CreateResponse(AnthropicChatCompletionDto completionDto)
    {
        var response = new ThorChatChoiceResponse();
        var chatMessage = new ThorChatMessage();
        if (completionDto == null)
        {
            return new List<ThorChatChoiceResponse>();
        }

        if (completionDto.content.Any(x => x.type.Equals("thinking", StringComparison.OrdinalIgnoreCase)))
        {
            // 将推理字段合并到返回对象去
            chatMessage.ReasoningContent = completionDto.content
                .First(x => x.type.Equals("thinking", StringComparison.OrdinalIgnoreCase)).Thinking;

            chatMessage.Role = completionDto.role;
            chatMessage.Content = completionDto.content
                .First(x => x.type.Equals("text", StringComparison.OrdinalIgnoreCase)).text;
        }
        else
        {
            chatMessage.Role = completionDto.role;
            chatMessage.Content = completionDto.content
                .FirstOrDefault()?.text;
        }

        response.Delta = chatMessage;
        response.Message = chatMessage;

        if (completionDto.content.Any(x => x.type.Equals("tool_use", StringComparison.OrdinalIgnoreCase)))
        {
            var toolUse = completionDto.content
                .First(x => x.type.Equals("tool_use", StringComparison.OrdinalIgnoreCase));

            chatMessage.ToolCalls =
            [
                new()
                {
                    Id = toolUse.id,
                    Function = new ThorChatMessageFunction()
                    {
                        Name = toolUse.name,
                        Arguments = JsonSerializer.Serialize(toolUse.input,
                            ThorJsonSerializer.DefaultOptions),
                    },
                    Index = 0,
                }
            ];

            return
            [
                response
            ];
        }

        return new List<ThorChatChoiceResponse> { response };
    }

    private object CreateMessage(List<ThorChatMessage> messages, AiModelDescribe options)
    {
        var list = new List<object>();

        foreach (var message in messages)
        {
            // 如果是图片
            if (message.ContentCalculated is IList<ThorChatMessageContent> contentCalculated)
            {
                list.Add(new
                {
                    role = message.Role,
                    content = (List<object>)contentCalculated.Select<ThorChatMessageContent, object>(x =>
                    {
                        if (x.Type == "text")
                        {
                            if ("true".Equals(options.ModelExtraInfo, StringComparison.OrdinalIgnoreCase))
                            {
                                return new
                                {
                                    type = "text",
                                    text = x.Text,
                                    cache_control = new
                                    {
                                        type = "ephemeral"
                                    }
                                };
                            }

                            return new
                            {
                                type = "text",
                                text = x.Text
                            };
                        }

                        var isBase64 = x.ImageUrl?.Url.StartsWith("http") == true;

                        if ("true".Equals(options.ModelExtraInfo, StringComparison.OrdinalIgnoreCase))
                        {
                            return new
                            {
                                type = "image",
                                source = new
                                {
                                    type = isBase64 ? "base64" : "url",
                                    media_type = "image/png",
                                    data = x.ImageUrl?.Url,
                                },
                                cache_control = new
                                {
                                    type = "ephemeral"
                                }
                            };
                        }

                        return new
                        {
                            type = "image",
                            source = new
                            {
                                type = isBase64 ? "base64" : "url",
                                media_type = "image/png",
                                data = x.ImageUrl?.Url,
                            }
                        };
                    })
                });
            }
            else
            {
                if ("true".Equals(options.ModelExtraInfo, StringComparison.OrdinalIgnoreCase))
                {
                    if (message.Role == "system")
                    {
                        list.Add(new
                        {
                            type = "text",
                            text = message.Content,
                            cache_control = new
                            {
                                type = "ephemeral"
                            }
                        });
                    }
                    else
                    {
                        list.Add(new
                        {
                            role = message.Role,
                            content = message.Content
                        });
                    }
                }
                else
                {
                    if (message.Role == "system")
                    {
                        list.Add(new
                        {
                            type = "text",
                            text = message.Content
                        });
                    }
                    else if (message.Role == "tool")
                    {
                        list.Add(new
                        {
                            role = "user",
                            content = new List<object>
                            {
                                new
                                {
                                    type = "tool_result",
                                    tool_use_id = message.ToolCallId,
                                    content = message.Content
                                }
                            }
                        });
                    }
                    else if (message.Role == "assistant")
                    {
                        // {
                        //     "role": "assistant",
                        //     "content": [
                        //     {
                        //         "type": "text",
                        //         "text": "<thinking>I need to use get_weather, and the user wants SF, which is likely San Francisco, CA.</thinking>"
                        //     },
                        //     {
                        //         "type": "tool_use",
                        //         "id": "toolu_01A09q90qw90lq917835lq9",
                        //         "name": "get_weather",
                        //         "input": {
                        //             "location": "San Francisco, CA",
                        //             "unit": "celsius"
                        //         }
                        //     }
                        //     ]
                        // },
                        if (message.ToolCalls?.Count > 0)
                        {
                            var content = new List<object>();
                            if (!string.IsNullOrEmpty(message.Content))
                            {
                                content.Add(new
                                {
                                    type = "text",
                                    text = message.Content
                                });
                            }

                            foreach (var toolCall in message.ToolCalls)
                            {
                                content.Add(new
                                {
                                    type = "tool_use",
                                    id = toolCall.Id,
                                    name = toolCall.Function?.Name,
                                    input = JsonSerializer.Deserialize<Dictionary<string, object>>(
                                        toolCall.Function?.Arguments, ThorJsonSerializer.DefaultOptions)
                                });
                            }

                            list.Add(new
                            {
                                role = "assistant",
                                content
                            });
                        }
                        else
                        {
                            list.Add(new
                            {
                                role = "assistant",
                                content = new List<object>
                                {
                                    new
                                    {
                                        type = "text",
                                        text = message.Content
                                    }
                                }
                            });
                        }
                    }
                    else
                    {
                        list.Add(new
                        {
                            role = message.Role,
                            content = message.Content
                        });
                    }
                }
            }
        }

        return list;
    }


    public async IAsyncEnumerable<ThorChatCompletionsResponse> CompleteChatStreamAsync(AiModelDescribe options,
        ThorChatCompletionsRequest input,
        CancellationToken cancellationToken)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("Claudia 对话补全");

        if (string.IsNullOrEmpty(options.Endpoint))
        {
            options.Endpoint = "https://api.anthropic.com/";
        }

        var client = httpClientFactory.CreateClient();

        var headers = new Dictionary<string, string>
        {
            { "x-api-key", options.ApiKey },
            { "anthropic-version", "2023-06-01" }
        };

        var isThinking = input.Model.EndsWith("thinking");
        input.Model = input.Model.Replace("-thinking", string.Empty);
        var budgetTokens = 1024;

        if (input.MaxTokens is < 2048)
        {
            input.MaxTokens = 2048;
        }

        if (input.MaxTokens != null && input.MaxTokens / 2 < 1024)
        {
            budgetTokens = input.MaxTokens.Value / (4 * 3);
        }

        // budgetTokens最大4096
        budgetTokens = Math.Min(budgetTokens, 4096);

        object tool_choice;
        if (input.ToolChoice is not null && input.ToolChoice.Type == "auto")
        {
            tool_choice = new
            {
                type = "auto",
                disable_parallel_tool_use = false,
            };
        }
        else if (input.ToolChoice is not null && input.ToolChoice.Type == "any")
        {
            tool_choice = new
            {
                type = "any",
                disable_parallel_tool_use = false,
            };
        }
        else if (input.ToolChoice is not null && input.ToolChoice.Type == "tool")
        {
            tool_choice = new
            {
                type = "tool",
                name = input.ToolChoice.Function?.Name,
                disable_parallel_tool_use = false,
            };
        }
        else
        {
            tool_choice = null;
        }

        var response = await client.HttpRequestRaw(options.Endpoint.TrimEnd('/') + "/v1/messages", new
        {
            model = input.Model,
            max_tokens = input.MaxTokens ?? 64000,
            stream = true,
            tool_choice,
            system = CreateMessage(input.Messages.Where(x => x.Role == "system").ToList(), options),
            messages = CreateMessage(input.Messages.Where(x => x.Role != "system").ToList(), options),
            top_p = isThinking ? null : input.TopP,
            thinking = isThinking
                ? new
                {
                    type = "enabled",
                    budget_tokens = budgetTokens,
                }
                : null,
            tools = input.Tools?.Select(x => new
            {
                name = x.Function?.Name,
                description = x.Function?.Description,
                input_schema = new
                {
                    type = x.Function?.Parameters?.Type,
                    required = x.Function?.Parameters?.Required,
                    properties = x.Function?.Parameters?.Properties?.ToDictionary(y => y.Key, y => new
                    {
                        description = y.Value.Description,
                        type = y.Value.Type,
                        @enum = y.Value.Enum
                    })
                }
            }).ToArray(),
            temperature = isThinking ? null : input.Temperature
        }, string.Empty, headers);

        openai?.SetTag("Model", input.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("OpenAI对话异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}",
                options.Endpoint,
                response.StatusCode, error);

            throw new Exception("OpenAI对话异常" + response.StatusCode);
        }

        using var stream = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));

        using StreamReader reader = new(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? line = string.Empty;
        var first = true;
        var isThink = false;

        string? toolId = null;
        string? toolName = null;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            line += Environment.NewLine;

            if (line.StartsWith('{'))
            {
                logger.LogInformation("OpenAI对话异常 , StatusCode: {StatusCode} Response: {Response}", response.StatusCode,
                    line);

                throw new Exception("OpenAI对话异常" + line);
            }

            if (line.StartsWith(OpenAIConstant.Data))
                line = line[OpenAIConstant.Data.Length..];

            line = line.Trim();

            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line == OpenAIConstant.Done)
            {
                break;
            }

            if (line.StartsWith(':'))
            {
                continue;
            }

            if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var result = JsonSerializer.Deserialize<AnthropicStreamDto>(line,
                ThorJsonSerializer.DefaultOptions);

            if (result?.Type == "content_block_delta")
            {
                if (result.Delta.Type is "text" or "text_delta")
                {
                    yield return new ThorChatCompletionsResponse()
                    {
                        Choices =
                        [
                            new()
                            {
                                Message = new ThorChatMessage()
                                {
                                    Content = result.Delta.Text,
                                    Role = "assistant",
                                }
                            }
                        ],
                        Model = input.Model,
                        Id = result?.Message?.id,
                        Usage = new ThorUsageResponse()
                        {
                            CompletionTokens = result?.Message?.Usage?.OutputTokens,
                            PromptTokens = result?.Message?.Usage?.InputTokens,
                        }
                    };
                }
                else if (result.Delta.Type == "input_json_delta")
                {
                    yield return new ThorChatCompletionsResponse()
                    {
                        Choices =
                        [
                            new ThorChatChoiceResponse()
                            {
                                Message = new ThorChatMessage()
                                {
                                    ToolCalls =
                                    [
                                        new ThorToolCall()
                                        {
                                            Id = toolId,
                                            Function = new ThorChatMessageFunction()
                                            {
                                                Name = toolName,
                                                Arguments = result.Delta.PartialJson
                                            }
                                        }
                                    ],
                                    Role = "tool",
                                }
                            }
                        ],
                        Model = input.Model,
                        Usage = new ThorUsageResponse()
                        {
                            PromptTokens = result?.Message?.Usage?.InputTokens,
                        }
                    };
                }
                else
                {
                    yield return new ThorChatCompletionsResponse()
                    {
                        Choices = new List<ThorChatChoiceResponse>()
                        {
                            new()
                            {
                                Message = new ThorChatMessage()
                                {
                                    ReasoningContent = result.Delta.Thinking,
                                    Role = "assistant",
                                }
                            }
                        },
                        Model = input.Model,
                        Id = result?.Message?.id,
                        Usage = new ThorUsageResponse()
                        {
                            CompletionTokens = result?.Message?.Usage?.OutputTokens,
                            PromptTokens = result?.Message?.Usage?.InputTokens
                        }
                    };
                }

                continue;
            }

            if (result?.Type == "content_block_start")
            {
                if (result?.ContentBlock?.Id is not null)
                {
                    toolId = result.ContentBlock.Id;
                }

                if (result?.ContentBlock?.Name is not null)
                {
                    toolName = result.ContentBlock.Name;
                }

                if (toolId is null)
                {
                    continue;
                }

                yield return new ThorChatCompletionsResponse()
                {
                    Choices =
                    [
                        new ThorChatChoiceResponse()
                        {
                            Message = new ThorChatMessage()
                            {
                                ToolCalls =
                                [
                                    new ThorToolCall()
                                    {
                                        Id = toolId,
                                        Function = new ThorChatMessageFunction()
                                        {
                                            Name = toolName
                                        }
                                    }
                                ],
                                Role = "tool",
                            }
                        }
                    ],
                    Model = input.Model,
                    Usage = new ThorUsageResponse()
                    {
                        PromptTokens = result?.Message?.Usage?.InputTokens,
                    }
                };
            }

            if (result.Type == "content_block_delta")
            {
                yield return new ThorChatCompletionsResponse()
                {
                    Choices =
                    [
                        new ThorChatChoiceResponse()
                        {
                            Message = new ThorChatMessage()
                            {
                                ToolCallId = result?.ContentBlock?.Id,
                                FunctionCall = new ThorChatMessageFunction()
                                {
                                    Name = result?.ContentBlock?.Name,
                                    Arguments = result?.Delta?.PartialJson
                                },
                                Role = "tool",
                            }
                        }
                    ],
                    Model = input.Model,
                    Usage = new ThorUsageResponse()
                    {
                        PromptTokens = result?.Message?.Usage?.InputTokens
                    }
                };
                continue;
            }

            if (result.Type == "message_start")
            {
                yield return new ThorChatCompletionsResponse()
                {
                    Choices =
                    [
                        new ThorChatChoiceResponse()
                        {
                            Message = new ThorChatMessage()
                            {
                                Content = result?.Delta?.Text,
                                Role = "assistant",
                            }
                        }
                    ],
                    Model = input.Model,
                    Usage = new ThorUsageResponse()
                    {
                        PromptTokens = result?.Message?.Usage?.InputTokens,
                    }
                };

                continue;
            }

            if (result.Type == "message_delta")
            {
                var deltaOutput = new ThorChatCompletionsResponse()
                {
                    Choices =
                    [
                        new ThorChatChoiceResponse()
                        {
                            Message = new ThorChatMessage()
                            {
                                Content = result.Delta?.Text,
                                Role = "assistant",
                            }
                        }
                    ],
                    Model = input.Model,
                    Usage = new ThorUsageResponse
                    {
                        InputTokens = result.Usage?.InputTokens + result.Usage?.CacheCreationInputTokens +
                                      result.Usage?.CacheReadInputTokens,
                        OutputTokens = result.Usage?.OutputTokens,
                    }
                };


                deltaOutput.Usage.PromptTokens = deltaOutput.Usage.InputTokens;
                deltaOutput.Usage.CompletionTokens = deltaOutput.Usage.OutputTokens;

                deltaOutput.Usage.TotalTokens = deltaOutput.Usage.InputTokens + deltaOutput.Usage.OutputTokens;

                yield return deltaOutput;

                continue;
            }

            if (result.Message == null)
            {
                continue;
            }

            var chat = CreateResponse(result.Message);

            var content = chat?.FirstOrDefault()?.Delta;

            if (first && string.IsNullOrWhiteSpace(content?.Content) && string.IsNullOrEmpty(content?.ReasoningContent))
            {
                continue;
            }

            if (first && content.Content == OpenAIConstant.ThinkStart)
            {
                isThink = true;
                continue;
                // 需要将content的内容转换到其他字段
            }

            if (isThink && content.Content.Contains(OpenAIConstant.ThinkEnd))
            {
                isThink = false;
                // 需要将content的内容转换到其他字段
                continue;
            }

            if (isThink)
            {
                // 需要将content的内容转换到其他字段
                foreach (var choice in chat)
                {
                    choice.Delta.ReasoningContent = choice.Delta.Content;
                    choice.Delta.Content = string.Empty;
                }
            }

            first = false;

            var output = new ThorChatCompletionsResponse()
            {
                Choices = chat,
                Model = input.Model,
                Id = result.Message.id,
                Usage = new ThorUsageResponse()
                {
                    InputTokens = result.Message.Usage?.InputTokens + result.Message.Usage?.CacheCreationInputTokens +
                                  result.Message.Usage?.CacheReadInputTokens,
                    OutputTokens = result.Message.Usage?.OutputTokens,
                }
            };
            output.Usage.PromptTokens = output.Usage.InputTokens;
            output.Usage.CompletionTokens = output.Usage.OutputTokens;
            output.Usage.TotalTokens = output.Usage.InputTokens + output.Usage.OutputTokens;

            yield return output;
        }
    }

    public async Task<ThorChatCompletionsResponse> CompleteChatAsync(AiModelDescribe options,
        ThorChatCompletionsRequest input,
        CancellationToken cancellationToken)
    {
        using var openai =
            Activity.Current?.Source.StartActivity("Claudia 对话补全");

        if (string.IsNullOrEmpty(options.Endpoint))
        {
            options.Endpoint = "https://api.anthropic.com/";
        }

        var client = httpClientFactory.CreateClient();

        var headers = new Dictionary<string, string>
        {
            { "x-api-key", options.ApiKey },
            { "anthropic-version", "2023-06-01" }
        };

        bool isThink = input.Model.EndsWith("-thinking");
        input.Model = input.Model.Replace("-thinking", string.Empty);

        var budgetTokens = 1024;
        if (input.MaxTokens is < 2048)
        {
            input.MaxTokens = 2048;
        }

        if (input.MaxTokens != null && input.MaxTokens / 2 < 1024)
        {
            budgetTokens = input.MaxTokens.Value / (4 * 3);
        }

        object tool_choice;
        if (input.ToolChoice is not null && input.ToolChoice.Type == "auto")
        {
            tool_choice = new
            {
                type = "auto",
                disable_parallel_tool_use = false,
            };
        }
        else if (input.ToolChoice is not null && input.ToolChoice.Type == "any")
        {
            tool_choice = new
            {
                type = "any",
                disable_parallel_tool_use = false,
            };
        }
        else if (input.ToolChoice is not null && input.ToolChoice.Type == "tool")
        {
            tool_choice = new
            {
                type = "tool",
                name = input.ToolChoice.Function?.Name,
                disable_parallel_tool_use = false,
            };
        }
        else
        {
            tool_choice = null;
        }

        // budgetTokens最大4096
        budgetTokens = Math.Min(budgetTokens, 4096);

        var response = await client.PostJsonAsync(options.Endpoint.TrimEnd('/') + "/v1/messages", new
        {
            model = input.Model,
            max_tokens = input.MaxTokens ?? 2000,
            system = CreateMessage(input.Messages.Where(x => x.Role == "system").ToList(), options),
            messages = CreateMessage(input.Messages.Where(x => x.Role != "system").ToList(), options),
            top_p = isThink ? null : input.TopP,
            tool_choice,
            thinking = isThink
                ? new
                {
                    type = "enabled",
                    budget_tokens = budgetTokens,
                }
                : null,
            tools = input.Tools?.Select(x => new
            {
                name = x.Function?.Name,
                description = x.Function?.Description,
                input_schema = new
                {
                    type = x.Function?.Parameters?.Type,
                    required = x.Function?.Parameters?.Required,
                    properties = x.Function?.Parameters?.Properties?.ToDictionary(y => y.Key, y => new
                    {
                        description = y.Value.Description,
                        type = y.Value.Type,
                        @enum = y.Value.Enum
                    })
                }
            }).ToArray(),
            temperature = isThink ? null : input.Temperature
        }, string.Empty, headers);

        openai?.SetTag("Model", input.Model);
        openai?.SetTag("Response", response.StatusCode.ToString());

        // 大于等于400的状态码都认为是异常
        if (response.StatusCode >= HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            logger.LogError("OpenAI对话异常 请求地址：{Address}, StatusCode: {StatusCode} Response: {Response}",
                options.Endpoint,
                response.StatusCode, error);

            throw new Exception("OpenAI对话异常" + response.StatusCode.ToString());
        }

        var value =
            await response.Content.ReadFromJsonAsync<AnthropicChatCompletionDto>(ThorJsonSerializer.DefaultOptions,
                cancellationToken: cancellationToken);

        var thor = new ThorChatCompletionsResponse()
        {
            Choices = CreateResponse(value),
            Model = input.Model,
            Id = value.id,
            Usage = new ThorUsageResponse()
            {
                CompletionTokens = value.Usage.OutputTokens,
                PromptTokens = value.Usage.InputTokens
            }
        };

        if (value.Usage.CacheReadInputTokens != null)
        {
            thor.Usage.PromptTokensDetails ??= new ThorUsageResponsePromptTokensDetails()
            {
                CachedTokens = value.Usage.CacheReadInputTokens.Value,
            };

            if (value.Usage.InputTokens > 0)
            {
                thor.Usage.InputTokens = value.Usage.InputTokens;
            }

            if (value.Usage.OutputTokens > 0)
            {
                thor.Usage.CompletionTokens = value.Usage.OutputTokens;
                thor.Usage.OutputTokens = value.Usage.OutputTokens;
            }
        }

        thor.Usage.TotalTokens = thor.Usage.InputTokens + thor.Usage.OutputTokens;
        return thor;
    }
}
