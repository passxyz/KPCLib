# KPCLib - KeePass Portable Class Library

This is a port of KeePassLib to PCL and .netstandard so that it can be used to build with Xamarin.Forms. With KPCLib and Xamarin.Forms, we can build KeePass based applications on all major platforms.


### Setup
* Available on NuGet: [![NuGet](https://img.shields.io/nuget/v/Xam.Plugin.Media.svg?label=NuGet)](https://www.nuget.org/packages/KPCLib)
* Build status: [![Build status](https://ci.appveyor.com/api/projects/status/ugxm1im7nsl634uy/branch/develop?svg=true)](https://ci.appveyor.com/project/shugaoye/kpclib/branch/develop)
* [Branch strategy](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)
* Install into your PCL/.NET Standard project and Client projects.

## Changes:

### Release 1.2.0
 - Added PassXYZLib
 - Support KPCLibPy

### Release 1.1.9
 - Removed dependency of Xamarin.Forms so it can be built with any .Netstandard apps
 - Using SkiaSharp to handle Bitmap which is supported by .Netstandard and .Net Core
 - Removed UWP test app and added .Net Core test app