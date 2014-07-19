﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroCms.SourceCode.Tests
{
    [TestClass]
    public class SourceCodeCmsRendererServiceTests
    {
        [TestMethod]
        public void ValidateBasicRender()
        {
            var result = new SourceCodeCmsRendererService().Render(new CmsPart(CmsTypes.SourceCode + "/csharp", "public class Thing { public string Name { get; set; } }"));
            Assert.IsNotNull(result);
        }
         
    }
}