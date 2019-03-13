# DeepPill Android image recognition neural network 
Android app and back-end image recognizer to identify speckled pills to stop counterfeiting and Opioid Abuse

Note: Opencv library should be added to the bin/running folder for making the Image match service working.

TruMedicine Project Structure

OpenCV
ImageMatchHost 
Source Url : $/TruMedicine/Source/TM.TruMedicine/OpenCVImageMatch
ImageMatchHost : This app is a standalone console application which runs a WCF service. This service exposes API to check the similarities between images. Our API consumes this service to give image match results. Opencv dlls should be included in the running folder. 
OpencvImageMatch
Source Url : $/TruMedicine/Source/TM.TruMedicine/OpenCVImageMatch
This project is a library to check Image similarity between images. This library is referred by 
ImageMatchHost.
We have used EmguCv .Net wrapper in order to access OpenCv functions. Only version 2 of EmguCv is compatible with our project. Avoid using version 3.
API
TM.Trumedicine.API
This is the API web project which hosts all API’s required for web app and mobile app. The html files and js files required for the web app is also hosted in this project.
TM.Trumedicine.Infrastructure
This project handles all helper classes required for the proper functioning of API. The templates for email notification, handling sessions etc included in this project.
TM.Trumedicine.ViewModels
Viewmodels required by Trumedicine API. Any data submit to  or receive from API is using this viewmodel formats.

Core
TM.Trumedicine.Core
All interfaces required for DAL, Data, Services included project.

Data
TM.Trumedicine.Data
DAL : All repositories
Data : Seeder, Entities
Migrations : All migrations done so far

TM.Trumedicine.Services
All services required for the API. These service classes are used in Api controllers.
All Owin providers included in this project.

Entities
TM.Trumedicine.Entities
Code first entity classes




