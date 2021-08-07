# KPCLib - KeePass Portable Class Library

This is a .NET Standard build of KeePassLib which is a library of KeePass. With **KPCLib** and Xamarin.Forms, we can build KeePass based applications on all major platforms. A command line application [KPCLibPy][1] is built using **KPCLib** and [Python.NET][2].

**KPCLib** included two components:
- **KeePassLib** - This is the port of the original KeePassLib under project `KPCLib`.
- **PassXYZLib** - This is the enhancement built on top of KeePassLib, such as localization, OTP etc.

To be compatiable with Xamarin.Forms, the current build is an .Net Standard 2.0 library. It can also be used for .NET 5 or .NET 6 applications.

### Setup
* Available on NuGet: [![NuGet](https://img.shields.io/nuget/v/Xam.Plugin.Media.svg?label=NuGet)](https://www.nuget.org/packages/KPCLib)
* Build status: [![Build status](https://ci.appveyor.com/api/projects/status/4py18evnh0xxxvi1?svg=true)](https://ci.appveyor.com/project/shugaoye/kpclib-bccwi)
* [Branch strategy](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)
* Install into your PCL/.NET Standard project and Client projects.


[1]: https://github.com/passxyz/KPCLibPy
[2]: https://github.com/pythonnet/pythonnet