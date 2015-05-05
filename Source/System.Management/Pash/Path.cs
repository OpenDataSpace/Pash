// Copyright (C) Pash Contributors. License GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Management.Automation;
using Pash.Implementation;
using Microsoft.PowerShell.Commands;

namespace System.Management
{

    /// <summary>
    /// Imutable class that acts like a string, but provides many options around manipulating a powershell 'path'.
    /// </summary>
    internal class Path
    {
        private readonly string _rawPath;
        public static readonly string PredeterminedCorrectSlash;
        public static readonly string PredeterminedWrongSlash;

        static Path()
        {
            if (System.IO.Path.DirectorySeparatorChar.Equals('/'))
            {
                PredeterminedCorrectSlash = "/";
                PredeterminedWrongSlash = "\\";
            }
            else
            {
                PredeterminedCorrectSlash = "\\";
                PredeterminedWrongSlash = "/";
            }
        }

        public Path(string rawPath)
            : this(PredeterminedCorrectSlash, PredeterminedWrongSlash, rawPath)
        {
        }

        public Path(string correctSlash, string wrongSlash, string rawPath)
        {
            _rawPath = rawPath ?? string.Empty;
            CorrectSlash = correctSlash;
            WrongSlash = wrongSlash;
        }

        public string CorrectSlash { get; set; }

        public string WrongSlash { get; set; }

        public static implicit operator string(Path path)
        {
            return path == null ? null : path._rawPath;
        }

        public static implicit operator Path(string path)
        {
            return new Path(path);
        }

