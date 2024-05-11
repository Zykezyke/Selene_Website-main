using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SELENE_STUDIO.Data;

namespace SELENE_STUDIO.Helper
{
    public static class DataHelper
    {

        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            //Service: An instance of db context
            var dbContextSvc = svcProvider.GetRequiredService<LogAppDbContext>();

            //Migration: This is the programmatic equivalent to Update-Database
            await dbContextSvc.Database.MigrateAsync();
        }


    }
}
