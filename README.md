# Classificationbox Toolkit

You can use this toolkit to build your own image classification solution.<br/>
The toolkit consists of two projects:
- Classificationbox.Net - a .Net wrapper for Machine Box's Classificationbox service (.NET Standard 2.0 library)
- imgclass.net - a command line program using the Classificationbox.Net library to train your model and sort your images

The `imgclass.net` tool is a real-world command line program that shows how the `Classificationbox.Net` library can be 
used to train a model and later classify folders of images. It's a solution where you don't have to upload your images 
to an online service or use a tool which extracts "features" which are send to the service. The solution can be run 
disconnected from the internet. Also it is not only a example implementation which lacks features for a real life usage. 
It's used in the field and will be improved and extended with new features as the necessity arrises.

## Prerequisites

For using this toolkit you need the following (free) components:
- [Docker](https://www.docker.com/products/docker-desktop)<br/>
  You should run your docker daemon with at least 2 CPUs and 4GB RAM. The more CPUs you assign the faster is your training and classification process.
- [Machine Box's Classificationbox docker image](https://machinebox.io/docs/classificationbox)

## Usage

1. Prepare training images
1. Run Classificationbox
1. Train and test your model
1. Classify (sort) a folder of images

### Prepare training images

Create a directory structure that organizes the images into classes, with each folder as the class name:

```
/training-images
	/class1
		class1example1.jpg
		class1example2.jpg
		...
	/class2
		class2example1.jpg
		class2example2.jpg
		...
	/class3
		class3example1.jpg
		class3example2.jpg
		...
```

You should put roughly the same amount of images in every class folder.

### Run Classificationbox

In a terminal do:

```
docker run -p 8080:8080 -e "MB_KEY=$MB_KEY" machinebox/classificationbox
```

* Get yourself an `MB_KEY` from https://machinebox.io/account 

### Train and test your model

Use the `imgclass.net` tool to train the model:

```
imgclass.net -cb http://localhost:8080 -model Abc -src ./training-images -trainratio 0.8 -passes 3
```

The tool will post a random 80% (`-trainratio 0.8`) of the images to Classificationbox for training, and the
remaining images will be used to test the model. The process is repeated three times (`-passes 3`) to train 
the model better and make later predictions more accurate. You should use roughly the same amount of images 
for every class, so that the model doesn't get biased towards any class.

The tool logs information to the console and a log file in the src folder.
At the end the trained model and the state of the training files are automatically saved to the source folder. 
On the next run they will be loaded again.

### Classify (sort) a folder of images

After you have trained your model you can use `imgclass.net` tool to sort a folder of images

```
imgclass.net -cb http://localhost:8080 -model Abc -src ./training-images -classify C:\ImageFolder -threshold 0.95
```

The tool requests an image class prediction for each image and moves the file to a subfolder according to the 
predicted class and its accuracy. Related to the `training-images` example the images will be sorted into subfolders:
```
/ImageFolder
	/class1
	/class1_check
	/class2
	/class2_check
	/class3
	/class3_check
```

Images moved to `class1` have a accuracy of 95% (`-threshold 0.95`) or higher. Those moved to `class1_check` have 
a lower accuracy and need to be checked.

## Download

Latest versions can be found [here](https://github.com/thomas694/Classification_Toolkit/releases).

## Contributing to Classificationbox Toolkit

PRs and contributions of any kind are welcome!<br/>
Please open an issue and introduce your planned changes first, before starting major work.
