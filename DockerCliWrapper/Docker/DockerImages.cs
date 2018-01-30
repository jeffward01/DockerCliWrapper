﻿using DockerCliWrapper.Extensions;
using DockerCliWrapper.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DockerCliWrapper.Docker
{
    /// <summary>
    /// Read operation for all local images. <see cref="https://docs.docker.com/engine/reference/commandline/images/"/> 
    /// for full CLI details.
    /// </summary>
    public class DockerImages
    {
        private const string Command = "docker";
        private const string DefaultArg = "images";

        private bool _showAll;
        private bool _showDigests;
        private bool _doNotTruncate;
        private IDictionary<string, string> _filters;
        private List<DockerImagesFormatPlaceHolders> _placeHolders;
        private bool _beQuiet;
        private string _repository;
        private string _tag;

        public DockerImages()
        {
            _filters = new Dictionary<string, string>();
            _placeHolders = new List<DockerImagesFormatPlaceHolders>();
        }

        /// <summary>
        /// Show all images (default hides intermediate images)
        /// </summary>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages ShowAll()
        {
            _showAll = true;
            return this;
        }

        /// <summary>
        /// Returns the digests data for the images.
        /// </summary>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages ShowDigests()
        {
            _showDigests = true;
            return this;
        }

        /// <summary>
        /// Add a filter that will display untagged images that are the leaves of the images tree (not intermediary layers). 
        /// These images occur when a new build of an image takes the repo:tag away from the image ID, leaving it as <none>:<none> or untagged.
        /// </summary>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages AddDanglingFilter()
        {
            _filters.Add("dangling", "true");
            return this;
        }

        /// <summary>
        /// Add a filter that matches images based on the presence of a label alone or a label and a value.
        /// </summary>
        /// <param name="label"></param>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages AddLabelFilter(string label)
        {
            _filters.Add("label", label);
            return this;
        }

        /// <summary>
        /// Add a filter that shows only images created before the image with given id or reference.
        /// </summary>
        /// <param name="before"></param>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages AddBeforeFilter(string before)
        {
            _filters.Add("before", before);
            return this;
        }

        /// <summary>
        /// Add a filter that shows only images created before the image with given id or reference. 
        /// </summary>
        /// <param name="since"></param>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages AddSinceFilter(string since)
        {
            _filters.Add("since", since);
            return this;
        }

        /// <summary>
        /// Add a filter that shows only images whose reference matches the specified pattern.
        /// </summary>
        /// <param name="pattern">The pattern value.</param>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages AddReferenceFilter(string pattern)
        {
            throw new NotImplementedException("Not yet implemented - do not understand the syntax");
        }

        /// <summary>
        /// Pretty-print images using a Go template.
        /// </summary>
        /// <param name="placeHolders">A collection of enums containing the available Go placeholder.</param>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages FormatResults(List<DockerImagesFormatPlaceHolders> placeHolders)
        {
            if (!placeHolders.Any())
            {
                throw new ArgumentException("At least one placeholder has to be defined.", nameof(placeHolders));
            }

            _placeHolders = placeHolders;
            return this;
        }

        /// <summary>
        /// Don’t truncate output.
        /// </summary>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages DoNotTruncate()
        {
            _doNotTruncate = true;
            return this;
        }

        /// <summary>
        /// Only populate the image IDs in the response.
        /// </summary>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages BeQuiet()
        {
            _beQuiet = true;
            return this;
        }

        /// <summary>
        /// Restricts the list to all images that match the argument.
        /// </summary>
        /// <param name="repository">The repository filter.</param>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages ForRepository(string repository)
        {
            _repository = repository;
            return this;
        }

        /// <summary>
        /// Restricts the list to images that match the argument and TAG.
        /// </summary>
        /// <param name="repository">The repository filter.</param>
        /// <param name="tag">The tag filter.</param>
        /// <returns>The current instance (fluent interface).</returns>
        public DockerImages ForRepositoryAndTag(string repository, string tag)
        {
            _repository = repository;
            _tag = tag;
            return this;
        }

        public List<DockerImagesResult> Execute()
        {
            var result = ShellExecutor.Instance.Execute(Command, GenerateArguments());

            if (_beQuiet)
            {
                return DockerImagesResultsParser.ParseQuietResult(result);
            }

            if (_placeHolders.Any())
            {
                return DockerImagesResultsParser.ParseFormattedResult(result, _placeHolders);
            }

            return DockerImagesResultsParser.ParseResult(result);
        }

        private string GenerateArguments()
        {
            var arguments = new StringBuilder();

            arguments.Append(" images");

            if (_showAll)
            {
                arguments.Append(" -a");
            }

            if (_showDigests)
            {
                arguments.Append(" -digests");
            }

            foreach(var filter in _filters)
            {
                arguments.AppendFormat(" -f \"{0}={1}\"", filter.Key, filter.Value);
            }

            if (_placeHolders.Any())
            {
                arguments.AppendFormat(
                    " --format \"{0}\"", 
                    string.Join(" ~ ", _placeHolders.Select(p => "{{" + p.GetDescription() + "}}")));
            }

            if (_doNotTruncate)
            {
                arguments.Append(" --no-trunc");
            }

            if (_beQuiet)
            {
                arguments.Append(" -q");
            }

            return arguments.ToString();
        }
    }
}