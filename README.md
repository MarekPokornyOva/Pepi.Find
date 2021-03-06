# Pepi.Find

[![Package Version](https://img.shields.io/nuget/v/Pepi.Find.Direct.svg)](https://www.nuget.org/packages/Pepi.Find.Direct/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Pepi.Find.Direct.svg)](https://www.nuget.org/packages/Pepi.Find.Direct/)
[![License](https://img.shields.io/github/license/MarekPokornyOva/Pepi.Find.svg)](https://github.com/MarekPokornyOva/Pepi.Find/blob/master/LICENSE)

Pepi.Find is an open source content indexing server reachable with various clients (namely EpiServer).

### Description:
This is set of extendable components to build functional indexing server/service hostable wherever you like e.g. locally during development even without internet connection.

### Projects description:
* Pepi.Find.Server.Abstract - Definiton of all public interfaces to be implemented.
* Pepi.Find.Server - Root functionality handling the indexing and searching HTTP requests.
* Pepi.Find.SqlRepository - Repository storing indexed data in MS Sql Server database.
* Pepi.Find.WebService - Sample ASP.NET core service hosting the server.
* Pepi.Find.WebService.Util - Supporting class to simplify implementation in ASP.NET core application.
* Pepi.Find.WinDesktop - Sample windows application hosting the server. It also displays HTTP communication with client so it is good while getting started.
* Pepi.Find.Direct - Possibility to host indexing service directly in EpiServer site - no external service needed. It also doesn't need JSON serialization+HTTP communication.
* Pepi.Find.Direct.Cms - Pepi.Find.Direct's extension for EpiServer CMS features.

### Install/Use:
There is no standard (user friendly) installer however this can be easily integrated to various applications based on target platform. It's easy to build an application (e.g. ASP.NET Core service, any .NET) which will handle the indexing and searching needs.
See [Documentation.md](./Documentation.md)

### DB specialist wanted:
Current solution is working however I believe it's possible to optimize data structure and speed up the solution. If you are interested in or know some1 who could help, please get involved (try by yourself or contact me).
Maybe partial indexes and forcing index usage might help.
