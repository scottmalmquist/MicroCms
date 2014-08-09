﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using MicroCms.Tests;
using Xunit;

namespace MicroCms.Client.Tests
{
    public class MicroCmsClientTests : IUseFixture<WebApiFixture>
    {
        private static readonly Uri _WebApiUrl = new Uri(WebApiFixture.WebUrl, "api/cms/");

        [Fact]
        public void GetDocumentsSucceeds()
        {
            using (var client = new MicroCmsClient(_WebApiUrl))
            {
                var documents = client.GetDocumentsAsync().Result;
                Assert.NotNull(documents);
                Debug.WriteLine("{0} documents", documents.Length);
                Assert.NotEqual(0, documents.Length);
            }
        }

        [Fact]
        public void PostDocumentSucceeds()
        {
            using (var client = new MicroCmsClient(_WebApiUrl))
            {
                var document = new CmsDocument("PostedDocument",
                    new CmsPart(CmsTypes.Markdown, "##NEWSTUFF", new
                    {
                        @class = "something"
                    }));

                var url = client.PostDocumentAsync(document).Result;
                
                Assert.NotNull(url);

                var loaded = client.GetDocumentAsync(Guid.Parse(url.AbsoluteUri.Split('/').Last())).Result;
                Assert.NotNull(loaded);
                Assert.Equal(document.Title, loaded.Title);
            }
        }

        [Fact]
        public void PutDocumentSucceeds()
        {
            using (var client = new MicroCmsClient(_WebApiUrl))
            {
                var document = Fixture.ExampleDocument;
                document.Tags.Add("PutTagAddition");
                client.PutDocumentAsync(document).Wait();

                var loaded = client.GetDocumentAsync(document.Id).Result;
                Assert.NotNull(loaded);
                Assert.Equal(document.Id, loaded.Id);
                Assert.Equal(1, loaded.Tags.Count);
                Assert.Equal("PutTagAddition", loaded.Tags.First());
            }
        }

        [Fact]
        public void DeleteDocumentSucceeds()
        {
            using (var client = new MicroCmsClient(_WebApiUrl))
            {
                var document = new CmsDocument("DeletedDocument", new CmsPart(CmsTypes.Markdown, "##DELETEDSTUFF", new
                {
                    @class = "something"
                }));

                var url = client.PostDocumentAsync(document).Result;
                Assert.NotNull(url);

                var id = Guid.Parse(url.AbsoluteUri.Split('/').Last());
                client.DeleteDocumentAsync(id).Wait();

                //Verify that the document was deleted
                Assert.Throws<HttpRequestException>(() =>
                {
                    try
                    {
                        var loaded = client.GetDocumentAsync(id).Result;
                    }
                    catch (AggregateException exception)
                    {
                        throw exception.InnerException;
                    }
                });
            }
        }

        [Fact]
        public void GetViewsSucceeds()
        {
            using (var client = new MicroCmsClient(_WebApiUrl))
            {
                var views = client.GetViewsAsync().Result;
                Assert.NotNull(views);
                Debug.WriteLine("{0} views", views.Length);
                Assert.NotEqual(0, views.Length);
            }
        }

        private WebApiFixture Fixture { get; set; }

        public void SetFixture(WebApiFixture fixture)
        {
            Fixture = fixture;
        }
    }
}
