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
using System.Globalization;
using System.IO;

namespace ImgClass.Net
{
    class Program
    {
        // debug (train): -cb http://192.168.176.1:8080 -model classify_pics -src "C:\Teach" -teachratio 0.8 -passes 5
        // debug (sort): -cb http://192.168.176.1:8080 -model classify_pics -src "C:\Teach" -classify C:\Images -threshold 0.95

        static void Main(string[] args)
        {
            string cb = string.Empty;
            string model = string.Empty;
            string basePath = string.Empty;
            double ratio = 0.0;
            int passes = 0;
            string imagesPath = string.Empty;
            double threshold = 0.0;

            if (args.Length == 0)
            {
                Console.WriteLine("Usage: imgtool.net -cb http://localhost:8080 -src . -teachratio 0.8 -passes 1");
                Console.WriteLine("Parameters: -cb      Classificationbox address");
                Console.WriteLine("            -model   Model name");
                Console.WriteLine("            -src     root folder of dataset");
                Console.WriteLine("            -teachratio  ratio of teach/validation images (e.g. 0.8)");
                Console.WriteLine("            -passes  number of times to teach the example images");
                Console.WriteLine("            -classify    folder of images to classify");
                Console.WriteLine("            -threshold   level an image is considered classified correctly (e.g. 0.95)");
                Console.WriteLine("Example usage: imgtool.net -cb http://localhost:8080 -model abc -src . -trainratio 0.8 -passes 5");
                Console.WriteLine("               imgtool.net -cb http://localhost:8080 -model abc -src . -classify C:\\ImageFolder -threshold 0.90");
                Console.WriteLine("Notes: ");
                Environment.Exit((int)EXIT_CODES.ERR_NO_PARAMETERS);
            }
            else
            {
                try
                {
                    int i = 0;
                    int check = 0;
                    do
                    {
                        switch (args[i])
                        {
                            case "-cb":
                                cb = args[++i];
                                check += 1;
                                break;
                            case "-model":
                                model = args[++i];
                                check += 2;
                                break;
                            case "-src":
                                basePath = args[++i];
                                basePath = Path.GetFullPath(basePath);
                                check += 4;
                                break;
                            case "-trainratio":
                                ratio = double.Parse(args[++i], CultureInfo.InvariantCulture);
                                check += 8;
                                break;
                            case "-passes":
                                passes = int.Parse(args[++i]);
                                check += 16;
                                break;
                            case "-classify":
                                imagesPath = args[++i];
                                imagesPath = Path.GetFullPath(imagesPath);
                                check += 32;
                                break;
                            case "-threshold":
                                threshold = double.Parse(args[++i], CultureInfo.InvariantCulture);
                                check += 64;
                                break;
                            default:
                                Console.Error.WriteLine($"Unknown parameter '{args[i]}' specified!");
                                Environment.Exit((int)EXIT_CODES.ERR_UNKNOWN_PARAMETER);
                                break;
                        }
                        i++;
                    } while (i < args.Length);
                    if ((check & 31) != 31 && (check & 103) != 103)
                        Environment.Exit((int)EXIT_CODES.ERR_NOT_ALL_PARAMETERS);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error while parsing the command line arguments: {ex}");
                    Environment.Exit((int)EXIT_CODES.ERR_PARAMETER_PARSE);
                }
            }

            Service mb = new Service(cb, model, basePath);
            if (imagesPath != "")
                mb.ClassifyImages(imagesPath, threshold);
            else
                mb.TrainModel(ratio, passes);
            
            Environment.Exit((int)EXIT_CODES.SUCCESS);
        }
    }
}
