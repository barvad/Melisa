using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Practicum.MelisaBot.Ef
{
    public static class Injection
    {
        public static IServiceCollection AddDbContext(this IServiceCollection services)
        {
            var connectionString = "Host=db;Port=5432;Database=melisadb;Username=postgres;Password=postgres123;" +
                                   "Maximum Pool Size=100;Minimum Pool Size=10;Pooling=true;";

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector(); 
            var dataSource = dataSourceBuilder.Build();

            services.AddDbContextFactory<MelisaDbContext>(options =>
            {
                options.UseNpgsql(dataSource, o => o.UseVector());
            });
         
            return services;
        }
    }
}
