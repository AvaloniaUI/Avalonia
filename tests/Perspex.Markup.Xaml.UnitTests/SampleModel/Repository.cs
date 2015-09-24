// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace GitHubClient.ViewModels
{
    public class Repository
    {
        private readonly string _name;

        public Repository(string name)
        {
            _name = name;
        }

        public string Name => _name;
    }
}