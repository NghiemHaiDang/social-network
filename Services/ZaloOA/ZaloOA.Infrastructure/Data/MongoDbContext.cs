using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using ZaloOA.Domain.Common;
using ZaloOA.Domain.Entities;

namespace ZaloOA.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public IMongoCollection<ZaloOAAccount> ZaloOAAccounts => _database.GetCollection<ZaloOAAccount>("ZaloOAAccounts");
    public IMongoCollection<ZaloUser> ZaloUsers => _database.GetCollection<ZaloUser>("ZaloUsers");
    public IMongoCollection<ZaloConversation> ZaloConversations => _database.GetCollection<ZaloConversation>("ZaloConversations");
    public IMongoCollection<ZaloMessage> ZaloMessages => _database.GetCollection<ZaloMessage>("ZaloMessages");

    static MongoDbContext()
    {
        // Configure conventions
        var conventionPack = new ConventionPack
        {
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.Int32)
        };
        ConventionRegistry.Register("ZaloOAConventions", conventionPack, _ => true);

        // Register class maps for entities with private setters
        RegisterClassMaps();
    }

    public MongoDbContext(IMongoDatabase database)
    {
        _database = database;
        CreateIndexes();
    }

    private static void RegisterClassMaps()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
        {
            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(x => x.Id)
                    .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
                cm.MapMember(x => x.CreatedAt);
                cm.MapMember(x => x.UpdatedAt);
                cm.SetIsRootClass(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ZaloOAAccount)))
        {
            BsonClassMap.RegisterClassMap<ZaloOAAccount>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ZaloUser)))
        {
            BsonClassMap.RegisterClassMap<ZaloUser>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ZaloConversation)))
        {
            BsonClassMap.RegisterClassMap<ZaloConversation>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                // Ignore EF Core navigation properties
                cm.UnmapMember(x => x.OAAccount);
                cm.UnmapMember(x => x.ZaloUser);
                cm.UnmapMember(x => x.Messages);
                cm.MapMember(x => x.OAAccountId)
                    .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
                cm.MapMember(x => x.ZaloUserId)
                    .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ZaloMessage)))
        {
            BsonClassMap.RegisterClassMap<ZaloMessage>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                // Ignore EF Core navigation property
                cm.UnmapMember(x => x.Conversation);
                cm.MapMember(x => x.ConversationId)
                    .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
            });
        }
    }

    private void CreateIndexes()
    {
        // ZaloOAAccounts indexes
        ZaloOAAccounts.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ZaloOAAccount>(
                Builders<ZaloOAAccount>.IndexKeys.Ascending(x => x.UserId)),
            new CreateIndexModel<ZaloOAAccount>(
                Builders<ZaloOAAccount>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Ascending(x => x.OAId),
                new CreateIndexOptions { Unique = true })
        });

        // ZaloUsers indexes
        ZaloUsers.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ZaloUser>(
                Builders<ZaloUser>.IndexKeys
                    .Ascending(x => x.ZaloUserId)
                    .Ascending(x => x.OAId),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<ZaloUser>(
                Builders<ZaloUser>.IndexKeys.Ascending(x => x.OAId))
        });

        // ZaloConversations indexes
        ZaloConversations.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ZaloConversation>(
                Builders<ZaloConversation>.IndexKeys
                    .Ascending(x => x.OAAccountId)
                    .Ascending(x => x.ZaloUserId),
                new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<ZaloConversation>(
                Builders<ZaloConversation>.IndexKeys.Ascending(x => x.OAAccountId)),
            new CreateIndexModel<ZaloConversation>(
                Builders<ZaloConversation>.IndexKeys.Descending(x => x.LastMessageAt))
        });

        // ZaloMessages indexes
        ZaloMessages.Indexes.CreateMany(new[]
        {
            new CreateIndexModel<ZaloMessage>(
                Builders<ZaloMessage>.IndexKeys.Ascending(x => x.ConversationId)),
            new CreateIndexModel<ZaloMessage>(
                Builders<ZaloMessage>.IndexKeys.Ascending(x => x.ZaloMessageId)),
            new CreateIndexModel<ZaloMessage>(
                Builders<ZaloMessage>.IndexKeys.Ascending(x => x.SentAt))
        });
    }

    public IMongoCollection<T> GetCollection<T>() where T : BaseEntity
    {
        var collectionName = typeof(T).Name switch
        {
            nameof(ZaloOAAccount) => "ZaloOAAccounts",
            nameof(ZaloUser) => "ZaloUsers",
            nameof(ZaloConversation) => "ZaloConversations",
            nameof(ZaloMessage) => "ZaloMessages",
            _ => typeof(T).Name + "s"
        };
        return _database.GetCollection<T>(collectionName);
    }
}
