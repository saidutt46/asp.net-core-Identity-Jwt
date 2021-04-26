using System;
using Application.Data.Shared;
using Microsoft.EntityFrameworkCore;

namespace Application.Data.Context
{
    public class ApplicationContextFactory : DesignTimeDbContextFactoryBase<ApplicationContext>
    {
        protected override ApplicationContext CreateNewInstance(DbContextOptions<ApplicationContext> options)
        {
            return new ApplicationContext(options);
        }
    }
}
