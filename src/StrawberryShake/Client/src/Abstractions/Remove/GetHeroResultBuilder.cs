using System;
using System.Collections.Generic;
using System.Text.Json;

namespace StrawberryShake.Remove
{
    public class GetHeroResultBuilder : IOperationResultBuilder<JsonDocument, GetHeroResult>
    {
        private readonly IEntityStore _entityStore;
        private readonly Func<JsonElement, EntityId> _extractId;
        private readonly IOperationResultDataFactory<GetHeroResult> _resultDataFactory;
        private readonly IValueSerializer<string, string> _stringSerializer;

        public GetHeroResultBuilder(
            IEntityStore entityStore,
            Func<JsonElement, EntityId> extractId,
            IOperationResultDataFactory<GetHeroResult> resultDataFactory,
            IValueSerializerResolver valueSerializerResolver)
        {
            _entityStore = entityStore;
            _extractId = extractId;
            _resultDataFactory = resultDataFactory;
            _stringSerializer = valueSerializerResolver.GetValueSerializer<string, string>("String");
        }

        public IOperationResult<GetHeroResult> Build(Response<JsonDocument> response)
        {
            (GetHeroResult Result, GetHeroResultInfo Info)? data = null;

            if (response.Body is not null &&
                response.Body.RootElement.TryGetProperty("data", out JsonElement obj))
            {
                data = BuildData(obj);
            }

            return new OperationResult<GetHeroResult>(
                data?.Result,
                data?.Info,
                _resultDataFactory,
                null);
        }

        private (GetHeroResult, GetHeroResultInfo) BuildData(JsonElement obj)
        {
            using (_entityStore.BeginUpdate())
            {
                var entityIds = new HashSet<EntityId>();

                // store updates ...
                EntityId heroId = UpdateHeroEntity(obj.GetProperty("hero"), entityIds);

                // build result
                var resultInfo = new GetHeroResultInfo(
                    heroId,
                    DeserializeNonNullString(obj, "version"),
                    entityIds);

                return (_resultDataFactory.Create(resultInfo), resultInfo);
            }
        }

        private EntityId UpdateHeroEntity(JsonElement obj, ISet<EntityId> entityIds)
        {
            EntityId entityId = _extractId(obj);
            entityIds.Add(entityId);

            if (entityId.Name.Equals("Human", StringComparison.Ordinal))
            {
                HumanEntity entity = _entityStore.GetOrCreate<HumanEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");

                var friends = new List<EntityId>();

                foreach (JsonElement child in obj.GetProperty("friends").EnumerateArray())
                {
                    friends.Add(UpdateFriendEntity(child, entityIds));
                }

                entity.Friends = friends;
            }

            if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
            {
                DroidEntity entity = _entityStore.GetOrCreate<DroidEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");

                var friends = new List<EntityId>();

                foreach (JsonElement child in obj.GetProperty("friends").EnumerateArray())
                {
                    friends.Add(UpdateFriendEntity(child, entityIds));
                }

                entity.Friends = friends;
            }

            throw new NotSupportedException();
        }

        private EntityId UpdateFriendEntity(JsonElement obj, ISet<EntityId> entityIds)
        {
            EntityId entityId = _extractId(obj);
            entityIds.Add(entityId);

            if (entityId.Name.Equals("Human", StringComparison.Ordinal))
            {
                HumanEntity entity = _entityStore.GetOrCreate<HumanEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");
            }

            if (entityId.Name.Equals("Droid", StringComparison.Ordinal))
            {
                DroidEntity entity = _entityStore.GetOrCreate<DroidEntity>(entityId);
                entity.Name = DeserializeNonNullString(obj, "name");
            }

            throw new NotSupportedException();
        }

        private string DeserializeNonNullString(JsonElement obj, string propertyName)
        {
            if (obj.TryGetProperty(propertyName, out JsonElement property) &&
                property.ValueKind != JsonValueKind.Null)
            {
                _stringSerializer.Deserialize(property.GetString());
            }

            throw new InvalidOperationException();
        }
    }
}