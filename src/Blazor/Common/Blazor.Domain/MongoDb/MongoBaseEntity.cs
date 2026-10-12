using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Blazor.Domain.MongoDb;

public abstract class MongoBaseEntity
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedDate { get; set; }
}
