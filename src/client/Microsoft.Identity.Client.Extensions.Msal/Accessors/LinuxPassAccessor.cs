// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.Identity.Client.Extensions.Msal.Accessors
{
    internal class LinuxPassAccessor : ICacheAccessor
    {
        public static readonly byte[] DummyData = Encoding.UTF8.GetBytes("{}");

        private readonly string _cacheFilePath;
        private readonly TraceSourceLogger _logger;
        private readonly bool _setOwnerOnlyPermission;

        internal LinuxPassAccessor(string cacheFilePath, bool setOwnerOnlyPermissions, TraceSourceLogger logger)
        {
            _cacheFilePath = cacheFilePath;
            _setOwnerOnlyPermission = setOwnerOnlyPermissions;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Clear()
        {
            _logger.LogInformation("Deleting cache file");
        }

        public ICacheAccessor CreateForPersistenceValidation()
        {
            return new LinuxPassAccessor(_cacheFilePath, _setOwnerOnlyPermission, _logger);
        }

        public byte[] Read()
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] data)
        {
            throw new NotImplementedException();
        }

        private string GetGpgPath()
        {
            if (TryLocateExecutable("gpg2", null, out string gpg2Path))
            {
                _logger.LogInformation($"Using PATH-located GPG (gpg2) executable: {gpg2Path}");
                return gpg2Path;
            }

            if (TryLocateExecutable("gpg", null,  out string gpgPath))
            {
                _logger.LogInformation($"Using PATH-located GPG (gpg) executable: {gpgPath}");
                return gpgPath;
            }
            throw new Exception("Failed to find gpg on PATH");
        }

        private bool TryLocateExecutable(string program, ICollection<string> pathsToIgnore, out string path)
        {
            // On UNIX-like systems we would normally use the "which" utility to locate a program,
            // but since distributions don't always place "which" in a consistent location we cannot
            // find it! Oh the irony..
            // We could also try using "env" to then locate "which", but the same problem exists in
            // that "env" isn't always in a standard location.
            //
            // On Windows we should avoid using the equivalent utility "where.exe" because this will
            // include the current working directory in the search, and we don't want this.
            //
            // The upshot of the above means we cannot use either of "which" or "where.exe" and must
            // instead manually scan the PATH variable looking for the program.
            // At least both Windows and UNIX use the same name for the $PATH or %PATH% variable!
            var pathValue = System.Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathValue))
            {
                string[] paths = pathValue.Split(':');
                foreach (var basePath in paths)
                {
                    string candidatePath = Path.Combine(basePath, program);
                    if (File.Exists(candidatePath) && (pathsToIgnore is null ||
                        !pathsToIgnore.Contains(candidatePath, StringComparer.OrdinalIgnoreCase)))
                    {
                        path = candidatePath;
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }
    }
}
