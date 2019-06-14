// Licensed under the MIT license. See https://kieranties.mit-license.org/ for full license information.

using System;
using System.IO;
using LibGit2Sharp;
using SimpleVersion.Abstractions;
using SimpleVersion.Abstractions.Pipeline;
using SimpleVersion.Model;
using SimpleVersion.Pipeline;
using SimpleVersion.Pipeline.Formatting;

namespace SimpleVersion
{
    /// <summary>
    /// Entry point for version calculation.
    /// </summary>
    public class VersionCalculator : IVersionCalculator
    {
        /// <summary>
        /// Default calculator instance.
        /// </summary>
        /// <returns>An instance of <see cref="VersionCalculator"/>.</returns>
        public static VersionCalculator Default() => new VersionCalculator();

        /// <inheritdoc/>
        public VersionResult GetResult(string path)
        {
            var resolvedPath = ResolveRepoPath(path);

            using (var repo = new Repository(resolvedPath))
            {
                // Init context
                var ctx = new VersionContext(repo);
                ctx.Result.RepositoryPath = Directory.GetParent(resolvedPath).Parent.FullName;

                // Resolve build server information
                ApplyProcessor<AzureDevopsContextProcessor>(ctx);

                // Resolve configuration
                ApplyProcessor<ConfigurationContextProcessor>(ctx);

                // Resolve Formats
                ApplyProcessor<VersionFormatProcess>(ctx);
                ApplyProcessor<Semver1FormatProcess>(ctx);
                ApplyProcessor<Semver2FormatProcess>(ctx);

                return ctx.Result;
            }
        }

        private string ResolveRepoPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must be provided.", nameof(path));

            var resolvedPath = Repository.Discover(path);

            if (string.IsNullOrWhiteSpace(resolvedPath))
                throw new DirectoryNotFoundException($"Could not find git repository at '{path}' or any parent directory.");

            return resolvedPath;
        }

        private void ApplyProcessor<T>(IVersionContext context)
            where T : IVersionContextProcessor, new()
        {
            var instance = new T();
            instance.Apply(context);
        }
    }
}