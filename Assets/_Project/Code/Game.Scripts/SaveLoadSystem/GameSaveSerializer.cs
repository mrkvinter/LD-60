using System.IO;
using System.Text;
using Game.Scripts.SaveLoadSystem.Convertors;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Game.Scripts.SaveLoadSystem
{
    public sealed class GameSaveSerializer : ISaveSerializer
    {
        private const int Precision = 100;

        private readonly JsonSerializer serializer;

        public GameSaveSerializer()
        {
            serializer = JsonSerializer.Create(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            serializer.Converters.Add(new Vector2Convertor(Precision));
            serializer.Converters.Add(new Vector3Convertor(Precision));
            serializer.Converters.Add(new QuaternionConvertor(Precision));
            serializer.Converters.Add(new PoseConvertor(Precision));
            serializer.Converters.Add(new StringEnumConverter());
        }

        public byte[] Serialize<TData>(TData data)
        {
            using var stringWriter = new StringWriter();
            serializer.Serialize(stringWriter, data);
            return Encoding.UTF8.GetBytes(stringWriter.ToString());
        }

        public TData Deserialize<TData>(byte[] bytes)
        {
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8);
            using var jsonReader = new JsonTextReader(reader);
            return serializer.Deserialize<TData>(jsonReader);
        }
    }
}
