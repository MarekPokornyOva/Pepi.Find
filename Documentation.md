### Get started
First of all, you need to have a repository to store indexed data. All data is (generally spoken) maintained by IIndexRepository implemetation. Every such repository might use its own specific storage and data format.  
You may create your own repository or find and use already prepared one in this solution (Pepi.find.SqlRepository).

### Pepi.Find.SqlRepository
This repository uses MS Sql Server to store data. You may use any your instance you like or download and install fresh one. Repository works fine even with Express edition.  
To initialize DB structure see [InitializeDb.sql](./Pepi.Find.SqlRepository/InitializeDb.sql) and apply its script to your DB. Yes, it's as simple.  

### Client
There're two ways to establish communication between client (e.g. EpiServer site) and repository. One is to host all solution within the EpiServer site, other is to host indexing part separately as web service.

##### 1) Host all solution within the EpiServer site
The easiest way is to add nuget package (Pepi.Find.Direct) which install all packages and binaries to your project.  
Then you have to setup it: add to proper place e.g. global asax
```
protected void Application_Start()
{
    Pepi.Find.Direct.SearchClientExtensions.SetDefaultDirectClient(
        new Pepi.Find.SqlRepository.SqlIndexRepository(<your connection string here>)
        );
    AreaRegistration.RegisterAllAreas();
}
```
This replaces default Find client with new one.

##### 2) Host as separated service
There's Pepi.Find.WebService project written in ASP.NET Core you may use as base to build up your service or get inspiration.  
Another sample is Pepi.Find.WinDesktop project useful during custom server implementation development. It hosts web service and displays HTTP communication.  
Once you make it up and running, configure your client, e.g. put following to web.config:
```
<episerver.find
	serviceUrl="http://myproject.company.net/dev-index/"
	defaultIndex="whatever-you-like"/>
```

### Usage
Pepi.Find is designed as compatible with EpiServer.Find. You should be fine if you just reconnect your site/application to the new solution.  
You have to just re-run indexing to index your existing data if any.  
All your application code and searches should remain as-is!

##### How does it work?
EpiServer.Find (on client side) is all around IClient interface. Anybody able to make an its implementation can make his own client. All searches then goes through the implementation.  
EpiServer.Find (on server side) is standard web service communicating using HTTP/JSON. Anybody able to read client requests and generate right responses can make his own service.  
Of course, to be able to write your Find queries you need EpiServer.Find - as it defines all query types and methods.

### Conclusion
Hopefully, that's enough information to be able to manage installation. Feel free and open issue to ask something or suggest improvement. Also any comment or rating is welcomed.
