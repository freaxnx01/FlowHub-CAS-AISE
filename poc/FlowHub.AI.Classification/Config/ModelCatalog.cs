using FlowHub.AI.Classification.Models;
using Microsoft.Extensions.Configuration;

namespace FlowHub.AI.Classification.Config;

public static class ModelCatalog
{
    public static List<ModelConfig> LoadFromConfig(IConfiguration config)
    {
        var models = new List<ModelConfig>();
        config.GetSection("Models").Bind(models);
        return models;
    }
}
