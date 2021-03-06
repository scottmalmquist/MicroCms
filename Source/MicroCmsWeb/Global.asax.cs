﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Lucene.Net.Store;
using Lucene.Net.Store.Azure;
using MicroCms.Azure.Configuration;
using MicroCms.Configuration;
using MicroCms.Lucene.Configuration;
using MicroCms.Redis.Configuration;
using MicroCms.Sql.Configuration;
using MicroCms.Unity;
using MicroCms.WebApi;
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.Storage;

namespace MicroCms
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            var builder = new ContainerBuilder();
            builder.RegisterControllers(typeof(MvcApplication).Assembly);
            builder.RegisterApiControllers(typeof(MvcApplication).Assembly, typeof(CmsDocumentsController).Assembly);
            _Container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(_Container));

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ConfigureCms();
        }

        private static IContainer _Container;
		private const bool UseAzure = false;
		private const bool UseRedis = false;
		private const bool UseSql = false;

        private void ConfigureCms()
        {
            var rootFolder = Server.MapPath("~/");
            var cmsDirectory = new DirectoryInfo(Path.Combine(rootFolder, @"App_Data\Cms"));

            if (cmsDirectory.Exists && !UseSql)
            {
                cmsDirectory.Delete(true);
            }

            var unity = new UnityContainer();
            var configuration = unity.ConfigureCms()
                .UseHtmlRenderer()
                .UseTextRenderer()
                .UseMarkdownRenderer()
                .UseSourceCodeRenderer();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (UseAzure)
            {
                var azureStorageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");                
                configuration
                    .UseAzureStorage(azureStorageAccount.CreateCloudBlobClient(), "cms")
                    .UseLuceneSearch(new AzureDirectory(azureStorageAccount, "cms-index", new RAMDirectory()));
            }
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			else if (UseRedis)
			{
				configuration
					.UseRedisStorage()
					.UseLuceneSearch(new SimpleFSDirectory(new DirectoryInfo(Path.Combine(cmsDirectory.FullName, "Index"))));
			}
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			else if (UseSql)
			{
				// To configure db via web.config, uncomment entityFramework section and connection string and pass in connection string
				// name to ICmsConfigurator; e.g. .UseSqlStorage("DefaultConnection")
				if (!cmsDirectory.Exists)
					cmsDirectory.Create();
				string connectionString =
					string.Format(
						@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={0}\{1}.mdf;Initial Catalog={1};Integrated Security=True;MultipleActiveResultSets=true",
						cmsDirectory.FullName, "MicroCmsSqlDb");
				configuration
					.UseSqlStorage(connectionString)
					.UseSqlSearch(connectionString);
			}
			else
            {
                configuration
                    .UseFileSystemStorage(cmsDirectory)
                    .UseLuceneSearch(new SimpleFSDirectory(new DirectoryInfo(Path.Combine(cmsDirectory.FullName, "Index"))));
            }

            Cms.Configure(() => new UnityCmsContainer(unity.CreateChildContainer()));
            using (var context = Cms.CreateContext())
            {
                if (!context.Documents.GetAll().Any())
                {
                    var rowView = new CmsView("RowView", "<div class=\"row\">{0}</div>");
                    var sidebarView = new CmsView("SidebarView", "<div>{0}</div>");
                    context.Views.Save(rowView);
                    context.Views.Save(sidebarView);
                    var document = new CmsDocument("Example Rows",
                        CreateMarkdown("#MD4", 4),
                        CreateMarkdown("#MD8", 8),
                        CreateMarkdown("#MD3", 3),
                        CreateMarkdown("#MD3", 3),
                        CreateMarkdown("#MD3", 3),
                        CreateMarkdown("#MD3", 3),
                        CreateMarkdown("#MD12", 12));
                    document.Tags.Add("documents");
                    context.Documents.Save(document);
                    context.Documents.Save(new CmsDocument("Source Code Example", CreateMarkdown(@"#CODE
    {{CSharp}}
    public class Thing
    {
        public string Name { get; set; }
    }
", 12)));
                    var sidebar = new CmsDocument("TableOfContents", new CmsPart(CmsTypes.Markdown, @"[Home](/)

[Docs](/docs/)"));
                    sidebar.Tags.Add("TableOfContents");
                    context.Documents.Save(sidebar);

                    var readmeText = File.ReadAllText(Path.Combine(rootFolder, @"..\..\README.md"));

                    var homeDocument = new CmsDocument("Readme", CreateMarkdown(readmeText, 12));
                    homeDocument.Tags.Add("home");
                    Cms.CreateContext().Documents.Save(homeDocument);
                }
            }
        }

        private CmsPart CreateMarkdown(string value, int columns)
        {
            return new CmsPart(CmsTypes.Markdown, value, new
            {
                @class = "col-md-" + columns
            });
        }
    }
}
