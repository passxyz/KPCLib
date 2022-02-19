# KPCLib, KeePassLib and PassXYZLib

This is a .NET standard 2.0 build of `KeePassLib` which is a library of [KeePass][1]. `KPCLib` 1.x.x is a single library built with `KeePassLib`. I used it as a library for [PassXYZ][2] which is a cross platform App based on [Xamarin.Forms][3].

In 2.x.x releases, I split it into three separate libraries.
- `KPCLib` - This is a .NET standard 2.0 library which generalizes `PwEntry` and `PwGroup` in `KeePassLib` so that we can design App easier for the navigation on mobile platform.
- `KeePassLib` - This is a .NET standard 2.0 port of the original KeePassLib.
- `PassXYZLib` - This is an extension of `KeePassLib` for .NET MAUI.

The below diagram shows the relationship of three libraries.
![image01](images/kpclib01.PNG)

Even though there are three libraries above, we have only two packages published in NuGet.
- [KPCLib][6] package includes both `KPCLib` and `KeePassLib`. I could not publish the .NET standard version of `KeePassLib` to NuGet, since [dlech][8] published a .NET Framework version of [KeePassLib][5] already. It is an outdated version 2.30.0 published on Sep 13, 2015.
- [PassXYZLib][7] package is a .NET MAUI library which can be used for .NET MAUI Apps.

[1]: https://keepass.info/
[2]: https://passxyz.github.io/
[3]: https://dotnet.microsoft.com/en-us/apps/xamarin/xamarin-forms
[4]: https://github.com/passxyz/KPCLib
[5]: https://www.nuget.org/packages/KeePassLib/
[6]: https://www.nuget.org/packages/KPCLib/
[7]: https://www.nuget.org/packages/PassXYZLib/
[8]: https://www.nuget.org/profiles/dlech
