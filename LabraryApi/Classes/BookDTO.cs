using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Xml.Linq;

namespace LabraryApi.Classes {
    public record BookDTO(
        string name,
        string description,
        string author,
        int year,
        string publisher
   ) {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string id;
    };
}
