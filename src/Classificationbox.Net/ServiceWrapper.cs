/*
 * Copyright 2021 thomas694 (@GH)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this project except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Classificationbox.Net.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Classificationbox.Net
{
    /// <summary>
    /// .Net wrapper around Machine Box's Classificationbox service/API.
    /// </summary>
    public class ServiceWrapper
    {
        private string _baseUrl;

        /// <summary>
        /// ServiceWrapper constructor
        /// </summary>
        /// <param name="baseUrl">root folder of dataset</param>
        public ServiceWrapper(string baseUrl)
        {
            if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Remove(baseUrl.Length - 1, 1);
            _baseUrl = baseUrl;
        }

        /// <summary>
        /// Creates a model with the given name and classification classes.
        /// </summary>
        /// <param name="modelName">name of the model to create</param>
        /// <param name="classes">list of classification classes</param>
        /// <returns></returns>
        public CreateModelResponse CreateModel(string modelName, IEnumerable<string> classes)
        {
            HttpClient client = new HttpClient();
            // id: 60ad2c0247c41223
            var classesString = "\"" + string.Join(",", classes).Replace(",", "\",\"") + "\"";
            var payload = string.Format("{{ \"name\": \"{0}\", \"classes\": [ {1} ] }}", modelName, classesString);
            HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_baseUrl + "/classificationbox/models"),
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json; charset=utf-8" }
                },
                Content = content
            };
            HttpResponseMessage rsp;
            try
            {
                rsp = client.SendAsync(request).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return new CreateModelResponse() { error = ex.Message };
            }
            var s = rsp.Content.ReadAsStringAsync().Result;
            var ret = JsonConvert.DeserializeObject<CreateModelResponse>(s);
            if (!ret.success)
                Debug.WriteLine($"Error creating model '{modelName}': {ret.error}");
            return ret;
        }

        /// <summary>
        /// Deletes the model with the given id.
        /// </summary>
        /// <param name="modelId">id of the model to delete</param>
        /// <returns>OK for success, otherwise error message</returns>
        public string DeleteModel(string modelId)
        {
            HttpClient client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri(string.Format("{0}/classificationbox/models/{1}", _baseUrl, modelId)),
                Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json; charset=utf-8" }
                }
            };
            HttpResponseMessage rsp;
            try
            {
                rsp = client.SendAsync(request).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return ex.Message;
            }
            if (rsp.IsSuccessStatusCode)
            {
                return "OK";
            }
            else
                return "";
        }

        /// <summary>
        /// Requests the teaching of an image in the given class of the specified model.
        /// </summary>
        /// <param name="model_id">id of the model to use</param>
        /// <param name="className">class this image belongs to</param>
        /// <param name="filename">filename of the image</param>
        /// <returns>OK for success, otherwise error message</returns>
        public string TeachModel(string model_id, string className, string filename)
        {
            Byte[] bytes = File.ReadAllBytes(filename);
            String base64 = Convert.ToBase64String(bytes);
            using (HttpClient client = new HttpClient())
            {
                var payload = string.Format("{{ \"class\": \"{0}\", \"inputs\": [ {{ \"key\": \"image\", \"type\": \"image_base64\", \"value\": \"{1}\"}} ] }}", className, base64);
                HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(string.Format("{0}/classificationbox/models/{1}/teach", _baseUrl, model_id)),
                    Headers = {
                    { HttpRequestHeader.Accept.ToString(), "application/json; charset=utf-8" }
                },
                    Content = content
                };
                HttpResponseMessage rsp;
                try
                {
                    rsp = client.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
                if (rsp.IsSuccessStatusCode)
                {
                    var s = rsp.Content.ReadAsStringAsync().Result;
                    dynamic json = JObject.Parse(s);
                    if (!json.success.Value) return json;
                    return "OK";
                }
                else
                {
                    var s = rsp.Content.ReadAsStringAsync().Result;
                    return s;
                }
            }
        }

        /// <summary>
        /// Requests the prediction of the given image file by the specified model.
        /// </summary>
        /// <param name="model_id">id of the model to use</param>
        /// <param name="filename">filename of the image</param>
        /// <returns></returns>
        public PredictionResponse Predict(string model_id, string filename)
        {
            Byte[] bytes = File.ReadAllBytes(filename);
            String base64 = Convert.ToBase64String(bytes);
            using (HttpClient client = new HttpClient())
            {
                var payload = string.Format("{{ \"limit\": 10, \"inputs\": [ {{ \"key\": \"image\", \"type\": \"image_base64\", \"value\": \"{0}\"}} ] }}", base64);
                HttpContent content = new StringContent(payload, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(string.Format("{0}/classificationbox/models/{1}/predict", _baseUrl, model_id)),
                    Headers = {
                        { HttpRequestHeader.Accept.ToString(), "application/json; charset=utf-8" }
                    },
                    Content = content
                };
                HttpResponseMessage rsp;
                try
                {
                    rsp = client.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return new PredictionResponse() { error = ex.Message };
                }
                var s = rsp.Content.ReadAsStringAsync().Result;
                var o = JsonConvert.DeserializeObject<PredictionResponse>(s);
                return o;
            }
        }

        /// <summary>
        /// Gets the statistics of the specified model.
        /// </summary>
        /// <param name="modelId">id of the model to return statistics for</param>
        /// <returns></returns>
        public ModelStatistics GetModelStatistics(string modelId)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(string.Format("{0}/classificationbox/models/{1}/stats", _baseUrl, modelId))
                };
                HttpResponseMessage rsp;
                try
                {
                    rsp = client.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return new ModelStatistics() { error = ex.Message };
                }
                var rspStr = rsp.Content.ReadAsStringAsync().Result;
                if (rsp.IsSuccessStatusCode)
                {
                    var ms = JsonConvert.DeserializeObject<ModelStatistics>(rspStr);
                    return ms;
                }
                else
                {
                    return new ModelStatistics()
                    {
                        success = false,
                        error = rspStr
                    };
                }
            }
        }

        /// <summary>
        /// Lists all the existing models.
        /// </summary>
        /// <returns></returns>
        public ListModelsResponse ListModels()
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(string.Format("{0}/classificationbox/models", _baseUrl))
                };
                HttpResponseMessage rsp;
                try
                {
                    rsp = client.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    var s = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                    Debug.WriteLine(s);
                    return new ListModelsResponse() { error = ex.Message };
                }
                var rspStr = rsp.Content.ReadAsStringAsync().Result;
                var obj = JsonConvert.DeserializeObject<ListModelsResponse>(rspStr);
                return obj;
            }
        }

        /// <summary>
        /// Gets the specified model.
        /// </summary>
        /// <param name="modelId">id of the model to return</param>
        /// <returns></returns>
        public GetModelResponse GetModel(string modelId)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(string.Format("{0}/classificationbox/models/{1}", _baseUrl, modelId))
                };
                HttpResponseMessage rsp;
                try
                {
                    rsp = client.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return new GetModelResponse() { error = ex.Message };
                }
                var s = rsp.Content.ReadAsStringAsync().Result;
                if (rsp.IsSuccessStatusCode)
                {
                    var model = JsonConvert.DeserializeObject<Model>(s);
                    return new GetModelResponse() { success = true, model = model };
                }
                else
                {
                    return new GetModelResponse() { error = s };
                }
            }
        }

        /// <summary>
        /// Loads the saved model from the specified file into the classificationbox
        /// </summary>
        /// <param name="filename">filename of the saved model</param>
        /// <returns>OK for success or error message</returns>
        public string LoadModel(string filename)
        {
            Byte[] bytes = File.ReadAllBytes(filename);
            String base64 = Convert.ToBase64String(bytes);
            using (HttpClient client = new HttpClient())
            {
                var byteContent = new ByteArrayContent(bytes);
                //var payload = string.Format("base64={0}", base64);
                //HttpContent content = new FormUrlEncodedContent(new KeyValuePair<string,string>("base64", byteContent.));
                var content = new MultipartFormDataContent();
                content.Add(byteContent, "file", filename);
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(string.Format("{0}/classificationbox/state", _baseUrl)),
                    Headers = {
                        { HttpRequestHeader.Accept.ToString(), "application/json; charset=utf-8" }
                    },
                    Content = content
                };
                //var payload = string.Format("base64={0}", base64);
                //HttpContent content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");
                //var request = new HttpRequestMessage
                //{
                //    Method = HttpMethod.Post,
                //    RequestUri = new Uri(string.Format("{0}/classificationbox/state", _baseUrl)),
                //    Headers = {
                //        { HttpRequestHeader.Accept.ToString(), "application/json; charset=utf-8" }
                //    },
                //    Content = content
                //};
                HttpResponseMessage rsp;
                try
                {
                    rsp = client.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
                if (rsp.IsSuccessStatusCode)
                {
                    var s = rsp.Content.ReadAsStringAsync().Result;
                    return "OK";
                }
                else
                {
                    var s = rsp.Content.ReadAsStringAsync().Result;
                    return s;
                }
            }
        }

        /// <summary>
        /// Saves the current model to the specified file
        /// </summary>
        /// <param name="modelId">id of the model to save</param>
        /// <param name="filename">filename where to save the model</param>
        /// <returns>OK for success or error message</returns>
        public string SaveModel(string modelId, string filename)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(string.Format("{0}/classificationbox/state/{1}", _baseUrl, modelId))
                };
                HttpResponseMessage rsp;
                try
                {
                    rsp = client.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
                if (rsp.IsSuccessStatusCode)
                {
                    if (File.Exists(filename)) File.Delete(filename);
                    using (Stream output = File.OpenWrite(filename))
                    using (Stream input = rsp.Content.ReadAsStreamAsync().Result)
                    {
                        input.CopyTo(output);
                    }
                    return "OK";
                }
                else
                {
                    var s = rsp.Content.ReadAsStringAsync().Result;
                    return s;
                }
            }
        }
    }
}
