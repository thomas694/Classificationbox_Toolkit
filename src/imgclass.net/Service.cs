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
using System.IO;
using System.Linq;
using System.Threading;
using Classificationbox.Net;
using Classificationbox.Net.Models;
using Newtonsoft.Json;

namespace ImgClass.Net
{
    class Service
    {
        protected class FileEntry
        {
            public string fn { get; set; }
            public int st { get; set; }
            public string pc { get; set; }
        }

        private Dictionary<string, List<FileEntry>> _files = new Dictionary<string, List<FileEntry>>();
        private Dictionary<string, List<int>> _train = new Dictionary<string, List<int>>();
        private ServiceWrapper svc;
        private string _baseUrl;
        private string _modelName;
        private string _modelId;
        private string _basePath;
        private double _ratio;
        private int _passes;
        private string _imagesPath;
        private double _threshold;
        private StreamWriter Log;

        public Service(string baseUrl, string modelName, string basePath)
        {
            _baseUrl = baseUrl;
            _modelName = modelName;
            _basePath = basePath;
            svc = new ServiceWrapper(baseUrl);
            Log = new StreamWriter(new FileStream(Path.Combine(basePath, string.Format( "imgclass.net_{0}.log", DateTime.Now.ToString("yyyyMMddHHmmss"))), FileMode.Create));
            Log.AutoFlush = true;
        }

        ~Service()
        {
            Log.Close();
            Log.Dispose();
        }

        private void MyLog(string msg)
        {
            Log.WriteLine(msg);
            Console.WriteLine(msg);
        }

        public void TrainModel(double ratio, int passes)
        {
            _ratio = ratio;
            _passes = passes;

            var ret = svc.ListModels();
            var list = ret.models;
            if (list == null) Environment.Exit((int)EXIT_CODES.ERR_NO_CB);
            if (!list.Any(x => x.name == _modelName))
            {
                MyLog("No models found, trying to load saved model...");
                if (File.Exists(Path.Combine(_basePath, _modelName + ".machinebox.classificationbox")))
                {
                    svc.LoadModel(Path.Combine(_basePath, _modelName + ".machinebox.classificationbox"));
                    ret = svc.ListModels();
                    list = ret.models;
                    if (list == null || !list.Any(x => x.name == _modelName))
                    {
                        MyLog($"Model '{_modelName}' couldn't be loaded, exiting...");
                        Environment.Exit((int)EXIT_CODES.ERR_LOADING_MODEL);
                    }
                }
                else
                {
                    MyLog($"Model '{_modelName}' couldn't be found, creating new one...");
                    var classes = Directory.GetDirectories(_basePath);
                    for (int i = 0; i < classes.Length; i++)
                    {
                        classes[i] = Path.GetFileName(classes[i]);
                    }
                    var res = svc.CreateModel(_modelName, new List<string>(classes));
                    if (!res.success)
                    {
                        Console.Error.WriteLine($"Error creating model '{_modelName}': {res.error}");
                        Environment.Exit((int)EXIT_CODES.ERR_CREATING_MODEL);
                    }
                    var rsp = svc.GetModel(res.id);
                    list = new List<Model>();
                    list.Add(rsp.model);
                }
            }
            _modelId = list.Find(x => x.name == _modelName)?.id;

            var stat = svc.GetModelStatistics(_modelId);
            var cs = "";
            foreach (var cls in stat.classes)
            {
                cs += $"  name: {cls.name}\texamples: {cls.examples}\n";
            }
            if (stat.examples != 0 || stat.predictions != 0)
                MyLog($"Model statistics:\n" +
                    $"predictions: {stat.predictions}\n" +
                    $"examples: {stat.examples}\n" +
                    $"classes:\n" + cs);

            LoadState(Path.Combine(_basePath, _modelName + "_files.db"));

            LoadFilesAndBuildGroups(_basePath, _ratio);

            for (int p = 0; p < _passes; p++)
            {
                MyLog($"Training pass {p + 1}/{_passes}");
                TrainModel(_modelId);
                Thread.Sleep(5000);
                PredictFiles(_modelId);
            }

            MyLog("Saving model...");
            svc.SaveModel(_modelId, Path.Combine(_basePath, _modelName + ".machinebox.classificationbox"));
            SaveState(Path.Combine(_basePath, _modelName + "_files.db"));
        }

