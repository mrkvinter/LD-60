using System;
using System.Globalization;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Scripts.SaveLoadSystem.Convertors
{
    public class PoseConvertor : JsonConverter<Pose>
    {
        private readonly int precision;

        public PoseConvertor(int precision = 100)
        {
            this.precision = precision;
        }

        public override void WriteJson(JsonWriter writer, Pose value, JsonSerializer serializer)
        {
            var p = value.position;
            var q = value.rotation;
            // q and -q are the same rotation; a positive w saves a minus sign.
            if (q.w < 0f) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);

            writer.WriteValue(string.Join("#", Round(p.x), Round(p.y), Round(p.z), Round(q.x), Round(q.y), Round(q.z), Round(q.w)));
        }

        public override Pose ReadJson(JsonReader reader, Type objectType, Pose existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            var split = ((string)reader.Value).Split('#');
            var position = new Vector3(Parse(split[0]), Parse(split[1]), Parse(split[2]));
            var rotation = Quaternion.Normalize(new Quaternion(Parse(split[3]), Parse(split[4]), Parse(split[5]), Parse(split[6])));
            return new Pose(position, rotation);
        }

        private string Round(float value) => Mathf.RoundToInt(value * precision).ToString(CultureInfo.InvariantCulture);

        private float Parse(string value) => int.Parse(value, CultureInfo.InvariantCulture) / (float)precision;
    }
}
