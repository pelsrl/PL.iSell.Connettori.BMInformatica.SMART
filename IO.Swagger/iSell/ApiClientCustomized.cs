using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using IO.Swagger.Model;
using Newtonsoft.Json;
using RestSharp;

namespace IO.Swagger.Client
{
    public partial class ApiClient
    {
        public const string NULL_VALUE = "7E37277A79A94CA899808B1B06B1E0E0BE03F0DA5A0A4CAC932D2CC481E208CC";

        partial void InterceptRequest(RestRequest request)
        {
            var parametriDaRimuovere = new List<Parameter>();
            foreach (var param in request.Parameters)
            {
                if (param.Value is string paramString && paramString.Equals(NULL_VALUE, StringComparison.OrdinalIgnoreCase))
                {
                    parametriDaRimuovere.Add(param);
                }
            }

            foreach (var param in parametriDaRimuovere)
            {
                request.RemoveParameter(param);
            }

            System.Diagnostics.Debugger.Break();
        }

        partial void InterceptResponse(RestRequest request, RestResponse response)
        {
            var dizionarioHeader = new Dictionary<string, List<string>>();
            foreach (var header in response.Headers)
            {
                if (!dizionarioHeader.ContainsKey(header.Name))
                    dizionarioHeader[header.Name] = new List<string>();
                dizionarioHeader[header.Name].Add(header.Value);
            }

            var lista = new List<HeaderParameter>();
            foreach (var headerConValori in dizionarioHeader)
            {
                lista.Add(new HeaderParameter(headerConValori.Key, string.Join(",", headerConValori.Value)));
            }

            response.Headers = new ReadOnlyCollection<HeaderParameter>(lista);
        }

        public static T DeserializeObjectFromJson<T>(string stringaJson)
        {
            return JsonConvert.DeserializeObject<T>(stringaJson, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            });
        }

        public static string SerializeObjectToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}