        public void ClassifyImages(string imagesPath, double threshold)
        {
            _imagesPath = imagesPath;
            _threshold = threshold;

            var ret = svc.ListModels();
            var list = ret.models;
            if (list == null) Environment.Exit((int)EXIT_CODES.ERR_NO_CB);
            if (!list.Any(x => x.name == _modelName))
            {
                MyLog("No models found, trying to load saved model...");
                if (File.Exists(Path.Combine(_basePath, _modelName + ".machinebox.classificationbox")))
                {
                    svc.LoadModel(Path.Combine(_basePath, _modelName + ".machinebox.classificationbox"));
                    ret = svc.ListModels();
                    list = ret.models;
                    if (list == null || !list.Any(x => x.name == _modelName))
                    {
                        MyLog($"Model '{_modelName}' couldn't be loaded, exiting...");
                        Environment.Exit((int)EXIT_CODES.ERR_LOADING_MODEL);
                    }
                }
                else
                {
                    MyLog($"Saved model '{_modelName}' couldn't be found, exiting...");
                    Environment.Exit((int)EXIT_CODES.ERR_SAVED_MODEL_NOT_FOUND);
                }
            }
            _modelId = list.Find(x => x.name == _modelName)?.id;
            var rsp = svc.GetModel(_modelId);
            if (!rsp.success)
            {
                MyLog("Couldn't get model, exiting...");
                Environment.Exit((int)EXIT_CODES.ERR_LOADING_MODEL);
            }

            var files = new List<FileEntry>();
            files = LoadFiles(_imagesPath);
            CreateDirectories(rsp.model);
            SortImages(files);

            MyLog("Saving model...");
            svc.SaveModel(_modelId, Path.Combine(_basePath, _modelName + ".machinebox.classificationbox"));
            SaveState(files, Path.Combine(_basePath, DateTime.Now.ToString("yyyyMMddHHmmss") + "_sortedfiles.db"));
        }

        protected List<FileEntry> LoadFiles(string basePath)
        {
            MyLog("Reading files...");
            var ignoredExts = new List<string>() { ".mp4", ".mp3", ".json" };
            var list = new List<FileEntry>();
            foreach (var filename in Directory.GetFiles(basePath))
            {
                if (ignoredExts.Contains(Path.GetExtension(filename).ToLower())) continue;

                list.Add(new FileEntry() { fn = filename });
            }
            return list;
        }

        protected void CreateDirectories(Model model)
        {
            foreach (var cn in model.classes)
            {
                Directory.CreateDirectory(Path.Combine(_imagesPath, cn));
                Directory.CreateDirectory(Path.Combine(_imagesPath, cn + "_check"));
            }
        }

        protected void SortImages(List<FileEntry> files)
        {
            MyLog("Sorting images...");
            var max = files.Count;
            for (int i = 0; i < max; i++)
            {
                var entry = files[i];
                var pd = svc.Predict(_modelId, entry.fn);
                if (pd != null && pd.success)
                {
                    MyLog(string.Format("{0}/{1}: {2}/{3:0.00} {4}", i + 1, max, pd.classes[0].id, pd.classes[0].score, entry.fn));
                    string fn_new;
                    if (pd.classes[0].score >= _threshold)
                    {
                        fn_new = Path.Combine(Path.GetDirectoryName(entry.fn), pd.classes[0].id, Path.GetFileName(entry.fn));
                    }
                    else
                    {
                        fn_new = Path.Combine(Path.GetDirectoryName(entry.fn), pd.classes[0].id + "_check", Path.GetFileName(entry.fn));
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(fn_new));
                    File.Move(entry.fn, fn_new);
                }
            }
        }

