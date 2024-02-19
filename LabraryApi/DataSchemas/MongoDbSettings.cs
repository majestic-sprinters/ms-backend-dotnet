namespace LabraryApi.DataSchemas {
    public class MongoDbSettings {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public static string ConnName => "MongoDB";
    }
}
