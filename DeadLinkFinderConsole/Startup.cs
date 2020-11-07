﻿using GitHubRepoFinder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WebsiteLinksChecker;


namespace DeadLinkFinderConsole
{
    class Startup
    {
        public static IConfigurationRoot Configuration { get; set; }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Configuration);
            services.AddSingleton(new GitHubClient(new ProductHeaderValue("GitHub-repo-finder-for-dead-links-in-readmes")));

            services.AddSingleton(new SearchRepositoriesRequest()
            {
                // lets find a library with over ? stars
                Stars = Octokit.Range.GreaterThan(5000),
                //Stars = Octokit.Range.LessThan(1),

                // check for repos that have been updated between a given date range?
                Updated = DateRange.Between(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow),

                // orrder by?
                SortField = RepoSearchSort.Updated,
                Order = SortDirection.Descending,
            });

            var linkGetter = new LinkGetter(new HttpClient(), "readme");
            services.AddTransient<ILinkGetter>(s => linkGetter);
            services.AddTransient<ILinkChecker>(s => new LinkChecker(new HttpClient(), linkGetter));

            services.AddTransient<ProgramUI, ProgramUI>();
            services.AddTransient<IFileNameFromUri, FileNameFromUri>();


            services.AddSingleton<IUriFinder, GitHubActiveReposFinder>();
        }

        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            ConfigureServices(services);
            ServiceProvider provider = services.BuildServiceProvider();

            Task task = Task.Run(async () =>
            {
                ProgramUI program = provider.GetRequiredService<ProgramUI>();
                await program.RunAsync();
            });

            try
            {
                task.Wait();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine(ae);
                throw ae.InnerException;
            }
        }
    }
}