        protected void LoadFilesAndBuildGroups(string basePath, double ratio)
        {
            MyLog("Cleaning orphaned file entries...");
            foreach (var key in _files.Keys)
            {
                int orphanCount = 0;
                for (var i = _files[key].Count - 1; i >= 0; i--)
                {
                    if (!File.Exists(_files[key][i].fn))
                    {
                        _files[key].RemoveAt(i);
                        orphanCount++;
                    }
                }
                if (orphanCount > 0) MyLog($"{key}: removed {orphanCount} orphaned entries");
            }

            MyLog("Reading new files...");
            foreach (var path in Directory.GetDirectories(basePath))
            {
                var className = Path.GetFileName(path);
                List<FileEntry> list;
                if (_files.ContainsKey(className))
                    list = _files[className];
                else
                    list = new List<FileEntry>();
                foreach (var item in Directory.GetFiles(path))
                {
                    if (!list.Any(x => x.fn == item))
                        list.Add(new FileEntry() { fn = item, st = 0 });
                }
                if (_files.ContainsKey(className))
                    _files[className] = list;
                else
                    _files.Add(className, list);
            }

            MyLog("Building training and verification groups...");
            foreach (var className in _files.Keys)
            {
                var list = _files[className];
                int max = list.Count;
                int solve = (int)(list.Count * ratio);
                var random = new Random((int)DateTime.Now.Ticks);
                var train = new List<int>();
                for (int i = 0; i < max; i++)
                {
                    if (list[i].st > 0)
                        train.Add(i);
                }
                int already = train.Count;
                for (int i = already; i < solve; i++)
                {
                    int n;
                    do
                    {
                        n = random.Next(max);
                    } while (list[n].st > 0);
                    FileEntry entry = list[n];
                    train.Add(n);
                    list[n] = new FileEntry() { fn = entry.fn, st = 1 };
                }
                _train.Add(className, train);
            }
        }

        private static void shuffle(int[] array)
        {
            Random rng = new Random();
            int n = array.Count();
            while (n > 1)
            {
                int k = rng.Next(n);
                n--;
                int temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        protected void TrainModel(string modelId)
        {
            MyLog("Training model...");
            Dictionary<string, List<int>> tmpLists = new Dictionary<string, List<int>>();
            foreach (var key in _train.Keys)
            {
                var arr = _train[key].ToArray();
                shuffle(arr);
                tmpLists.Add(key, new List<int>(arr));
            }
            bool cont;
            var x = 1;
            do
            {
                cont = false;
                foreach (var className in tmpLists.Keys)
                {
                    var trainList = tmpLists[className];
                    if (trainList.Count > 0)
                    {
                        cont = true;
                        var fileList = _files[className];
                        var entry = fileList[trainList[0]];
                        svc.TeachModel(modelId, className, entry.fn);
                        fileList[trainList[0]] = new FileEntry() { fn = entry.fn, st = 2 };
                        trainList.RemoveAt(0);
                        MyLog(string.Format("training {0}: {1}", x++, entry.fn));
                    }
                }
            } while (cont);
        }

        protected void PredictFiles(string modelId)
        {
            MyLog("Predicting classes...");
            foreach (var className  in _files.Keys)
            {
                var list = _files[className];
                var n = 1;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].st > 0) continue;

                    FileEntry entry = list[i];
                    var pd = svc.Predict(modelId, entry.fn);
                    if (pd != null && pd.success)
                        MyLog(string.Format("p{0}: {1} {2}/{3:0.00} {4}", n++, (pd.classes[0].id == className), pd.classes[0].id, pd.classes[0].score, entry.fn));
                }
            }
        }

        protected void SaveState(string filename)
        {
            var s = JsonConvert.SerializeObject(_files);
            File.WriteAllText(filename, s);
            MyLog("Current state has been saved to file.");
        }

        protected void SaveState(List<FileEntry> files, string filename)
        {
            var s = JsonConvert.SerializeObject(files);
            File.WriteAllText(filename, s);
            MyLog("File list of sorted images has been saved to file.");
        }

        protected void LoadState(string filename)
        {
            if (File.Exists(filename))
            {
                var s = File.ReadAllText(filename);
                _files = JsonConvert.DeserializeObject<Dictionary<string, List<FileEntry>>>(s);
            }
            else
                MyLog("Old state file not found, starting fresh...");
        }
    }
}
