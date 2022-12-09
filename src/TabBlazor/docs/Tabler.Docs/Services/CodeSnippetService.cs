﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Tabler.Docs.Services
{

    public interface ICodeSnippetService
    {
        public Task<string> GetCodeSnippet(string className);
    }

    public class FakeSnippetService : ICodeSnippetService
    {
        public Task<string> GetCodeSnippet(string className)
        {
            return Task.FromResult("Source code view is disabled");
        }
    }

    public class LocalSnippetService : ICodeSnippetService
    {
        public async Task<string> GetCodeSnippet(string className)
        {
            var basePath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.Parent.FullName;
            const string projectName = "Tabler.Docs";
            var classPath = projectName + className.Substring(projectName.Length).Replace(".", @"\");
            var codePath = Path.Combine(basePath, $"{classPath}.razor");

            if (File.Exists(codePath))
            {
                return await Task.FromResult(File.ReadAllText(codePath));
            }
            else
            {
                return await Task.FromResult($"Unable to find code at {codePath}");
            }
        }
    }

    public class GitHubSnippetService : ICodeSnippetService
    {
        const string repo = "joadan/Blazor-Tabler";
        //const string baseUrl = "https://raw.githubusercontent.com/joadan/TabBlazor/master/docs/Tabler.Docs";
        const string baseUrl = "https://tabblazor.com/_content/razor_source";
     
        private readonly IHttpClientFactory httpClientFactory;

        private Dictionary<string, string> cachedCode = new Dictionary<string, string>();

        public GitHubSnippetService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetCodeSnippet(string className)
        {
            try
            {
                if (!cachedCode.ContainsKey(className))
                {
                    var baseName = "Tabler.Docs.";
                    var path = baseUrl + "/" + className.Replace(baseName, "").Replace(".", "/") + ".razor";

                    using var httpClient = httpClientFactory.CreateClient("GitHub");
                    using var stream = await httpClient.GetStreamAsync(path);
                    StreamReader reader = new StreamReader(stream);

                    var code = reader.ReadToEnd();

                    if (!cachedCode.ContainsKey(className))
                    {
                        cachedCode[className] = code;
                    }
                 
                }

                return cachedCode[className];

            }
            catch (Exception ex)
            {
                return $"Unable to load code. Error: {ex.Message}";
            }
        }
    }
}
