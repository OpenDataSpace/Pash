// Copyright (C) Pash Contributors. License GPL/BSD. See https://github.com/Pash-Project/Pash/
using System;
using System.Linq;

namespace System.Management
{
    internal class PathNavigation
    {
        public static string CalculateFullPath(Path curLocation, Path changeCommandStr)
        {
            // TODO: sburnicki rewrite that stuff with pathintrinsics
            var changeCommand = (changeCommandStr ?? string.Empty).NormalizeSlashes();
            var currentLocation = curLocation.NormalizeSlashes();

            bool applyParts = false;
            Path resultPath;

            // use the input 'changeCommand' path if it's 
            // 'rooted' otherwise we go from the currentLocation
            if (changeCommand.HasDrive())
            {
                // windows case where changeCommand == "/" or "\" but the currentLocation has a "C:" drive
                string currentLocationDrive = currentLocation.GetDrive();
                if (changeCommand.StartsWithSlash() && !changeCommand.GetDrive().Equals(currentLocationDrive, StringComparison.InvariantCultureIgnoreCase))
                {
                    resultPath = new Path(currentLocation.CorrectSlash, currentLocation.WrongSlash, string.Format("{0}:{1}", currentLocationDrive, changeCommand));
                }
                else
                {
                    resultPath = changeCommand;
                }
            }
            else
            {
                applyParts = true;
                resultPath = currentLocation;
            }

            var correctSeparator = Char.Parse(currentLocation.CorrectSlash);
            var changeParts = changeCommand.ToString().Split(correctSeparator).Where(s => !string.IsNullOrEmpty(s));

            foreach (var part in changeParts)
            {
                // ignore single dot as it does nothing...
                if (part == ".")
                {
                    continue;
                }

                // ignore trying to go up a dir from the root dir
                if (part == ".." && resultPath.IsRootPath())
                {
                    continue;
                }

                if (part == "..")
                {
                    resultPath = resultPath.GetParentPath(currentLocation.GetDrive());
                }
                else if (applyParts)
                {
                    resultPath = resultPath.Combine(part);
                }
            }

            return resultPath.ApplyDriveSlash();
        }
    }
}
