using Guider.API.MVP.Data;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace Guider.API.MVP.Services
{
    public class TagsService
    {
        private readonly IMongoCollection<BsonDocument> _tagsCollection;
        public TagsService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _tagsCollection = database.GetCollection<BsonDocument>(
                mongoSettings.Value.Collections["Tags"]);
        }

        public async Task<JsonDocument> GetTagsByTypeAsync(string typeName)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("name_en", typeName);
                var projection = Builders<BsonDocument>.Projection.Include("tags").Exclude("_id");
                var result = await _tagsCollection.Find(filter).Project(projection).FirstOrDefaultAsync();

                if (result == null || !result.Contains("tags"))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Type tags not found or no tags available."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var tags = result["tags"].AsBsonArray
                    .Select(tag => new
                    {
                        NameEn = tag["name_en"].AsString,
                        NameSp = tag["name_sp"].AsString,
                        Web = tag["web"].AsString
                    })
                    .ToList();

                var successResponse = new
                {
                    IsSuccess = true,
                    Tags = tags
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
        }

        public async Task<JsonDocument> GetTagsByTypeAndNameAsync(string typeName, string tagName)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("name_en", typeName) &
                             Builders<BsonDocument>.Filter.ElemMatch("tags", Builders<BsonDocument>.Filter.Eq("name_en", tagName));
                var projection = Builders<BsonDocument>.Projection.Include("tags").Exclude("_id");
                var result = await _tagsCollection.Find(filter).Project(projection).FirstOrDefaultAsync();
                if (result == null || !result.Contains("tags"))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Type tags not found or no tags available."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }
                var tags = result["tags"].AsBsonArray
                    .Where(tag => tag["name_en"] == tagName)
                    .Select(tag => new
                    {
                        NameEn = tag["name_en"].AsString,
                        NameSp = tag["name_sp"].AsString,
                        Web = tag["web"].AsString
                    })
                    .ToList();
                var successResponse = new
                {
                    IsSuccess = true,
                    Tags = tags
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
        }


        //public async Task<JsonDocument> CreateTagAsync(string typeName, JsonDocument newTagData)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(typeName))
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = "Type name cannot be null or empty."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        if (newTagData == null || !newTagData.RootElement.TryGetProperty("name_en", out _) ||
        //            !newTagData.RootElement.TryGetProperty("name_sp", out _) ||
        //            !newTagData.RootElement.TryGetProperty("web", out _))
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = "Invalid tag data. Required fields: name_en, name_sp, web."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        // Check if the tag exists
        //        var existingTagFilter = Builders<BsonDocument>.Filter.Eq("name_en", typeName) &
        //                                 Builders<BsonDocument>.Filter.ElemMatch("tags", Builders<BsonDocument>.Filter.Eq("name_en", newTagData.RootElement.GetProperty("name_en").GetString()));
        //        var existingTag = await _tagsCollection.Find(existingTagFilter).FirstOrDefaultAsync();
        //        if (existingTag != null)
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = $"Tag '{newTagData.RootElement.GetProperty("name_en").GetString()}' already exists in type '{typeName}'."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        var filter = Builders<BsonDocument>.Filter.Eq("name_en", typeName);
        //        var update = Builders<BsonDocument>.Update.Push("tags", new BsonDocument
        //        {
        //            { "name_en", newTagData.RootElement.GetProperty("name_en").GetString() },
        //            { "name_sp", newTagData.RootElement.GetProperty("name_sp").GetString() },
        //            { "web", newTagData.RootElement.GetProperty("web").GetString() }
        //        });

        //        var result = await _tagsCollection.UpdateOneAsync(filter, update);

        //        if (result.ModifiedCount == 0)
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = "Failed to add the tag. Type not found or update operation failed."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        var successResponse = new
        //        {
        //            IsSuccess = true,
        //            Message = "Tag successfully created.",
        //            Tag = new
        //            {
        //                NameEn = newTagData.RootElement.GetProperty("name_en").GetString(),
        //                NameSp = newTagData.RootElement.GetProperty("name_sp").GetString(),
        //                Web = newTagData.RootElement.GetProperty("web").GetString()
        //            }
        //        };
        //        return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
        //    }
        //    catch (Exception ex)
        //    {
        //        var errorResponse = new
        //        {
        //            IsSuccess = false,
        //            Message = $"An error occurred: {ex.Message}"
        //        };
        //        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //    }
        //}

        public async Task<JsonDocument> CreateTagAsync(string typeName, JsonDocument newTagData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Type name cannot be null or empty."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                if (newTagData == null || !newTagData.RootElement.TryGetProperty("name_en", out _) ||
                    !newTagData.RootElement.TryGetProperty("name_sp", out _) ||
                    !newTagData.RootElement.TryGetProperty("web", out _))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Invalid tag data. Required fields: name_en, name_sp, web."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                string newTagName = newTagData.RootElement.GetProperty("name_en").GetString();

                // Проверка существования тега внутри своего типа
                var existingTagFilter = Builders<BsonDocument>.Filter.Eq("name_en", typeName) &
                                         Builders<BsonDocument>.Filter.ElemMatch("tags", Builders<BsonDocument>.Filter.Eq("name_en", newTagName));
                var existingTag = await _tagsCollection.Find(existingTagFilter).FirstOrDefaultAsync();
                if (existingTag != null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"Tag '{newTagName}' already exists in type '{typeName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Новая проверка уникальности тега во всей коллекции типов
                var globalUniquenessFilter = Builders<BsonDocument>.Filter.ElemMatch("tags",
                                             Builders<BsonDocument>.Filter.Eq("name_en", newTagName));
                var duplicateTagInOtherType = await _tagsCollection.Find(globalUniquenessFilter).FirstOrDefaultAsync();
                if (duplicateTagInOtherType != null)
                {
                    string otherTypeName = duplicateTagInOtherType["name_en"].AsString;
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"Tag '{newTagName}' already exists in type '{otherTypeName}'. Tags must be globally unique."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var filter = Builders<BsonDocument>.Filter.Eq("name_en", typeName);
                var update = Builders<BsonDocument>.Update.Push("tags", new BsonDocument
        {
            { "name_en", newTagName },
            { "name_sp", newTagData.RootElement.GetProperty("name_sp").GetString() },
            { "web", newTagData.RootElement.GetProperty("web").GetString() }
        });

                var result = await _tagsCollection.UpdateOneAsync(filter, update);

                if (result.ModifiedCount == 0)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Failed to add the tag. Type not found or update operation failed."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = "Tag successfully created.",
                    Tag = new
                    {
                        NameEn = newTagName,
                        NameSp = newTagData.RootElement.GetProperty("name_sp").GetString(),
                        Web = newTagData.RootElement.GetProperty("web").GetString()
                    }
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
        }

        //public async Task<JsonDocument> UpdateTagAsync(string typeName, string tagName, JsonDocument updateTagData)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(tagName))
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = "Type name and tag name cannot be null or empty."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        if (updateTagData == null || !updateTagData.RootElement.TryGetProperty("name_sp", out _) ||
        //            !updateTagData.RootElement.TryGetProperty("web", out _))
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = "Invalid tag data. Required fields: name_sp, web."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        // Проверка существования тега внутри своего типа
        //        var existingTagFilter = Builders<BsonDocument>.Filter.And(
        //            Builders<BsonDocument>.Filter.Eq("name_en", typeName),
        //            Builders<BsonDocument>.Filter.AnyEq("tags.name_en", tagName)
        //        );

        //        var tagExists = await _tagsCollection.Find(existingTagFilter).FirstOrDefaultAsync();
        //        if (tagExists == null)
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = $"Tag '{tagName}' does not exist in type '{typeName}'."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        // Обновляем данные тега
        //        var filter = Builders<BsonDocument>.Filter.Eq("name_en", typeName);
        //        var update = Builders<BsonDocument>.Update.Set(
        //            "tags.$[elem].name_sp", updateTagData.RootElement.GetProperty("name_sp").GetString())
        //            .Set("tags.$[elem].web", updateTagData.RootElement.GetProperty("web").GetString());

        //        var arrayFilters = new List<ArrayFilterDefinition>
        //{
        //    new BsonDocumentArrayFilterDefinition<BsonDocument>(
        //        new BsonDocument("elem.name_en", tagName))
        //};

        //        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        //        var result = await _tagsCollection.UpdateOneAsync(filter, update, updateOptions);

        //        if (result.ModifiedCount == 0)
        //        {
        //            var errorResponse = new
        //            {
        //                IsSuccess = false,
        //                Message = "Failed to update the tag. No changes were made."
        //            };
        //            return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //        }

        //        var successResponse = new
        //        {
        //            IsSuccess = true,
        //            Message = "Tag successfully updated.",
        //            Tag = new
        //            {
        //                NameEn = tagName,
        //                NameSp = updateTagData.RootElement.GetProperty("name_sp").GetString(),
        //                Web = updateTagData.RootElement.GetProperty("web").GetString()
        //            }
        //        };
        //        return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
        //    }
        //    catch (Exception ex)
        //    {
        //        var errorResponse = new
        //        {
        //            IsSuccess = false,
        //            Message = $"An error occurred: {ex.Message}"
        //        };
        //        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
        //    }
        //}

        public async Task<JsonDocument> UpdateTagAsync(string typeName, string tagName, JsonDocument updateTagData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(tagName))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Type name and tag name cannot be null or empty."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                if (updateTagData == null || !updateTagData.RootElement.TryGetProperty("name_en", out _) ||
                    !updateTagData.RootElement.TryGetProperty("name_sp", out _) ||
                    !updateTagData.RootElement.TryGetProperty("web", out _))
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Invalid tag data. Required fields: name_en, name_sp, web."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                string newTagName = updateTagData.RootElement.GetProperty("name_en").GetString();

                // Проверка существования тега внутри своего типа
                var existingTagFilter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("name_en", typeName),
                    Builders<BsonDocument>.Filter.AnyEq("tags.name_en", tagName)
                );

                var tagExists = await _tagsCollection.Find(existingTagFilter).FirstOrDefaultAsync();
                if (tagExists == null)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = $"Tag '{tagName}' does not exist in type '{typeName}'."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                // Проверка, что новое имя тега не конфликтует с существующими тегами (если имя меняется)
                if (tagName != newTagName)
                {
                    // Проверка уникальности в рамках текущего типа
                    var sameTypeConflictFilter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("name_en", typeName),
                        Builders<BsonDocument>.Filter.AnyEq("tags.name_en", newTagName)
                    );

                    var sameTypeConflict = await _tagsCollection.Find(sameTypeConflictFilter).FirstOrDefaultAsync();
                    if (sameTypeConflict != null)
                    {
                        var errorResponse = new
                        {
                            IsSuccess = false,
                            Message = $"Tag '{newTagName}' already exists in type '{typeName}'."
                        };
                        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                    }

                    // Проверка глобальной уникальности
                    var globalConflictFilter = Builders<BsonDocument>.Filter.AnyEq("tags.name_en", newTagName);
                    var globalConflict = await _tagsCollection.Find(globalConflictFilter).FirstOrDefaultAsync();
                    if (globalConflict != null)
                    {
                        string conflictTypeName = globalConflict["name_en"].AsString;
                        var errorResponse = new
                        {
                            IsSuccess = false,
                            Message = $"Tag '{newTagName}' already exists in type '{conflictTypeName}'. Tags must be globally unique."
                        };
                        return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                    }
                }

                // Обновляем данные тега включая name_en
                var filter = Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("name_en", typeName),
                    Builders<BsonDocument>.Filter.AnyEq("tags.name_en", tagName)
                );

                var update = Builders<BsonDocument>.Update
                    .Set("tags.$[elem].name_en", newTagName)
                    .Set("tags.$[elem].name_sp", updateTagData.RootElement.GetProperty("name_sp").GetString())
                    .Set("tags.$[elem].web", updateTagData.RootElement.GetProperty("web").GetString());

                var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(
                new BsonDocument("elem.name_en", tagName))
        };

                var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
                var result = await _tagsCollection.UpdateOneAsync(filter, update, updateOptions);

                if (result.ModifiedCount == 0)
                {
                    var errorResponse = new
                    {
                        IsSuccess = false,
                        Message = "Failed to update the tag. No changes were made."
                    };
                    return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
                }

                var successResponse = new
                {
                    IsSuccess = true,
                    Message = "Tag successfully updated.",
                    Tag = new
                    {
                        OldNameEn = tagName,
                        NameEn = newTagName,
                        NameSp = updateTagData.RootElement.GetProperty("name_sp").GetString(),
                        Web = updateTagData.RootElement.GetProperty("web").GetString()
                    }
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(successResponse));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    IsSuccess = false,
                    Message = $"An error occurred: {ex.Message}"
                };
                return JsonDocument.Parse(JsonSerializer.Serialize(errorResponse));
            }
        }


    }
}
