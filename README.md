# Autonomous Learning for Facial Identification
ALFI is a facial identification application that uses the Kinect natural interface and machine learning techniques.

It is develop as a student project, to test the efficiency and practicality of a new pipeline for machine learning applications, called Autonomous Learning. This approach seeks to facilitate the acquisition and preprocessing of data, crowdsourcing it through the use of natural interfaces like the Kinect.
___
## Installation
### Prerequisites
- [Python 3.6](https://www.python.org/downloads/release/python-367/)
- [.NET Framework 4.6.1](https://dotnet.microsoft.com/download)
- Visual Studio 2017 (Optional)
### Download and Setup
1. Download the latest ALFI release from this [link](https://github.com/sebastian-mc/ALFI/releases).
2. Unzip the file. You should be able to see three folders.
```
.
+-- ALFI_Data
+-- FaceID
+-- IdentificationApp
```
3. Copy the `ALFI_Data` folder to the root of the drive where you unzip the folder. For example, if you download and unzip the project in `C:\Users\Documents`, then copy `ALFI_Data` to the root of the C drive, i.e. `C:\`.
### Python Environment
1. Open a command prompt inside the `FaceID` folder.
2. Download the pipenv package manager by executing the following command:
```
$ python -m pip install pipenv
```
3. Install the project prerequisites through pipenv:
```
$ pipenv install
```
![pipenv install process](https://raw.githubusercontent.com/sebastian-mc/ALFI/master/Docs/ALFI_Install_1.gif)

(This process may take several minutes)

4. Start the FaceID server:
```
$ pipenv run python App.py
```
You should see the console display the following:

![Running the server](https://raw.githubusercontent.com/sebastian-mc/ALFI/master/Docs/ALFI_Install_2.png)

The FaceID server is ready.
### Kinect Setup
Due to the way the existing images in the dataset were taken, the Kinect needs to be place in a specific way in order for the app to work optimally.
- The sensor must be on the bottom of the screen, pointing slightly upwards.
- If the users will use the app standing up, the sensor must be place at a height around 100-120 cm from the floor. Otherwise, if the app is to be used sitting down, the sensor must be around 50-70 cm from the floor.
- Preferably, the sensor should be align to the center of the screen horizontally.
### Identification App
Run the `IdentificationApp.exe` executable, found on the `IdentificationApp` folder on the unzip project.

You should see the Kinect sensor turning on, and standing in front of it should prompt the screen to change from blue to orange.
___
