using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace LabraryApi.Classes {
    public record UserDTO(
        string fio,
        string gender,
        string username
     ) {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id;
    };
}
