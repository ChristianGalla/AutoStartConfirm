using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Text;

namespace AutoStartConfirm.Helpers
{
    public class JsonNetSerializer : NLog.IJsonConverter
    {
        private readonly DefaultContractResolver contractResolver;

        public JsonNetSerializer()
        {
            contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
        }

        /// <summary>Serialization of an object into JSON format.</summary>
        /// <param name="value">The object to serialize to JSON.</param>
        /// <param name="builder">Output destination.</param>
        /// <returns>Serialize succeeded (true/false)</returns>
        public bool SerializeObject(object value, StringBuilder builder)
        {
            try
            {
                string json = JsonConvert.SerializeObject(value, new JsonSerializerSettings
                {
                    ContractResolver = contractResolver
                });

                builder.Append(json);
            }
            catch (Exception e)
            {
                NLog.Common.InternalLogger.Error(e, "Error when custom JSON serialization");
                return false;
            }
            return true;
        }
    }
}