        public override bool Equals(object obj)
        {
            if (obj is string)
            {
                return _rawPath.Equals(obj);
            }

            var objPath = obj as Path;
            if (objPath != null)
            {
                return _rawPath.Equals(objPath._rawPath);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _rawPath.GetHashCode();
        }

        public Path NormalizeSlashes()
        {
            return new Path(CorrectSlash, WrongSlash, _rawPath.Replace(WrongSlash, CorrectSlash));
        }

        public Path TrimEnd(params char[] trimChars)
        {
            return new Path(CorrectSlash, WrongSlash, _rawPath.TrimEnd(trimChars));
        }

        public Path TrimEndSlash()
        {
            return new Path(CorrectSlash, WrongSlash, _rawPath.TrimEnd(char.Parse(CorrectSlash)));
        }

        public Path TrimStartSlash()
        {
            return new Path(CorrectSlash, WrongSlash, _rawPath.TrimStart(char.Parse(CorrectSlash)));
        }

        public int LastIndexOf(char value)
        {
            return _rawPath.LastIndexOf(value);
        }

        public int LastIndexOf(string value)
        {
            return _rawPath.LastIndexOf(value);
        }

        public int IndexOf(char value)
        {
            return _rawPath.IndexOf(value);
        }

        public int IndexOf(string value)
        {
            return _rawPath.IndexOf(value);
        }

        public Path GetChildNameOrSelfIfNoChild()
        {
            Path path = this.NormalizeSlashes()
                .TrimEndSlash();

            int iLastSlash = path.LastIndexOf(CorrectSlash);
            if (iLastSlash == -1)
            {
                return path;
            }

            return new Path(CorrectSlash, WrongSlash, path._rawPath.Substring(iLastSlash + 1));
        }

        public Path GetParentPath(Path root)
        {
            var path = this;
            // normalize first
            path = path.NormalizeSlashes().TrimEndSlash();

            if (root != null)
            {
                root = root.NormalizeSlashes().TrimEndSlash();
                if (string.Equals(path, root, StringComparison.CurrentCultureIgnoreCase))
                {
                    return new Path(CorrectSlash, WrongSlash, string.Empty);
                }
            }

            int iLastSlash = path._rawPath.LastIndexOf(CorrectSlash);

            string newPath = root;

            if (iLastSlash > 0)
            {
                newPath = path._rawPath.Substring(0, iLastSlash);
            }
            else if (iLastSlash == 0)
            {
                newPath = new Path(CorrectSlash, WrongSlash, CorrectSlash);
            }

            Path resultPath = new Path(CorrectSlash, WrongSlash, newPath);

            return resultPath.ApplyDriveSlash();
        }

        public Path ApplyDriveSlash()
        {
            // append a slash to the end (if it's the root drive)
            if (this.IsRootPath())
            {
                if (!this.EndsWithSlash())
                {
                    return this.AppendSlashAtEnd();
                }
                return this;
            }

            var result = this.TrimEndSlash();
            return result;
        }

        public Path Combine(Path child)
        {
            var parent = this;

            if (string.IsNullOrEmpty(parent) && string.IsNullOrEmpty(child))
            {
                return CorrectSlash; // root
            }

            if (string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(child))
            {
                return child.NormalizeSlashes();
            }

            parent = parent.NormalizeSlashes();

            if (!string.IsNullOrEmpty(parent) && string.IsNullOrEmpty(child))
            {
                if (parent.EndsWithSlash())
                {
                    return parent;
                }
                else
                {
                    return parent.AppendSlashAtEnd();
                }
            }

            child = child.NormalizeSlashes();
            var builder = new System.Text.StringBuilder(parent);

            if (!parent.EndsWithSlash())
                builder.Append(CorrectSlash);

            // Make sure we do not add two \
            if (child.StartsWithSlash())
            {
                builder.Append(child, 1, child.Length - 1);
            }
            else
            {
                builder.Append(child);
            }

            return new Path(CorrectSlash, WrongSlash, builder.ToString());

        }

        public Path GetFullPath(string driveName, string currentLocation, bool isFileSystemProvider)
        {
            if (this.IsRootPath())
            {
                return this.MakePath(driveName);
            }

            if (isFileSystemProvider)
            {
                Path combinedPath;

                if (this.HasDrive())
                {
                    combinedPath = this;
                }
                else
                {
                    combinedPath = ((Path)currentLocation).Combine(this);
                }

                return new Path(CorrectSlash, WrongSlash, System.IO.Path.GetFullPath(combinedPath));
            }


            // TODO: this won't work with non-file-system paths that use "../" navigation or other "." navigation.
            return (new Path(CorrectSlash, WrongSlash, currentLocation)).Combine(this);
        }

        public bool StartsWithSlash()
        {
            if (this.NormalizeSlashes()._rawPath.StartsWith(CorrectSlash))
            {
                return true;
            }

            return false;
        }

        public bool EndsWithSlash()
        {
            if (this.NormalizeSlashes()._rawPath.EndsWith(CorrectSlash))
            {
                return true;
            }
            return false;
        }

        public Path AppendSlashAtEnd()
        {
            if (this.EndsWithSlash())
            {
                return this;
            }

            return new Path(CorrectSlash, WrongSlash, this._rawPath + CorrectSlash);
        }

        public bool StartsWith(string value)
        {
            return _rawPath.StartsWith(value);
        }

        public bool IsRootPath()
        {
            // handle unix '/' path
            if (this.Length == 1 && this == CorrectSlash)
            {
                return true;
            }

            // handle windows drive "C:" "C:\\"
            var x = this.TrimEndSlash();
            if (this.GetDrive() == x._rawPath.TrimEnd(':'))
            {
                return true;
            }

            return false;
        }

        public string GetDrive()
        {
            if (this.StartsWithSlash())
            {
                // return unix drive
                return FileSystemProvider.FallbackDriveName;
            }

            int iDelimiter = _rawPath.IndexOf(':');

            if (iDelimiter == -1)
                return null;

            return _rawPath.Substring(0, iDelimiter);
        }

        public bool TryGetDriveName(out string driveName)
        {
            driveName = GetDrive();
            if (string.IsNullOrEmpty(driveName))
            {
                return false;
            }
            return true;
        }

        public bool HasDrive()
        {
            var drive = GetDrive();

            if (string.IsNullOrEmpty(drive))
            {
                return false;
            }
            return true;
        }

        public string GetDirectory()
        {
            var lastSlash = LastIndexOf(CorrectSlash);
            return new Path(_rawPath.Substring(0, lastSlash)); // path without last slash and stuff behind
        }

        public bool HasExtension()
        {
            return System.IO.Path.HasExtension(_rawPath);
        }

        public string GetExtension()
        {
            return System.IO.Path.GetExtension(_rawPath);
        }

        public string GetFileNameWithoutExtension()
        {
            return System.IO.Path.GetFileNameWithoutExtension(_rawPath);
        }

        public Path RemoveDrive()
        {
            string drive;
            if (this.TryGetDriveName(out drive))
            {
                var newPath = _rawPath.Substring(drive.Length);
                if (newPath.StartsWith(":"))
                    return new Path(CorrectSlash, WrongSlash, newPath.Substring(1));
                return new Path(CorrectSlash, WrongSlash, newPath);
            }

            return this;
        }

        public Path MakePath(string driveName)
        {
            Path fullPath;
            if (driveName == CorrectSlash)
            {
                string preSlash = this.StartsWithSlash() ? string.Empty : CorrectSlash;

                fullPath = new Path(CorrectSlash, WrongSlash, string.Format("{0}{1}", preSlash, this));
            }
            else
            {
                if (this.HasDrive())
                {
                    return this;
                }

                //TODO: should this take a "current path" parameter? EX: {drive}:{currentPath??}/{this}
                string preSlash = this.StartsWithSlash() ? string.Empty : CorrectSlash;

                fullPath = new Path(CorrectSlash, WrongSlash, string.Format("{0}:{1}{2}", driveName, preSlash, this));
            }

            return fullPath.NormalizeSlashes();
        }

        public Path ResolveTilde()
        {
            if (!_rawPath.StartsWith("~"))
            {
                return this;
            }

            // TODO: this function should use the (currently not implemented) value of ProviderInfo.Home of the current
            // provider. maybe that value should be passed as an argument

            // Older Mono versions (sadly the one that's currently still
            // available) have a bug where GetFolderPath returns an empty
            // string for most SpecialFolder values, but only on
            // non-Windows.
            // See: https://bugzilla.xamarin.com/show_bug.cgi?id=2873

            Path homepath= Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // HACK: Use $Env:HOME until Mono 2.10 dies out.
            if (homepath == "")
            {
                homepath = Environment.GetEnvironmentVariable("HOME");
            }
            homepath = homepath.AppendSlashAtEnd();
            return homepath.Combine(_rawPath.Substring(1));
        }

        public override string ToString()
        {
            return _rawPath;
        }

        public int Length { get { return _rawPath.Length; } }
    }

    internal static partial class _
    {
        public static Path AsPath(this string value)
        {
            return (Path)value;
        }

        public static string NormalizeSlashes(this string value)
        {
            return ((Path)value).NormalizeSlashes();
        }
    }

}

