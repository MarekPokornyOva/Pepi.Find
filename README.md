# Pepi.Find
Pepi.Find is an open source content indexing server reachable with various clients (namely EpiServer).

Description:
This is set of extendable components to build functional indexing server/service hostable wherever you like e.g. locally during development even without internet connection.

Install:
There is no standard (user friendly) installer however this can be easily integrated to various applications based on target platform. It's easy to build an application (e.g. ASP.NET Core service, any .NET) which will handle the indexing and searching needs.

Projects description:
Pepi.Find.Server.Abstract - Definiton of all public interfaces to be implemented.
Pepi.Find.Server - Root functionality handling the indexing and searching HTTP requests.
Pepi.Find.SqlRepository - Repository storing indexed data in MS Sql Server database.
Pepi.Find.WebService - Sample ASP.NET core service hosting the server.
Pepi.Find.WebService.Util - Supporting class to simplify implementation in ASP.NET core application.
Pepi.Find.WinDesktop - Sample windows application hosting the server. It also displays HTTP communication with client so it is good while getting started.

Further ideas:
I'd like to make a custom client implementation and omit serialization+HTTP communication. It should be faster to search data directly from client system instead of call separate sevice. Both systems has pros and cons so why to don't have options? :-)

DB specialist wanted:
Current solution is working however I believe it's possible to optimize data structure and speed up the solution. If you are interested in or know some1 who could help, please get involved (try by yourself or contact me).
Maybe partial indexes and forcing index usage might help.
