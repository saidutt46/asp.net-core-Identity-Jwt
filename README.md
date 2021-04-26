# asp.net-core-Identity-Jwt
ASP.NET Core Identity/JWT angular app with minimal startup setup

### This application repo is to help me and other devs fly right by with minial setup for their ASP.NET Application to have Authentication and Authorization setup with Identity, JWT, Entity Framework Core and
MySql (or any preferred DB, with few changes).

### Setup

- Download or Clone repo https://github.com/saidutt46/asp.net-core-Identity-Jwt
- Restore Nuget packages and Build the application.
- This application uses MySql server by default powered by EF ORM.
- If that works for you, great! All you need to do is change `connection string`
- Its in the appsettings.json file in the WebApi folder.

### Projects in the Solution and details

#### Data 
- Holds the entities User and Role tied to IdentityUser and Identity Role
- Has the Database Context logic for EF.
- You can add your own DbContext and make it to work with `DesignTimeDbContextFactoryBase` Class
- Follow the `ApplicationContext` file to replicate your own DbContext
- Two ways to run EF Migrations
 
Navigate to Data project and run the following commands
`dotnet ef migrations add 'name of the migration(initial)'`
`dotnet ef database update`

If you want to do it on the fly using code.
**please uncomment the following section in the `Startup.cs` file on WebApi project.

`using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
  {
      var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationContext>();
      context.Database.Migrate();
  }`

This should run pending migrations for you that have already been created and exist in the system.
**Please make sure to delete the migrations folder if following step 1 doing it manually**


#### Dto
- Dto project holds the Data Transfer Objects for communication between UI and API

#### WebApi
- WebApi holds the Controller logic for Authentication, Roles, Check User Details, Get List of User Details
- The Startup.cs file holds the primary middleware logic for Identity and other services used in the application.

**Important Steps Below**
-If everything is successful and you have the app running, the swagger page should be launched on startup
- Register and login doesn't have any Authorizations on API endpoints.
- Rest of the other API's have Authorization enabled by default
- You can remove it by commenting or removing the `[Authorize]` tags on individual API's
- Once a user is register and logged in you should have a Bearer Token in the login response along with UserId (Guid).
- You can add custom roles to the users
- As an example all the Authorized endpoints with `AllowAllAccess` Policy created should have SuperUser or Admin Roles.
- `options.AddPolicy("AllAccessPolicy", policy => policy.RequireRole("SuperUser", "Admin"));`
- Create your own Policy or Role based Auth setup and update the Controller logic to follow the updated instructions.
- Keep hacking! 🧑‍💻
